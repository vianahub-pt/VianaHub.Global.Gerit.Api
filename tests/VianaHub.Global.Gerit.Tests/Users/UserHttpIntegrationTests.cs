using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Tests.Infrastructure;

namespace VianaHub.Global.Gerit.Tests.Users;

[Collection("Roles SQL HTTP")]
public sealed class UserHttpIntegrationTests(GeritSqlWebApplicationFactory factory)
    : IClassFixture<GeritSqlWebApplicationFactory>
{
    [SqlServerIntegrationFact]
    public async Task ConcurrentCreates_BothReachSqlAndOnlyOnePersists()
    {
        var tenants = new SqlTestTenantFixture();
        var (tenantId, _) = await tenants.CreateAsync();
        var gate = new UserInsertGate(tenantId);
        try
        {
            using var raceFactory = new GeritSqlWebApplicationFactory();
            using var app = raceFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                var original = services.Last(service => service.ServiceType == typeof(DbContextOptions<GeritDbContext>));
                services.AddScoped(provider =>
                {
                    var options = (DbContextOptions<GeritDbContext>)original.ImplementationFactory!(provider);
                    return new DbContextOptionsBuilder<GeritDbContext>(options).AddInterceptors(gate).Options;
                });
            }));
            using var client = app.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Tenant", tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            client.DefaultRequestHeaders.Add("X-Test-Permission-Resource", "Users");
            client.DefaultRequestHeaders.Add("X-Test-Permissions", "Create,GetAll");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US");
            var password = $"Aa1!{Guid.NewGuid():N}";
            const string email = "http-race@example.invalid";
            var firstRequest = client.PostAsJsonAsync("/v1/users/", new
            { name = "First", email, password, confirmPassword = password });
            var secondRequest = client.PostAsJsonAsync("/v1/users/", new
            { name = "Second", email = email.ToUpperInvariant(), password, confirmPassword = password });
            await Task.WhenAll(firstRequest, secondRequest);
            using var first = await firstRequest;
            using var second = await secondRequest;
            Assert.Equal(2, gate.Arrivals);
            Assert.Equal(1, gate.SqlConflicts);
            Assert.Equal(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict },
                new[] { first.StatusCode, second.StatusCode }.Order());
            var conflict = first.StatusCode == HttpStatusCode.Conflict ? first : second;
            var error = await conflict.Content.ReadAsStringAsync();
            Assert.Contains("A user with this email already exists", error, StringComparison.Ordinal);
            Assert.DoesNotContain("UX_Users", error, StringComparison.Ordinal);
            Assert.DoesNotContain("SqlException", error, StringComparison.Ordinal);
            Assert.DoesNotContain("Application.Service.User.", error, StringComparison.Ordinal);
            Assert.DoesNotContain(password, error, StringComparison.Ordinal);
            Assert.Single((await client.GetFromJsonAsync<System.Text.Json.JsonElement[]>("/v1/users/"))!);
        }
        finally
        {
            await DeleteTenantUsersAsync(tenants, tenantId);
        }
    }

    private sealed class UserInsertGate(int tenantId) : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;
        private int _sqlConflicts;
        public int Arrivals => _arrivals;
        public int SqlConflicts => _sqlConflicts;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context?.ChangeTracker.Entries<UserEntity>()
                .Any(entry => entry.State == EntityState.Added && entry.Entity.TenantId == tenantId) == true)
            {
                if (Interlocked.Increment(ref _arrivals) == 2)
                    _release.TrySetResult();
                await _release.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }

            return result;
        }

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            if (eventData.Exception.InnerException is SqlException { Number: 2601 or 2627 })
                Interlocked.Increment(ref _sqlConflicts);
            return Task.CompletedTask;
        }
    }

    [SqlServerIntegrationFact]
    public async Task BulkUpload_RejectsWholeBatchAndPersistsValidUtf8()
    {
        var tenants = new SqlTestTenantFixture();
        var (tenantId, _) = await tenants.CreateAsync();
        try
        {
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Tenant", tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            client.DefaultRequestHeaders.Add("X-Test-Permission-Resource", "Users");
            client.DefaultRequestHeaders.Add("X-Test-Permissions", "BulkUpload,GetAll");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US");
            const string header = "Name;Email;Password;PhoneNumber\n";
            const string valid = "João;bulk-one@example.invalid;Synthetic1!;\n";
            const string another = "Other;bulk-two@example.invalid;Synthetic1!;\n";
            const string duplicate = "Duplicate;BULK-ONE@example.invalid;Synthetic1!;\n";

            foreach (var (locale, phoneError, malformedError) in new[]
            {
                ("pt-PT", "Erro ao processar a linha 3 do CSV", "Erro ao ler o ficheiro CSV"),
                ("pt-BR", "Erro ao processar a linha 3 do CSV", "Erro ao ler o arquivo CSV"),
                ("en-US", "Error processing CSV line 3", "Error reading CSV file"),
                ("es-ES", "Error al procesar la línea 3 del CSV", "Error al leer el archivo CSV")
            })
            {
                client.DefaultRequestHeaders.Remove("Accept-Language");
                client.DefaultRequestHeaders.Add("Accept-Language", locale);
                using var unsafePhone = await UploadAsync(client, header + valid +
                    "Unsafe;unsafe@example.invalid;Synthetic1!;=cmd|test\n");
                await AssertLocalizedErrorAsync(unsafePhone, HttpStatusCode.BadRequest, phoneError);
                using var malformed = await UploadAsync(client, header + valid + "incomplete;row\n");
                await AssertLocalizedErrorAsync(malformed, HttpStatusCode.BadRequest, malformedError);
                Assert.Empty((await client.GetFromJsonAsync<System.Text.Json.JsonElement[]>("/v1/users/"))!);
            }
            using var duplicateInFile = await UploadAsync(client, header + valid + duplicate);
            Assert.Equal(HttpStatusCode.Conflict, duplicateInFile.StatusCode);
            Assert.Empty((await client.GetFromJsonAsync<System.Text.Json.JsonElement[]>("/v1/users/"))!);
            using var accepted = await UploadAsync(client, header + valid);
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
            using var duplicateInDb = await UploadAsync(client, header + another + duplicate);
            Assert.Equal(HttpStatusCode.Conflict, duplicateInDb.StatusCode);
            var error = await duplicateInDb.Content.ReadAsStringAsync();
            Assert.DoesNotContain("Application.Service.User.", error, StringComparison.Ordinal);
            Assert.DoesNotContain("UX_Users", error, StringComparison.Ordinal);
            Assert.DoesNotContain("Synthetic1!", error, StringComparison.Ordinal);
            var persisted = Assert.Single((await client.GetFromJsonAsync<System.Text.Json.JsonElement[]>("/v1/users/"))!);
            Assert.Equal("João", persisted.GetProperty("name").GetString());
        }
        finally
        {
            await DeleteTenantUsersAsync(tenants, tenantId);
        }
    }

    private static async Task AssertLocalizedErrorAsync(HttpResponseMessage response, HttpStatusCode status, string expected)
    {
        Assert.Equal(status, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<VianaHub.Global.Gerit.Application.Dtos.Base.ErrorResponse>();
        Assert.NotNull(error);
        var messages = error.Errors.Values.SelectMany(values => values).ToArray();
        Assert.Contains(expected, messages);
        Assert.DoesNotContain(messages, message => message.Contains("Application.Service.User.", StringComparison.Ordinal));
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string csv)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(csv, System.Text.Encoding.UTF8, "text/csv"), "file", "users.csv");
        return await client.PostAsync("/v1/users/bulk-upload", content);
    }

    [SqlServerIntegrationFact]
    public async Task AdministrativeRoutes_RequireRoleAndTheirSpecificPermission()
    {
        var routes = new (string Action, HttpMethod Method, string Path, HttpStatusCode Allowed)[]
        {
            ("GetAll", HttpMethod.Get, "/v1/users/", HttpStatusCode.OK),
            ("GetBy", HttpMethod.Get, "/v1/users/2147483647", HttpStatusCode.NotFound),
            ("GetPaged", HttpMethod.Get, "/v1/users/paged?PageNumber=1&PageSize=10", HttpStatusCode.OK),
            ("Create", HttpMethod.Post, "/v1/users/", HttpStatusCode.BadRequest),
            ("Update", HttpMethod.Put, "/v1/users/2147483647", HttpStatusCode.Gone),
            ("Activate", HttpMethod.Patch, "/v1/users/2147483647/activate", HttpStatusCode.Gone),
            ("Deactivate", HttpMethod.Patch, "/v1/users/2147483647/deactivate", HttpStatusCode.Gone),
            ("Delete", HttpMethod.Delete, "/v1/users/2147483647", HttpStatusCode.Gone),
            ("BulkUpload", HttpMethod.Post, "/v1/users/bulk-upload", HttpStatusCode.BadRequest)
        };
        using var client = factory.CreateClient();
        foreach (var route in routes)
        {
            foreach (var scenario in new[]
            {
                (Role: "BackOffice", Permission: route.Action, Expected: route.Allowed),
                (Role: "BackOffice", Permission: "Unrelated", Expected: HttpStatusCode.Forbidden),
                (Role: "User", Permission: route.Action, Expected: HttpStatusCode.Forbidden),
                (Role: "anonymous", Permission: route.Action, Expected: HttpStatusCode.Unauthorized)
            })
            {
                using var request = new HttpRequestMessage(route.Method, route.Path);
                request.Headers.Add("X-Test-Role", scenario.Role);
                request.Headers.Add("X-Test-Permission-Resource", "Users");
                request.Headers.Add("X-Test-Permissions", scenario.Permission);
                if (scenario.Role == "anonymous")
                    request.Headers.Add("X-Test-Anonymous", "1");
                if (route.Action == "Create")
                    request.Content = JsonContent.Create(new { });
                if (route.Action == "Update")
                    request.Content = JsonContent.Create(new { name = "Test" });
                using var response = await client.SendAsync(request);
                Assert.True(response.StatusCode == scenario.Expected,
                    $"{route.Action}, {scenario.Role}, {scenario.Permission}: expected {scenario.Expected}, got {response.StatusCode}");
            }
        }

        using var anonymous = new HttpRequestMessage(HttpMethod.Patch, "/v1/users/2147483647/password");
        anonymous.Headers.Add("X-Test-Anonymous", "1");
        anonymous.Content = JsonContent.Create(new { });
        using var rejected = await client.SendAsync(anonymous);
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
    }

    [SqlServerIntegrationFact]
    public async Task PasswordChange_RequiresOwnershipAndValidCurrentPassword()
    {
        var tenants = new SqlTestTenantFixture();
        var (tenantId, _) = await tenants.CreateAsync();
        try
        {
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Tenant", tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            client.DefaultRequestHeaders.Add("X-Test-Permission-Resource", "Users");
            client.DefaultRequestHeaders.Add("X-Test-Permissions", "Create,GetBy,Update,Delete");
            var password = $"Aa1!{Guid.NewGuid():N}";
            var email = $"test-{Guid.NewGuid():N}@example.invalid";
            var createRequest = new
            {
                name = "Password integration test",
                email,
                tenantId = int.MaxValue,
                password,
                confirmPassword = password
            };
            using var created = await client.PostAsJsonAsync("/v1/users/", createRequest);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var id = (await created.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["id"];
            foreach (var (locale, message) in new[]
            {
                ("pt-PT", "Já existe um utilizador com este e-mail"),
                ("pt-BR", "Já existe um usuário com este e-mail"),
                ("en-US", "A user with this email already exists"),
                ("es-ES", "Ya existe un usuario con este correo electrónico")
            })
            {
                using var duplicateRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/users/")
                {
                    Content = JsonContent.Create(createRequest with { email = email.ToUpperInvariant() })
                };
                duplicateRequest.Headers.Add("Accept-Language", locale);
                using var duplicate = await client.SendAsync(duplicateRequest);
                await AssertLocalizedErrorAsync(duplicate, HttpStatusCode.Conflict, message);
                var duplicateBody = await duplicate.Content.ReadAsStringAsync();
                Assert.DoesNotContain("Application.Service.User.", duplicateBody, StringComparison.Ordinal);
                Assert.DoesNotContain("UX_Users", duplicateBody, StringComparison.Ordinal);
            }
            using var detail = await client.GetAsync($"/v1/users/{id}");
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
            var body = await detail.Content.ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>();
            Assert.NotNull(body);
            Assert.DoesNotContain(body.Keys, key => key.Contains("password", StringComparison.OrdinalIgnoreCase));

            var (otherTenantId, _) = await tenants.CreateAsync();
            try
            {
                using var otherClient = factory.CreateClient();
                otherClient.DefaultRequestHeaders.Add("X-Test-Tenant", otherTenantId.ToString(System.Globalization.CultureInfo.InvariantCulture));
                otherClient.DefaultRequestHeaders.Add("X-Test-Permission-Resource", "Users");
                otherClient.DefaultRequestHeaders.Add("X-Test-Permissions", "GetBy,GetAll,GetPaged,Update,Activate,Deactivate,Delete");
                using var hidden = await otherClient.GetAsync($"/v1/users/{id}");
                Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
                using var update = await otherClient.PutAsJsonAsync($"/v1/users/{id}", new { name = "Cross-tenant update" });
                Assert.Equal(HttpStatusCode.Gone, update.StatusCode);
                using var crossPassword = await otherClient.PatchAsJsonAsync($"/v1/users/{id}/password",
                    new { currentPassword = password, newPassword = $"Cc3!{Guid.NewGuid():N}" });
                Assert.Equal(HttpStatusCode.Forbidden, crossPassword.StatusCode);
                foreach (var action in new[] { "activate", "deactivate" })
                {
                    using var lifecycle = await otherClient.PatchAsync($"/v1/users/{id}/{action}", null);
                    Assert.Equal(HttpStatusCode.Gone, lifecycle.StatusCode);
                }
                using var crossDelete = await otherClient.DeleteAsync($"/v1/users/{id}");
                Assert.Equal(HttpStatusCode.Gone, crossDelete.StatusCode);
                var all = await otherClient.GetFromJsonAsync<System.Text.Json.JsonElement[]>("/v1/users/");
                Assert.Empty(all!);
                var page = await otherClient.GetFromJsonAsync<System.Text.Json.JsonElement>("/v1/users/paged?PageNumber=1&PageSize=10");
                Assert.Empty(page.GetProperty("items").EnumerateArray());
            }
            finally
            {
                await tenants.DeleteAsync(otherTenantId);
            }

            client.DefaultRequestHeaders.Remove("X-Test-Permissions");
            client.DefaultRequestHeaders.Add("X-Test-Role", "none");
            client.DefaultRequestHeaders.Add("X-Test-User", int.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            var path = $"/v1/users/{id}/password";
            var nextPassword = $"Bb2!{Guid.NewGuid():N}";
            using var forbidden = await client.PatchAsJsonAsync(path, new { currentPassword = password, newPassword = nextPassword });
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
            using var deniedDetail = await client.GetAsync($"/v1/users/{id}");
            Assert.Equal(HttpStatusCode.Forbidden, deniedDetail.StatusCode);
            client.DefaultRequestHeaders.Remove("X-Test-Role");
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
            using var forbiddenAdmin = await client.PatchAsJsonAsync(path, new { currentPassword = password, newPassword = nextPassword });
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenAdmin.StatusCode);
            client.DefaultRequestHeaders.Remove("X-Test-Role");
            client.DefaultRequestHeaders.Add("X-Test-Role", "none");

            client.DefaultRequestHeaders.Remove("X-Test-User");
            client.DefaultRequestHeaders.Add("X-Test-User", id.ToString(System.Globalization.CultureInfo.InvariantCulture));
            using var weak = await client.PatchAsJsonAsync(path, new { currentPassword = password, newPassword = "weak" });
            Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
            using var invalid = await client.PatchAsJsonAsync(path, new { currentPassword = nextPassword, newPassword = password });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            using var changed = await client.PatchAsJsonAsync(path, new { currentPassword = password, newPassword = nextPassword });
            Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
            using var stale = await client.PatchAsJsonAsync(path, new { currentPassword = password, newPassword = nextPassword });
            Assert.Equal(HttpStatusCode.BadRequest, stale.StatusCode);

            client.DefaultRequestHeaders.Remove("X-Test-Role");
            client.DefaultRequestHeaders.Add("X-Test-Permissions", "GetBy,GetAll,GetPaged,Activate,Deactivate,Delete");
            foreach (var action in new[] { "deactivate", "activate" })
            {
                for (var repeat = 0; repeat < 2; repeat++)
                {
                    using var result = await client.PatchAsync($"/v1/users/{id}/{action}", null);
                    Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                }
                var current = await client.GetFromJsonAsync<System.Text.Json.JsonElement>($"/v1/users/{id}");
                Assert.Equal(action == "activate", current.GetProperty("isActive").GetBoolean());
            }
            using var deleted = await client.DeleteAsync($"/v1/users/{id}");
            Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
            foreach (var (locale, message) in new[]
            {
                ("pt-PT", "Utilizador não encontrado ou inativo"),
                ("pt-BR", "usuário não encontrado ou inativo"),
                ("en-US", "User not found or inactive"),
                ("es-ES", "Usuario no encontrado o inactivo")
            })
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/users/{id}");
                request.Headers.Add("Accept-Language", locale);
                using var deletedDetail = await client.SendAsync(request);
                await AssertLocalizedErrorAsync(deletedDetail, HttpStatusCode.Gone, message);
            }
            using var repeatedDelete = await client.DeleteAsync($"/v1/users/{id}");
            Assert.Equal(HttpStatusCode.Gone, repeatedDelete.StatusCode);
            using var deletedPassword = await client.PatchAsJsonAsync(path, new { currentPassword = nextPassword, newPassword = password });
            Assert.Equal(HttpStatusCode.Gone, deletedPassword.StatusCode);
            using var resurrect = await client.PatchAsync($"/v1/users/{id}/activate", null);
            Assert.Equal(HttpStatusCode.Gone, resurrect.StatusCode);
            Assert.Empty((await client.GetFromJsonAsync<System.Text.Json.JsonElement[]>("/v1/users/"))!);
            var deletedPage = await client.GetFromJsonAsync<System.Text.Json.JsonElement>("/v1/users/paged?PageNumber=1&PageSize=10");
            Assert.Empty(deletedPage.GetProperty("items").EnumerateArray());
        }
        finally
        {
            await DeleteTenantUsersAsync(tenants, tenantId);
        }
    }

    private static async Task DeleteTenantUsersAsync(SqlTestTenantFixture tenants, int tenantId)
    {
        await using var connection = new SqlConnection(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
                EXEC sys.sp_set_session_context @key=N'TenantId', @value=@tenant;
                DELETE FROM dbo.Users WHERE TenantId = @tenant;
                """;
        command.Parameters.AddWithValue("@tenant", tenantId);
        await command.ExecuteNonQueryAsync();
        await tenants.DeleteAsync(tenantId);
        command.CommandText = """
            EXEC sys.sp_set_session_context @key=N'IsSuperAdmin', @value=1;
            SELECT (SELECT COUNT(*) FROM dbo.Users WHERE TenantId=@tenant)
                 + (SELECT COUNT(*) FROM dbo.Tenants WHERE Id=@tenant);
            """;
        Assert.Equal(0, Assert.IsType<int>(await command.ExecuteScalarAsync()));
    }
}
