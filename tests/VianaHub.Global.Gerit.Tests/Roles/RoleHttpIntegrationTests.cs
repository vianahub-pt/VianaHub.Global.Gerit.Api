using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using VianaHub.Global.Gerit.Tests.Infrastructure;

namespace VianaHub.Global.Gerit.Tests.Roles;

[Collection("Roles SQL HTTP")]
public sealed class RoleHttpIntegrationTests : IClassFixture<GeritSqlWebApplicationFactory>
{
    private readonly GeritSqlWebApplicationFactory _factory;

    public RoleHttpIntegrationTests(GeritSqlWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task SqlBackedPipelineCreatesReadsAndCleansRole()
    {
        using var client = _factory.CreateClient();
        var name = $"TEST_ROLE_{Guid.NewGuid():N}";
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "Roles:GetAll,GetBy,Create,Delete");
        var id = 0;

        try
        {
            using var response = await client.PostAsJsonAsync("/v1/roles/", new { name, description = "SQL-backed proof" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
            Assert.NotNull(created);
            id = created!["id"];

            using var detail = await client.GetAsync($"/v1/roles/{id}");
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);

            using var deleted = await client.DeleteAsync($"/v1/roles/{id}");
            Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);

            using var gone = await client.GetAsync($"/v1/roles/{id}");
            Assert.Equal(HttpStatusCode.Gone, gone.StatusCode);
        }
        finally
        {
            if (id > 0)
            {
                using var cleanup = await client.DeleteAsync($"/v1/roles/{id}");
            }
        }

    }

