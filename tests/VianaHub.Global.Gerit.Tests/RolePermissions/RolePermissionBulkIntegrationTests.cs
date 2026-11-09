using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using VianaHub.Global.Gerit.Tests.Infrastructure;

namespace VianaHub.Global.Gerit.Tests.RolePermissions;

[Collection("Roles SQL HTTP")]
public sealed class RolePermissionBulkIntegrationTests : IClassFixture<GeritSqlWebApplicationFactory>
{
    private readonly GeritSqlWebApplicationFactory _factory;

    public RolePermissionBulkIntegrationTests(GeritSqlWebApplicationFactory factory) => _factory = factory;

    [SqlServerIntegrationFact]
    public async Task ValidBulkIsAtomicAndPersistsAllRows()
    {
        using var client = CreateClient();
        var roleId = await CreateRoleAsync();
        var (resourceA, resourceB, actionId) = await ReadCatalogIdsAsync();
        try
        {
            using var response = await PostCsvAsync(client, $"RoleId;ResourceId;ActionId\n{roleId};{resourceA};{actionId}\n{roleId};{resourceB};{actionId}\n");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains($"\"roleId\":{roleId}", await client.GetStringAsync($"/v1/role-permissions/role/{roleId}"), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await CleanupPermissionAsync(client, roleId, resourceA, actionId);
            await CleanupPermissionAsync(client, roleId, resourceB, actionId);
            await CleanupRoleAsync(roleId);
        }
    }

    [SqlServerIntegrationFact]
    public async Task InternalDuplicateRejectsWholeBatchWithoutPartialWrite()
    {
        using var client = CreateClient();
        var roleId = await CreateRoleAsync();
        var (resourceId, _, actionId) = await ReadCatalogIdsAsync();
        try
        {
            using var response = await PostCsvAsync(client, $"RoleId;ResourceId;ActionId\n{roleId};{resourceId};{actionId}\n{roleId};{resourceId};{actionId}\n");
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var rows = await client.GetStringAsync($"/v1/role-permissions/role/{roleId}");
            Assert.DoesNotContain($"\"roleId\":{roleId}", rows, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await CleanupPermissionAsync(client, roleId, resourceId, actionId);
            await CleanupRoleAsync(roleId);
        }
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "GetAll,GetBy,Create,Delete,BulkUpload");
        client.DefaultRequestHeaders.Add("X-Test-Permission-Resource", "RolePermissions");
        return client;
    }

    private async Task<int> CreateRoleAsync()
    {
        var name = $"TEST_ROLE_PERMISSION_BULK_{Guid.NewGuid():N}";
        using var roleClient = _factory.CreateClient();
        roleClient.DefaultRequestHeaders.Add("X-Test-Permissions", "Create,Delete");
        using var response = await roleClient.PostAsJsonAsync("/v1/roles/", new { name, description = "RolePermissions bulk test" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["id"];
    }

    private async Task CleanupRoleAsync(int roleId)
    {
        using var roleClient = _factory.CreateClient();
        roleClient.DefaultRequestHeaders.Add("X-Test-Permissions", "Delete");
        _ = await roleClient.DeleteAsync($"/v1/roles/{roleId}");
    }

    private static async Task CleanupPermissionAsync(HttpClient client, int roleId, int resourceId, int actionId)
    {
        _ = await client.DeleteAsync($"/v1/role-permissions/roles/{roleId}/resources/{resourceId}/actions/{actionId}");
    }

    private static async Task<HttpResponseMessage> PostCsvAsync(HttpClient client, string csv)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(csv), "file", "role-permissions.csv");
        return await client.PostAsync("/v1/role-permissions/bulk-upload", form);
    }

    private static async Task<(int ResourceA, int ResourceB, int ActionId)> ReadCatalogIdsAsync()
    {
        await using var db = new SqlConnection(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"));
        await db.OpenAsync();
        await using var command = db.CreateCommand();
        command.CommandText = "EXEC sys.sp_set_session_context @key=N'IsSuperAdmin', @value=1; SELECT TOP (2) Id FROM dbo.Resources WHERE IsDeleted=0 ORDER BY Id; SELECT TOP (1) Id FROM dbo.Actions WHERE IsDeleted=0 ORDER BY Id";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var resourceA = reader.GetInt32(0);
        Assert.True(await reader.ReadAsync());
        var resourceB = reader.GetInt32(0);
        Assert.True(await reader.NextResultAsync());
        Assert.True(await reader.ReadAsync());
        return (resourceA, resourceB, reader.GetInt32(0));
    }
}
