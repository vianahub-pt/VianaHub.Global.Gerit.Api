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

    private static async Task<HttpStatusCode> GetStatusAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        return response.StatusCode;
    }
}
