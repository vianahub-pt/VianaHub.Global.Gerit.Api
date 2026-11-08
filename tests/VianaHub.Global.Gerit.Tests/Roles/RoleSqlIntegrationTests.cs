using Microsoft.Data.SqlClient;
using VianaHub.Global.Gerit.Tests.Infrastructure;

namespace VianaHub.Global.Gerit.Tests.Roles;

public sealed class RoleSqlServerFactAttribute : FactAttribute
{
    public RoleSqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING")))
            Skip = "GERIT_RLS_TEST_CONNECTIONSTRING unavailable";
    }
}

public sealed class RoleSqlIntegrationTests
{
    [RoleSqlServerFact]
    public async Task RolesSchemaIndexesForeignKeysAndRlsArePresent()
    {
        await using var db = new SqlConnection(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"));
        await db.OpenAsync();
        Assert.Equal(1, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.tables WHERE object_id = OBJECT_ID(N'dbo.Roles')"));
        Assert.Equal(1, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Roles') AND name = N'TenantId' AND is_nullable = 0"));
        Assert.Equal(1, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Roles') AND name = N'Name' AND max_length = 200 AND is_nullable = 0"));
        Assert.Equal(1, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Roles') AND name = N'Description' AND max_length = 1000 AND is_nullable = 0"));
        Assert.Equal(1, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Roles') AND name = N'UX_Roles_Tenant_Name' AND has_filter = 1"));
        Assert.Equal(1, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.Roles') AND referenced_object_id = OBJECT_ID(N'dbo.Tenants')"));
        Assert.True(await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.security_predicates WHERE target_object_id = OBJECT_ID(N'dbo.Roles')") >= 4);
        Assert.Equal(1, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.RolePermissions') AND referenced_object_id = OBJECT_ID(N'dbo.Roles')"));
        Assert.Equal(1, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.UserRoles') AND referenced_object_id = OBJECT_ID(N'dbo.Roles')"));
    }

    [RoleSqlServerFact]
    public async Task TenantSessionContextIsolatesRoleRowsAcrossTenants()
    {
        var tenants = new SqlTestTenantFixture();
        var (tenantB, _) = await tenants.CreateAsync();
        var suffix = Guid.NewGuid().ToString("N");
        var roleAName = $"TEST_ROLE_RLS_A_{suffix}";
        var roleBName = $"TEST_ROLE_RLS_B_{suffix}";
        var roleA = 0;
        var roleB = 0;

        try
        {
            await using var db = new SqlConnection(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"));
            await db.OpenAsync();
            roleA = await InsertRoleAsync(db, 1, roleAName);
            roleB = await InsertRoleAsync(db, tenantB, roleBName);

            Assert.Equal(1, await CountVisibleAsync(db, 1, roleAName));
            Assert.Equal(0, await CountVisibleAsync(db, 1, roleBName));
            Assert.Equal(1, await CountVisibleAsync(db, tenantB, roleBName));
            Assert.Equal(0, await CountVisibleAsync(db, tenantB, roleAName));

            await DeleteRoleAsync(db, 1, roleA);
            await DeleteRoleAsync(db, tenantB, roleB);
        }
        finally
        {
            await tenants.DeleteAsync(tenantB);
        }
    }

    [RoleSqlServerFact]
    public async Task RoleDependenciesRemainOnSoftDeleteAndBlockPhysicalDelete()
    {
        var connectionString = Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING");
        await using var db = new SqlConnection(connectionString);
        await db.OpenAsync();
        await SetSuperAdminTenantAsync(db, 1);
        var roleId = await InsertRoleAsync(db, 1, $"TEST_ROLE_DEP_{Guid.NewGuid():N}");
        var resourceId = await ScalarAsync<int>(db, "SELECT TOP (1) Id FROM dbo.Resources WHERE IsDeleted = 0 ORDER BY Id");
        var actionId = await ScalarAsync<int>(db, "SELECT TOP (1) Id FROM dbo.Actions WHERE IsDeleted = 0 ORDER BY Id");
        var userId = await ScalarAsync<int>(db, "SELECT TOP (1) Id FROM dbo.Users WHERE TenantId = 1 AND IsDeleted = 0 ORDER BY Id");

        try
        {
            await ExecuteAsync(db, "INSERT INTO dbo.RolePermissions (TenantId, RoleId, ResourceId, ActionId) VALUES (1, @role, @resource, @action); INSERT INTO dbo.UserRoles (TenantId, UserId, RoleId) VALUES (1, @user, @role)",
                ("@role", roleId), ("@resource", resourceId), ("@action", actionId), ("@user", userId));
            await ExecuteAsync(db, "UPDATE dbo.Roles SET IsActive = 0, IsDeleted = 1 WHERE Id = @role", ("@role", roleId));
            Assert.Equal(1, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM dbo.RolePermissions WHERE RoleId = @role", ("@role", roleId)));
            Assert.Equal(1, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM dbo.UserRoles WHERE RoleId = @role", ("@role", roleId)));
            await SetSuperAdminTenantAsync(db, 1);
            await Assert.ThrowsAsync<SqlException>(() => ExecuteAsync(db, "DELETE FROM dbo.Roles WHERE Id = @role", ("@role", roleId)));
        }
        finally
        {
            await ExecuteAsync(db, "DELETE FROM dbo.RolePermissions WHERE RoleId = @role; DELETE FROM dbo.UserRoles WHERE RoleId = @role; DELETE FROM dbo.Roles WHERE Id = @role", ("@role", roleId));
        }
    }

    private static async Task<int> InsertRoleAsync(SqlConnection db, int tenantId, string name)
    {
        await SetTenantAsync(db, tenantId);
        await using var command = db.CreateCommand();
        command.CommandText = "INSERT INTO dbo.Roles (TenantId, Name, Description, CreatedBy) OUTPUT INSERTED.Id VALUES (@tenant, @name, N'RLS test role', 1)";
        command.Parameters.AddWithValue("@tenant", tenantId);
        command.Parameters.AddWithValue("@name", name);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<int> CountVisibleAsync(SqlConnection db, int tenantId, string name)
    {
        await SetTenantAsync(db, tenantId);
        await using var command = db.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM dbo.Roles WHERE Name = @name";
        command.Parameters.AddWithValue("@name", name);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private static async Task DeleteRoleAsync(SqlConnection db, int tenantId, int roleId)
    {
        await SetTenantAsync(db, tenantId);
        await using var command = db.CreateCommand();
        command.CommandText = "DELETE FROM dbo.Roles WHERE Id = @id";
        command.Parameters.AddWithValue("@id", roleId);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task SetTenantAsync(SqlConnection db, int tenantId)
    {
        await using var command = db.CreateCommand();
        command.CommandText = "SET QUOTED_IDENTIFIER ON; EXEC sys.sp_set_session_context @key=N'TenantId', @value=@tenant; EXEC sys.sp_set_session_context @key=N'IsSuperAdmin', @value=0";
        command.Parameters.AddWithValue("@tenant", tenantId);
        await command.ExecuteNonQueryAsync();
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

    private static async Task SetSuperAdminTenantAsync(SqlConnection db, int tenantId)
    {
        await using var command = db.CreateCommand();
        command.CommandText = "SET QUOTED_IDENTIFIER ON; EXEC sys.sp_set_session_context @key=N'TenantId', @value=@tenant; EXEC sys.sp_set_session_context @key=N'IsSuperAdmin', @value=1";
        command.Parameters.AddWithValue("@tenant", tenantId);
        await command.ExecuteNonQueryAsync();
    }
}