    [Fact]
    public async Task SqlBackedRoleLifecycleAndPaginationAreIdempotent()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "GetAll,GetBy,GetPaged,Create,Update,Activate,Deactivate,Delete");
        var name = $"TEST_ROLE_{Guid.NewGuid():N}";
        var id = 0;

        try
        {
            using var created = await client.PostAsJsonAsync("/v1/roles/", new { name, description = "Original" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            id = (await created.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["id"];

            var detail = await client.GetStringAsync($"/v1/roles/{id}");
            Assert.Contains(name, detail, StringComparison.Ordinal);
            Assert.Contains("Original", detail, StringComparison.Ordinal);

            using var paged = await client.GetAsync($"/v1/roles/paged?PageNumber=1&PageSize=10&Search={name}");
            Assert.Equal(HttpStatusCode.OK, paged.StatusCode);
            Assert.Contains(name, await paged.Content.ReadAsStringAsync(), StringComparison.Ordinal);

            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/v1/roles/{id}", new { description = "Updated" })).StatusCode);
            Assert.Contains("Updated", await client.GetStringAsync($"/v1/roles/{id}"), StringComparison.Ordinal);

            Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/roles/{id}/deactivate", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/roles/{id}/deactivate", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/roles/{id}/activate", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/roles/{id}/activate", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/v1/roles/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/roles/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/roles/{id}/activate", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/roles/{id}/deactivate", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Gone, (await client.DeleteAsync($"/v1/roles/{id}")).StatusCode);
            Assert.DoesNotContain(name, await client.GetStringAsync("/v1/roles/"), StringComparison.Ordinal);
            Assert.DoesNotContain(name, await client.GetStringAsync($"/v1/roles/paged?PageNumber=1&PageSize=10&Search={name}"), StringComparison.Ordinal);

            using var reused = await client.PostAsJsonAsync("/v1/roles/", new { name, description = "Reused after soft delete" });
            Assert.Equal(HttpStatusCode.Created, reused.StatusCode);
            var reusedId = (await reused.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["id"];
            using var reusedCleanup = await client.DeleteAsync($"/v1/roles/{reusedId}");
        }
        finally
        {
            if (id > 0)
            {
                using var cleanup = await client.DeleteAsync($"/v1/roles/{id}");
            }
        }
    }

    [Fact]
    public async Task ValidationDuplicateNormalizationAndAuthorizationAreEnforced()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "Create,Delete");
        var name = $"TEST_ROLE_{Guid.NewGuid():N}";
        var id = 0;

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/v1/roles/", new { name })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/v1/roles/", new { name = "Long", description = new string('x', 256) })).StatusCode);

        try
        {
            using var created = await client.PostAsJsonAsync("/v1/roles/", new { name, description = "Description" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            id = (await created.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["id"];
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/v1/roles/", new { name = $" {name} ", description = "Duplicate" })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/v1/roles/", new { name = name.ToLowerInvariant(), description = "Duplicate" })).StatusCode);

            using var denied = _factory.CreateClient();
            denied.DefaultRequestHeaders.Add("X-Test-Permissions", "GetAll");
            Assert.Equal(HttpStatusCode.Forbidden, (await denied.PostAsJsonAsync("/v1/roles/", new { name = $"TEST_ROLE_DENIED_{Guid.NewGuid():N}", description = "Denied" })).StatusCode);

            using var deniedRead = _factory.CreateClient();
            deniedRead.DefaultRequestHeaders.Add("X-Test-Permissions", "Create");
            Assert.Equal(HttpStatusCode.Forbidden, (await deniedRead.GetAsync($"/v1/roles/{id}")).StatusCode);

            using var deniedDeactivate = _factory.CreateClient();
            deniedDeactivate.DefaultRequestHeaders.Add("X-Test-Permissions", "Create");
            Assert.Equal(HttpStatusCode.Forbidden, (await deniedDeactivate.PatchAsync($"/v1/roles/{id}/deactivate", null)).StatusCode);

            using var deniedDelete = _factory.CreateClient();
            deniedDelete.DefaultRequestHeaders.Add("X-Test-Permissions", "Create");
            Assert.Equal(HttpStatusCode.Forbidden, (await deniedDelete.DeleteAsync($"/v1/roles/{id}")).StatusCode);
        }
        finally
        {
            if (id > 0)
            {
                using var cleanup = await client.DeleteAsync($"/v1/roles/{id}");
            }
        }
    }

    [Fact]
    public void FactoryUsesSqlServerProvider()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<VianaHub.Global.Gerit.Infra.Data.Context.GeritDbContext>();
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
    }

    [Fact]
    public async Task TemporaryTenantIsolationAndSameNameAreEnforced()
    {
        var tenants = new SqlTestTenantFixture();
        var (tenantB, _) = await tenants.CreateAsync();
        var sharedName = $"TEST_ROLE_SHARED_{Guid.NewGuid():N}";
        var roleA = 0;
        var roleB = 0;

        try
        {
            using var clientA = _factory.CreateClient();
            clientA.DefaultRequestHeaders.Add("X-Test-Permissions", "GetBy,Create,Update,Activate,Deactivate,Delete");
            using var createA = await clientA.PostAsJsonAsync("/v1/roles/", new { name = sharedName, description = "Tenant A" });
            Assert.Equal(HttpStatusCode.Created, createA.StatusCode);
            roleA = (await createA.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["id"];

            using var clientB = _factory.CreateClient();
            clientB.DefaultRequestHeaders.Add("X-Test-Tenant", tenantB.ToString());
            clientB.DefaultRequestHeaders.Add("X-Test-Permissions", "GetBy,Create,Update,Activate,Deactivate,Delete");
            using var createB = await clientB.PostAsJsonAsync("/v1/roles/", new { name = sharedName, description = "Tenant B" });
            Assert.Equal(HttpStatusCode.Created, createB.StatusCode);
            roleB = (await createB.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["id"];

            Assert.Equal(HttpStatusCode.Gone, await GetStatusAsync(clientB, $"/v1/roles/{roleA}"));
            Assert.Equal(HttpStatusCode.Gone, await GetStatusAsync(clientB, $"/v1/roles/{roleA}", HttpMethod.Put));
            Assert.Equal(HttpStatusCode.Gone, await GetStatusAsync(clientB, $"/v1/roles/{roleA}/activate", HttpMethod.Patch));
            Assert.Equal(HttpStatusCode.Gone, await GetStatusAsync(clientB, $"/v1/roles/{roleA}", HttpMethod.Delete));
        }
        finally
        {
            if (roleA > 0)
            {
                using var cleanupAClient = _factory.CreateClient();
                cleanupAClient.DefaultRequestHeaders.Add("X-Test-Permissions", "Delete");
                using var cleanupA = await cleanupAClient.DeleteAsync($"/v1/roles/{roleA}");
            }

            if (roleB > 0)
            {
                using var cleanupB = _factory.CreateClient();
                cleanupB.DefaultRequestHeaders.Add("X-Test-Tenant", tenantB.ToString());
                cleanupB.DefaultRequestHeaders.Add("X-Test-Permissions", "Delete");
                using var deletedB = await cleanupB.DeleteAsync($"/v1/roles/{roleB}");
            }

            await tenants.DeleteAsync(tenantB);
        }
    }

    [Fact]
    public async Task ConcurrentCreateReturnsSingleSuccessAndLocalizedConflict()
    {
        var name = $"TEST_ROLE_RACE_{Guid.NewGuid():N}";
        using var clientA = _factory.CreateClient();
        using var clientB = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Add("X-Test-Permissions", "Create,Delete");
        clientB.DefaultRequestHeaders.Add("X-Test-Permissions", "Create,Delete");

        using var first = new HttpRequestMessage(HttpMethod.Post, "/v1/roles/")
        {
            Content = JsonContent.Create(new { name, description = "Race A" })
        };
        using var second = new HttpRequestMessage(HttpMethod.Post, "/v1/roles/")
        {
            Content = JsonContent.Create(new { name, description = "Race B" })
        };

        var responses = await Task.WhenAll(clientA.SendAsync(first), clientB.SendAsync(second));
        var statuses = responses.Select(response => response.StatusCode).OrderBy(status => status).ToArray();
        Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], statuses);

        foreach (var response in responses)
        {
            if (response.StatusCode != HttpStatusCode.Created)
            {
                var conflictBody = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain("Application.Service.Role.Create.ResourceAlreadyExists", conflictBody, StringComparison.Ordinal);
                continue;
            }

            var created = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
            Assert.NotNull(created);
            using var cleanup = await clientA.DeleteAsync($"/v1/roles/{created!["id"]}");
        }

        foreach (var response in responses)
            response.Dispose();
    }

    private static async Task<HttpStatusCode> GetStatusAsync(HttpClient client, string uri, HttpMethod? method = null)
    {
        using var request = new HttpRequestMessage(method ?? HttpMethod.Get, uri);
        if (method == HttpMethod.Put)
            request.Content = JsonContent.Create(new { description = "cross tenant" });
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }
}
