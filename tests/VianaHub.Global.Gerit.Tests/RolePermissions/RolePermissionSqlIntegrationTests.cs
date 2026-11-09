using Microsoft.Data.SqlClient;
using VianaHub.Global.Gerit.Tests.Infrastructure;

namespace VianaHub.Global.Gerit.Tests.RolePermissions;

public sealed class RolePermissionSqlIntegrationTests
{
    [SqlServerIntegrationFact]
    public async Task SchemaIndexesForeignKeysAndRlsArePresent()
    {
        await using var db = await OpenAsync();
        Assert.Equal(1, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.tables WHERE object_id = OBJECT_ID(N'dbo.RolePermissions')"));
        Assert.Equal(1, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RolePermissions') AND name = N'UQ_RolePermissions' AND is_unique = 1 AND has_filter = 0"));
        Assert.Equal(1, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.index_columns ic JOIN sys.indexes i ON i.object_id=ic.object_id AND i.index_id=ic.index_id WHERE i.object_id=OBJECT_ID(N'dbo.RolePermissions') AND i.name=N'UQ_RolePermissions' AND ic.key_ordinal=4 AND COL_NAME(ic.object_id,ic.column_id)=N'ActionId'"));
        Assert.Equal(4, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.RolePermissions')"));
        Assert.Equal(4, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.security_predicates WHERE target_object_id = OBJECT_ID(N'dbo.RolePermissions')"));
        Assert.Equal(0, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM (SELECT TenantId,RoleId,ResourceId,ActionId FROM dbo.RolePermissions GROUP BY TenantId,RoleId,ResourceId,ActionId HAVING COUNT(*) > 1) d"));
        Assert.Equal(0, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM dbo.RolePermissions p LEFT JOIN dbo.Roles r ON r.Id=p.RoleId AND r.TenantId=p.TenantId WHERE r.Id IS NULL"));
        Assert.Equal(0, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM dbo.RolePermissions p LEFT JOIN dbo.Resources r ON r.Id=p.ResourceId WHERE r.Id IS NULL"));
        Assert.Equal(0, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM dbo.RolePermissions p LEFT JOIN dbo.Actions a ON a.Id=p.ActionId WHERE a.Id IS NULL"));
    }

    [SqlServerIntegrationFact]
    public async Task TenantRlsIsolatesRolePermissions()
    {
        var tenants = new SqlTestTenantFixture();
        var (tenantB, _) = await tenants.CreateAsync();
        var suffix = Guid.NewGuid().ToString("N");
        var roleA = 0;
        var roleB = 0;
        var resourceId = 0;
        var actionId = 0;

        try
        {
            await using var db = await OpenAsync();
            resourceId = await ScalarAsync<int>(db, "SELECT TOP (1) Id FROM dbo.Resources WHERE IsDeleted=0 ORDER BY Id");
            actionId = await ScalarAsync<int>(db, "SELECT TOP (1) Id FROM dbo.Actions WHERE IsDeleted=0 ORDER BY Id");
            roleA = await InsertRoleAsync(db, 1, $"TEST_ROLE_PERMISSION_A_{suffix}");
            roleB = await InsertRoleAsync(db, tenantB, $"TEST_ROLE_PERMISSION_B_{suffix}");
            await InsertPermissionAsync(db, 1, roleA, resourceId, actionId);
            await InsertPermissionAsync(db, tenantB, roleB, resourceId, actionId);

            Assert.Equal(1, await CountPermissionAsync(db, 1, roleA));
            Assert.Equal(0, await CountPermissionAsync(db, 1, roleB));
            Assert.Equal(1, await CountPermissionAsync(db, tenantB, roleB));
            Assert.Equal(0, await CountPermissionAsync(db, tenantB, roleA));
        }
        finally
        {
            await using var cleanup = await OpenAsync();
            await SetSuperAdminAsync(cleanup);
            if (roleA > 0)
                await ExecuteAsync(cleanup, "DELETE FROM dbo.RolePermissions WHERE TenantId=1 AND RoleId=@role", ("@role", roleA));
            if (roleB > 0)
                await ExecuteAsync(cleanup, "DELETE FROM dbo.RolePermissions WHERE TenantId=@tenant AND RoleId=@role", ("@tenant", tenantB), ("@role", roleB));
            if (roleA > 0)
                await ExecuteAsync(cleanup, "DELETE FROM dbo.Roles WHERE Id=@role", ("@role", roleA));
            if (roleB > 0)
                await ExecuteAsync(cleanup, "DELETE FROM dbo.Roles WHERE Id=@role", ("@role", roleB));
            await tenants.DeleteAsync(tenantB);
        }
    }

    private static async Task<SqlConnection> OpenAsync()
    {
        var db = new SqlConnection(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"));
        await db.OpenAsync();
        return db;
    }

    private static async Task<int> InsertRoleAsync(SqlConnection db, int tenantId, string name)
    {
        await SetTenantAsync(db, tenantId);
        return await ScalarAsync<int>(db, "INSERT INTO dbo.Roles (TenantId,Name,Description,CreatedBy) OUTPUT INSERTED.Id VALUES (@tenant,@name,N'RolePermissions SQL test',1)", ("@tenant", tenantId), ("@name", name));
    }

    private static async Task InsertPermissionAsync(SqlConnection db, int tenantId, int roleId, int resourceId, int actionId)
    {
        await SetTenantAsync(db, tenantId);
        await ExecuteAsync(db, "INSERT INTO dbo.RolePermissions (TenantId,RoleId,ResourceId,ActionId) VALUES (@tenant,@role,@resource,@action)", ("@tenant", tenantId), ("@role", roleId), ("@resource", resourceId), ("@action", actionId));
    }

    private static async Task<int> CountPermissionAsync(SqlConnection db, int tenantId, int roleId)
    {
        await SetTenantAsync(db, tenantId);
        return await ScalarAsync<int>(db, "SELECT COUNT(*) FROM dbo.RolePermissions WHERE RoleId=@role", ("@role", roleId));
    }

    private static async Task SetTenantAsync(SqlConnection db, int tenantId)
    {
        await ExecuteAsync(db, "EXEC sys.sp_set_session_context @key=N'TenantId', @value=@tenant; EXEC sys.sp_set_session_context @key=N'IsSuperAdmin', @value=0", ("@tenant", tenantId));
    }

    private static async Task SetSuperAdminAsync(SqlConnection db)
    {
        await ExecuteAsync(db, "EXEC sys.sp_set_session_context @key=N'IsSuperAdmin', @value=1");
    }

    private static async Task<T> ScalarAsync<T>(SqlConnection db, string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = db.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static async Task ExecuteAsync(SqlConnection db, string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = db.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync();
    }
}
