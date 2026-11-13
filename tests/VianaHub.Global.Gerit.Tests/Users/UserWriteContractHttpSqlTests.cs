using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Exceptions;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Repository.Identity;
using VianaHub.Global.Gerit.Infra.Data.Security;
using VianaHub.Global.Gerit.Tests.Infrastructure;

namespace VianaHub.Global.Gerit.Tests.Users;

[Collection("Roles SQL HTTP")]
public sealed class UserWriteContractHttpSqlTests(GeritSqlWebApplicationFactory factory)
    : IClassFixture<GeritSqlWebApplicationFactory>
{
    [SqlServerIntegrationFact]
    public Task LegacyPost_OmittingOptionalFields_ReturnsOnlyIdAndPersistsNulls() => WithTenantAsync(async (tenant, client) =>
    {
        using var response = await client.PostAsJsonAsync("/v1/users/", new
        {
            name = "Legacy client",
            email = "legacy@example.invalid",
            password = "Synthetic1!",
            confirmPassword = "Synthetic1!"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        AssertFields(body, ["Id"]);
        var id = body.GetProperty("id").GetInt32();
        var saved = await ReadAsync(tenant, id);
        Assert.Null(saved.PhoneNumber);
        Assert.Null(saved.UrlImage);
        var detail = await client.GetFromJsonAsync<JsonElement>($"/v1/users/{id}");
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("phoneNumber").ValueKind);
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("urlImage").ValueKind);
    });

    [SqlServerIntegrationFact]
    public Task Writes_PersistOptionalFieldsPreserveOmissionsClearNullsAndProtectCredentials() => WithTenantAsync(async (tenant, client) =>
    {
        var id = await CreateAsync(client, "Initial", "initial@example.invalid", "+351123", "https://example.invalid/avatar.png");
        var original = await ReadAsync(tenant, id);
        Assert.Equal("+351123", original.PhoneNumber);
        Assert.Equal("https://example.invalid/avatar.png", original.UrlImage);
        Assert.False(string.IsNullOrWhiteSpace(original.PasswordHash));

        // Campos internos enviados pelo cliente não podem alterar presença ou credenciais.
        using var preserved = await client.PutAsJsonAsync($"/v1/users/{id}", new
        {
            name = "Renamed",
            emailProvided = true,
            phoneNumberProvided = true,
            urlImageProvided = true,
            password = "injected",
            passwordHash = "injected",
            tenantId = int.MaxValue
        });
        Assert.Equal(HttpStatusCode.OK, preserved.StatusCode);
        Assert.True(await preserved.Content.ReadFromJsonAsync<bool>());
        var afterOmission = await ReadAsync(tenant, id);
        Assert.Equal(original.Email, afterOmission.Email);
        Assert.Equal(original.PhoneNumber, afterOmission.PhoneNumber);
        Assert.Equal(original.UrlImage, afterOmission.UrlImage);
        Assert.Equal(original.PasswordHash, afterOmission.PasswordHash);
        Assert.Equal(tenant, afterOmission.TenantId);

        using var cleared = await client.PutAsJsonAsync($"/v1/users/{id}", new Dictionary<string, object?>
        {
            ["name"] = "Cleared",
            ["phoneNumber"] = null,
            ["urlImage"] = null,
            ["phoneNumberProvided"] = false,
            ["urlImageProvided"] = false
        });
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        var afterClear = await ReadAsync(tenant, id);
        Assert.Null(afterClear.PhoneNumber);
        Assert.Null(afterClear.UrlImage);
        Assert.Equal(original.Email, afterClear.Email);

        foreach (var email in new string?[] { null, "", " " })
        {
            using var invalid = await client.PutAsJsonAsync($"/v1/users/{id}", new { name = "Invalid", email });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        Assert.Equal("Cleared", (await ReadAsync(tenant, id)).Name);
        var detail = await client.GetFromJsonAsync<JsonElement>($"/v1/users/{id}");
        AssertFields(detail, UserMappingTests.DetailFields);
        var list = await client.GetFromJsonAsync<JsonElement>("/v1/users/");
        AssertFields(Assert.Single(list.EnumerateArray()), UserMappingTests.ListFields);
        var page = await client.GetFromJsonAsync<JsonElement>("/v1/users/paged?PageNumber=1&PageSize=10");
        AssertFields(page, ["Items", "TotalItems", "PageNumber", "PageSize", "TotalPages"]);
        AssertFields(Assert.Single(page.GetProperty("items").EnumerateArray()), UserMappingTests.ListFields);
    });

    [SqlServerIntegrationFact]
    public Task Boundaries_AcceptExactLimitsAndRejectOversizeOrUnsafeUrlsAtomically() => WithTenantAsync(async (tenant, client) =>
    {
        var email = new string('e', 64) + "@" + new string('a', 63) + "." + new string('b', 63) + "." + new string('c', 60) + ".io";
        Assert.Equal(256, email.Length);
        var url = "https://example.invalid/" + new string('x', 500 - "https://example.invalid/".Length);
        var id = await CreateAsync(client, new string('n', 150), email, new string('1', 50), url);
        var saved = await ReadAsync(tenant, id);
        Assert.Equal(email, saved.Email);
        Assert.Equal(50, saved.PhoneNumber!.Length);
        Assert.Equal(url, saved.UrlImage);
        using var validPut = await client.PutAsJsonAsync($"/v1/users/{id}", new { name = new string('m', 150), email, phoneNumber = new string('2', 50), urlImage = url });
        Assert.Equal(HttpStatusCode.OK, validPut.StatusCode);

        var invalidUpdates = new Dictionary<string, object?>[]
        {
            new() { ["name"] = new string('n', 151) },
            new() { ["email"] = "x" + email },
            new() { ["phoneNumber"] = new string('1', 51) },
            new() { ["urlImage"] = url + "x" },
            new() { ["urlImage"] = "/relative/avatar.png" },
            new() { ["urlImage"] = "ftp://example.invalid/avatar.png" },
            new() { ["urlImage"] = "https://user:secret@example.invalid/avatar.png" },
            new() { ["urlImage"] = "http://user@example.invalid/avatar.png" }
        };
        foreach (var fields in invalidUpdates)
        {
            fields.TryAdd("name", "Rejected update");
            using var response = await client.PutAsJsonAsync($"/v1/users/{id}", fields);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var persisted = await ReadAsync(tenant, id);
            Assert.Equal(new string('m', 150), persisted.Name);
            Assert.Equal(email, persisted.Email);
            Assert.Equal(url, persisted.UrlImage);

            fields["email"] = fields.TryGetValue("email", out var badEmail) ? badEmail : $"invalid-{Guid.NewGuid():N}@example.invalid";
            fields["password"] = "Synthetic1!";
            fields["confirmPassword"] = "Synthetic1!";
            using var create = await client.PostAsJsonAsync("/v1/users/", fields);
            Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        }
    });

    [SqlServerIntegrationFact]
    public Task EmailChanges_ResetConfirmationNormalizeAndRejectTenantDuplicatesWithoutPartialWrites() => WithTenantAsync(async (tenant, client) =>
    {
        var firstId = await CreateAsync(client, "First", "first@example.invalid");
        var secondId = await CreateAsync(client, "Second", "second@example.invalid");
        await WithContextAsync(tenant, async db =>
        {
            var first = await db.Users.SingleAsync(user => user.Id == firstId);
            first.ConfirmEmail(1);
            await db.SaveChangesAsync();
        });
        using var changed = await client.PutAsJsonAsync($"/v1/users/{firstId}", new { name = "Changed", email = "Changed@Example.Invalid" });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var saved = await ReadAsync(tenant, firstId);
        Assert.Equal("Changed@Example.Invalid", saved.Email);
        Assert.Equal("CHANGED@EXAMPLE.INVALID", saved.NormalizedEmail);
        Assert.False(saved.EmailConfirmed);
        using var conflict = await client.PutAsJsonAsync($"/v1/users/{secondId}", new { name = "Must not persist", email = "CHANGED@example.invalid", phoneNumber = "123" });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        var untouched = await ReadAsync(tenant, secondId);
        Assert.Equal("Second", untouched.Name);
        Assert.Equal("second@example.invalid", untouched.Email);
        Assert.Null(untouched.PhoneNumber);

        var otherFixture = new SqlTestTenantFixture();
        var (otherTenant, _) = await otherFixture.CreateAsync();
        try
        {
            using var other = CreateClient(otherTenant);
            await CreateAsync(other, "Other tenant", "CHANGED@example.invalid");
            using var hidden = await other.GetAsync($"/v1/users/{firstId}");
            Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        }
        finally { await otherFixture.DeleteAsync(otherTenant); }
    });

    [SqlServerIntegrationFact]
    public Task SqlUpdateConflict_AfterBothPrechecksMiss_RollsBackAllProfileChanges() => WithTenantAsync(async (tenant, client) =>
    {
        var firstId = await CreateAsync(client, "First", "first@example.invalid");
        var secondId = await CreateAsync(client, "Second", "second@example.invalid", "original-phone", "https://example.invalid/original");
        var originals = new[] { await ReadAsync(tenant, firstId), await ReadAsync(tenant, secondId) };
        var loserId = 0;
        await WithContextAsync(tenant, async firstDb => await WithContextAsync(tenant, async secondDb =>
        {
            var first = new UserDataRepository(firstDb);
            var second = new UserDataRepository(secondDb);
            const string email = "winner@example.invalid";
            Assert.False(await first.ExistsByEmailAsync(email, firstId, CancellationToken.None));
            Assert.False(await second.ExistsByEmailAsync(email, secondId, CancellationToken.None));
            var winner = await first.GetByIdAsync(firstId, CancellationToken.None);
            var loser = await second.GetByIdAsync(secondId, CancellationToken.None);
            winner!.Update("Winner", email, "winner-phone", "https://example.invalid/winner", 1);
            loser!.Update("Loser", email.ToUpperInvariant(), "loser-phone", "https://example.invalid/loser", 1);
            // Contextos e conexões independentes disputam o mesmo índice após ambos os prechecks.
            var outcomes = await Task.WhenAll(SaveAsync(first, winner), SaveAsync(second, loser));
            Assert.Single(outcomes, success => success);
            Assert.Single(outcomes, success => !success);
            loserId = outcomes[0] ? secondId : firstId;
        }));
        var unchanged = await ReadAsync(tenant, loserId);
        var original = originals.Single(user => user.Id == loserId);
        Assert.Equal(original.Name, unchanged.Name);
        Assert.Equal(original.Email, unchanged.Email);
        Assert.Equal(original.NormalizedEmail, unchanged.NormalizedEmail);
        Assert.Equal(original.PhoneNumber, unchanged.PhoneNumber);
        Assert.Equal(original.UrlImage, unchanged.UrlImage);
        Assert.Equal(original.PasswordHash, unchanged.PasswordHash);
        Assert.Equal(original.ModifiedAt, unchanged.ModifiedAt);
    });

    private static async Task<bool> SaveAsync(UserDataRepository repository, UserEntity entity)
    {
        try { Assert.True(await repository.UpdateAsync(entity, CancellationToken.None)); return true; }
        catch (UserEmailConflictException) { return false; }
    }

    [SqlServerIntegrationFact]
    public async Task GeneratedSwagger_ExposesExactReadSchemasAndHidesPresenceFlags()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        JsonElement document;
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // Testing não expõe o middleware; gera o mesmo documento pelo provider real.
            var swagger = factory.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
            using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            swagger.SerializeAsV3(new OpenApiJsonWriter(writer));
            document = JsonSerializer.Deserialize<JsonElement>(writer.ToString());
        }
        else
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            document = await response.Content.ReadFromJsonAsync<JsonElement>();
        }
        // Os nomes dos componentes são personalizados; resolve os contratos pelas próprias rotas.
        var paths = document.GetProperty("paths");
        var root = paths.EnumerateObject().Single(path => path.Name.TrimEnd('/') == "/v1/users").Value;
        var listSchema = ResolveSchema(document, root.GetProperty("get").GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        AssertFields(ResolveSchema(document, listSchema.GetProperty("items")).GetProperty("properties"), UserMappingTests.ListFields);
        var detail = ResolveSchema(document, paths.GetProperty("/v1/users/{id}").GetProperty("get").GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        AssertFields(detail.GetProperty("properties"), UserMappingTests.DetailFields);
        var page = ResolveSchema(document, paths.GetProperty("/v1/users/paged").GetProperty("get").GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        AssertFields(page.GetProperty("properties"), ["Items", "TotalItems", "PageNumber", "PageSize", "TotalPages"]);
        AssertFields(ResolveSchema(document, page.GetProperty("properties").GetProperty("items").GetProperty("items")).GetProperty("properties"), UserMappingTests.ListFields);
        var create = ResolveSchema(document, root.GetProperty("post").GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        var update = ResolveSchema(document, paths.GetProperty("/v1/users/{id}").GetProperty("put").GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        AssertFields(create.GetProperty("properties"), ["Name", "Email", "Password", "ConfirmPassword", "PhoneNumber", "UrlImage"]);
        AssertFields(update.GetProperty("properties"), ["Name", "Email", "PhoneNumber", "UrlImage"]);
        foreach (var schema in new[] { create, update })
        {
            Assert.Contains("name", schema.GetProperty("required").EnumerateArray().Select(value => value.GetString()));
            var properties = schema.GetProperty("properties");
            foreach (var (field, limit) in new[] { ("name", 150), ("email", 256), ("phoneNumber", 50), ("urlImage", 500) })
                Assert.Equal(limit, properties.GetProperty(field).GetProperty("maxLength").GetInt32());
        }
        Assert.Contains("email", create.GetProperty("required").EnumerateArray().Select(value => value.GetString()));
        Assert.DoesNotContain("email", update.GetProperty("required").EnumerateArray().Select(value => value.GetString()));
    }

    private static JsonElement ResolveSchema(JsonElement document, JsonElement schema)
    {
        while (true)
        {
            if (schema.TryGetProperty("$ref", out var reference))
            {
                var path = reference.GetString()!;
                Assert.StartsWith("#/components/schemas/", path);
                schema = document.GetProperty("components").GetProperty("schemas").GetProperty(path["#/components/schemas/".Length..].Replace("~1", "/").Replace("~0", "~"));
            }
            else if (!schema.TryGetProperty("properties", out _) && schema.TryGetProperty("allOf", out var allOf))
                schema = Assert.Single(allOf.EnumerateArray());
            else
                return schema;
        }
    }

    private async Task WithTenantAsync(Func<int, HttpClient, Task> test)
    {
        var fixture = new SqlTestTenantFixture();
        var (tenant, _) = await fixture.CreateAsync();
        try { using var client = CreateClient(tenant); await test(tenant, client); }
        finally { await fixture.DeleteAsync(tenant); }
    }

    private HttpClient CreateClient(int tenant)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Tenant", tenant.ToString(System.Globalization.CultureInfo.InvariantCulture));
        client.DefaultRequestHeaders.Add("X-Test-Permission-Resource", "Users");
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "Create,Update,GetBy,GetAll,GetPaged");
        return client;
    }

    private static async Task<int> CreateAsync(HttpClient client, string name, string email, string? phoneNumber = null, string? urlImage = null)
    {
        const string password = "Synthetic1!";
        using var response = await client.PostAsJsonAsync("/v1/users/", new { name, email, phoneNumber, urlImage, password, confirmPassword = password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static async Task<UserEntity> ReadAsync(int tenant, int id)
    {
        UserEntity? result = null;
        await WithContextAsync(tenant, async context => result = await context.Users.AsNoTracking().SingleAsync(user => user.Id == id && user.TenantId == tenant));
        return result!;
    }

    private static async Task WithContextAsync(int tenant, Func<GeritDbContext, Task> test)
    {
        await using var connection = new SqlConnection(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"));
        await connection.OpenAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SET QUOTED_IDENTIFIER ON; EXEC sys.sp_set_session_context @key=N'IsSuperAdmin', @value=0; EXEC sys.sp_set_session_context @key=N'TenantId', @value=@tenant;";
            command.Parameters.AddWithValue("@tenant", tenant);
            await command.ExecuteNonQueryAsync();
            await using var context = new GeritDbContext(new DbContextOptionsBuilder<GeritDbContext>().UseSqlServer(connection).Options);
            await test(context);
        }
        finally
        {
            try { await SqlRlsSessionContext.ResetAsync(connection, CancellationToken.None); }
            catch { SqlConnection.ClearPool(connection); throw; }
        }
    }

    private static void AssertFields(JsonElement value, string[] fields) =>
        Assert.Equal(fields.Select(JsonNamingPolicy.CamelCase.ConvertName).Order(), value.EnumerateObject().Select(property => property.Name).Order());
}
