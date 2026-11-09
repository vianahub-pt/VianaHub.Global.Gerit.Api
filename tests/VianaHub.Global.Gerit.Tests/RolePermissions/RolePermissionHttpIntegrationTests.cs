using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using VianaHub.Global.Gerit.Tests.Infrastructure;

namespace VianaHub.Global.Gerit.Tests.RolePermissions;

[Collection("Roles SQL HTTP")]
public sealed class RolePermissionHttpIntegrationTests : IClassFixture<GeritSqlWebApplicationFactory>
{
    private readonly GeritSqlWebApplicationFactory _factory;

    public RolePermissionHttpIntegrationTests(GeritSqlWebApplicationFactory factory) => _factory = factory;

    [SqlServerIntegrationFact]
    public async Task SqlBackedLifecycleReturnsCompositeIdsForReadDeleteRoundtrip()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "GetAll,GetBy,Create,Delete");
        client.DefaultRequestHeaders.Add("X-Test-Permission-Resource", "RolePermissions");
        var roleId = 0;
        var roleName = $"TEST_ROLE_PERMISSION_HTTP_{Guid.NewGuid():N}";
        var (resourceId, actionId) = await ReadCatalogIdsAsync();

        try
        {
            using var roleClient = _factory.CreateClient();
            roleClient.DefaultRequestHeaders.Add("X-Test-Permissions", "Create,Delete");
            using var roleResponse = await roleClient.PostAsJsonAsync("/v1/roles/", new { name = roleName, description = "RolePermissions HTTP test" });
            Assert.Equal(HttpStatusCode.Created, roleResponse.StatusCode);
            roleId = (await roleResponse.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["id"];

            using var create = await client.PostAsJsonAsync("/v1/role-permissions", new { roleId, resourceId, actionId });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);

            var list = await client.GetFromJsonAsync<List<Dictionary<string, JsonElement>>>("/v1/role-permissions");
            Assert.NotNull(list);
            var item = Assert.Single(list!, x => x["roleId"].GetInt32() == roleId && x["resourceId"].GetInt32() == resourceId && x["actionId"].GetInt32() == actionId);

            var detailPath = $"/v1/role-permissions/roles/{roleId}/resources/{resourceId}/actions/{actionId}";
            using var detail = await client.GetAsync(detailPath);
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
            var detailBody = await detail.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
            Assert.Equal(roleId, detailBody!["roleId"].GetInt32());
            Assert.Equal(resourceId, detailBody["resourceId"].GetInt32());
            Assert.Equal(actionId, detailBody["actionId"].GetInt32());

            Assert.Equal(HttpStatusCode.OK, await GetStatusAsync(client, $"/v1/role-permissions/role/{roleId}"));
            Assert.Equal(HttpStatusCode.OK, await GetStatusAsync(client, $"/v1/role-permissions/resource/{resourceId}"));
            Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync(detailPath)).StatusCode);
            Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync(detailPath)).StatusCode);
        }
        finally
        {
            if (roleId > 0)
            {
                using var cleanupClient = _factory.CreateClient();
                cleanupClient.DefaultRequestHeaders.Add("X-Test-Permissions", "Delete");
                _ = await cleanupClient.DeleteAsync($"/v1/roles/{roleId}");
            }
        }
    }

    [SqlServerIntegrationFact]
    public async Task MissingPermissionReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "GetAll");
        client.DefaultRequestHeaders.Add("X-Test-Permission-Resource", "RolePermissions");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/v1/role-permissions", new { roleId = 1, resourceId = 1, actionId = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync("/v1/role-permissions/roles/1/resources/1/actions/1")).StatusCode);
    }

    [SqlServerIntegrationFact]
    public async Task AuthorizationCoversAllRolePermissionActions()
    {
        var (resourceId, actionId) = await ReadCatalogIdsAsync();
        var detailPath = "/v1/role-permissions/roles/999999999/resources/999999999/actions/999999999";

        using (var client = CreatePermissionClient("GetAll"))
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/v1/role-permissions")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(detailPath)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/v1/role-permissions", new { roleId = 1, resourceId, actionId })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync(detailPath)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await PostEmptyBulkAsync(client)).StatusCode);
        }

        using (var client = CreatePermissionClient("GetBy"))
            Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync(detailPath)).StatusCode);

        using (var client = CreatePermissionClient("Create"))
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/v1/role-permissions", new { roleId = 999999999, resourceId, actionId })).StatusCode);

        using (var client = CreatePermissionClient("Delete"))
            Assert.Equal(HttpStatusCode.Gone, (await client.DeleteAsync(detailPath)).StatusCode);

        using (var client = CreatePermissionClient("BulkUpload"))
            Assert.Equal(HttpStatusCode.BadRequest, (await PostEmptyBulkAsync(client)).StatusCode);
    }

    [SqlServerIntegrationFact]
    public async Task TenantABIsolationCoversReadAndDelete()
    {
        var tenants = new SqlTestTenantFixture();
        var (tenantB, _) = await tenants.CreateAsync();
        var roleA = 0;
        var roleB = 0;
        var (resourceId, actionId) = await ReadCatalogIdsAsync();
        try
        {
            roleA = await CreateRoleForTenantAsync(1, "TEST_ROLE_PERMISSION_TENANT_A");
            roleB = await CreateRoleForTenantAsync(tenantB, "TEST_ROLE_PERMISSION_TENANT_B");
            using var clientA = CreatePermissionClient("GetBy,Create,Delete", 1);
            using var clientB = CreatePermissionClient("GetBy,Create,Delete", tenantB);
            Assert.Equal(HttpStatusCode.Created, (await clientA.PostAsJsonAsync("/v1/role-permissions", new { roleId = roleA, resourceId, actionId })).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await clientB.PostAsJsonAsync("/v1/role-permissions", new { roleId = roleB, resourceId, actionId })).StatusCode);

            var pathA = $"/v1/role-permissions/roles/{roleA}/resources/{resourceId}/actions/{actionId}";
            var pathB = $"/v1/role-permissions/roles/{roleB}/resources/{resourceId}/actions/{actionId}";
            Assert.Equal(HttpStatusCode.OK, (await clientA.GetAsync(pathA)).StatusCode);
            Assert.Equal(HttpStatusCode.Gone, (await clientB.GetAsync(pathA)).StatusCode);
            Assert.Equal(HttpStatusCode.Gone, (await clientB.DeleteAsync(pathA)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await clientB.GetAsync(pathB)).StatusCode);
            Assert.Equal(HttpStatusCode.Gone, (await clientA.GetAsync(pathB)).StatusCode);
            Assert.Equal(HttpStatusCode.Gone, (await clientA.DeleteAsync(pathB)).StatusCode);
        }
        finally
        {
            using var cleanupA = CreatePermissionClient("Delete", 1);
            using var cleanupB = CreatePermissionClient("Delete", tenantB);
            if (roleA > 0)
                _ = await cleanupA.DeleteAsync($"/v1/role-permissions/roles/{roleA}/resources/{resourceId}/actions/{actionId}");
            if (roleB > 0)
                _ = await cleanupB.DeleteAsync($"/v1/role-permissions/roles/{roleB}/resources/{resourceId}/actions/{actionId}");
            await DeleteRoleForTenantAsync(roleA, 1);
            await DeleteRoleForTenantAsync(roleB, tenantB);
            await tenants.DeleteAsync(tenantB);
        }
    }

    [SqlServerIntegrationFact]
    public async Task ConcurrentCreateMapsUniqueConflictTo409()
    {
        using var client = CreatePermissionClient("Create,GetBy,Delete");
        var roleId = await CreateRoleForTenantAsync(1, $"TEST_ROLE_PERMISSION_RACE_{Guid.NewGuid():N}");
        var (resourceId, actionId) = await ReadCatalogIdsAsync();
        var path = "/v1/role-permissions";
        try
        {
            var responses = await Task.WhenAll(
                client.PostAsJsonAsync(path, new { roleId, resourceId, actionId }),
                client.PostAsJsonAsync(path, new { roleId, resourceId, actionId }));
            var statuses = responses.Select(response => response.StatusCode).OrderBy(status => status).ToArray();
            Assert.Equal(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }.OrderBy(status => status), statuses);
            foreach (var response in responses)
                Assert.DoesNotContain("UQ_RolePermissions", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            _ = await client.DeleteAsync($"/v1/role-permissions/roles/{roleId}/resources/{resourceId}/actions/{actionId}");
            await DeleteRoleForTenantAsync(roleId, 1);
        }
    }

    private async Task<(int ResourceId, int ActionId)> ReadCatalogIdsAsync()
    {
        await using var db = new SqlConnection(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"));
        await db.OpenAsync();
        await using var command = db.CreateCommand();
        command.CommandText = "EXEC sys.sp_set_session_context @key=N'IsSuperAdmin', @value=1; SELECT (SELECT TOP (1) Id FROM dbo.Resources WHERE IsDeleted=0 ORDER BY Id) AS ResourceId, (SELECT TOP (1) Id FROM dbo.Actions WHERE IsDeleted=0 ORDER BY Id) AS ActionId";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (reader.GetInt32(0), reader.GetInt32(1));
    }

    private HttpClient CreatePermissionClient(string permissions, int tenantId = 1)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", permissions);
        client.DefaultRequestHeaders.Add("X-Test-Permission-Resource", "RolePermissions");
        client.DefaultRequestHeaders.Add("X-Test-Tenant", tenantId.ToString());
        return client;
    }

    private async Task<int> CreateRoleForTenantAsync(int tenantId, string name)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "Create,Delete");
        client.DefaultRequestHeaders.Add("X-Test-Tenant", tenantId.ToString());
        using var response = await client.PostAsJsonAsync("/v1/roles/", new { name, description = "RolePermissions HTTP tenant test" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["id"];
    }

    private async Task DeleteRoleForTenantAsync(int roleId, int tenantId)
    {
        if (roleId <= 0)
            return;

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "Delete");
        client.DefaultRequestHeaders.Add("X-Test-Tenant", tenantId.ToString());
        _ = await client.DeleteAsync($"/v1/roles/{roleId}");
    }

    private static async Task<HttpResponseMessage> PostEmptyBulkAsync(HttpClient client)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(string.Empty), "file", "empty.csv");
        return await client.PostAsync("/v1/role-permissions/bulk-upload", form);
    }

    private static async Task<HttpStatusCode> GetStatusAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        return response.StatusCode;
    }
}
