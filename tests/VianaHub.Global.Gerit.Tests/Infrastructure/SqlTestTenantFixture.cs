using Microsoft.Data.SqlClient;

namespace VianaHub.Global.Gerit.Tests.Infrastructure;

public sealed class SqlTestTenantFixture
{
    private readonly string _connectionString;

    public SqlTestTenantFixture()
    {
        _connectionString = Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING")
            ?? throw new InvalidOperationException("GERIT_RLS_TEST_CONNECTIONSTRING is required.");
    }

    public async Task<(int Id, string Name)> CreateAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var name = $"TEST_TENANT_{suffix}";
        await using var db = new SqlConnection(_connectionString);
        await db.OpenAsync();
        await SetSuperAdminAsync(db);
        await using var command = db.CreateCommand();
        command.CommandText = "INSERT INTO dbo.Tenants (PartyTypeId, AcquisitionSourceTypeId, Name, Email, Note, CreatedBy, CreatedAt) OUTPUT INSERTED.Id VALUES (2, 8, @name, @email, N'Test tenant only', 1, SYSUTCDATETIME())";
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@email", $"{suffix}@example.invalid");
        return ((int)(await command.ExecuteScalarAsync())!, name);
    }

    public async Task DeleteAsync(int tenantId)
    {
        await using var db = new SqlConnection(_connectionString);
        await db.OpenAsync();
        await SetTenantAsync(db, tenantId, isSuperAdmin: false);
        await using (var tenantCommand = db.CreateCommand())
        {
            tenantCommand.CommandText = "DELETE FROM dbo.RolePermissions WHERE TenantId = @id; DELETE FROM dbo.UserRoles WHERE TenantId = @id; DELETE FROM dbo.Roles WHERE TenantId = @id AND Name LIKE N'TEST_ROLE_%'";
            tenantCommand.Parameters.AddWithValue("@id", tenantId);
            await tenantCommand.ExecuteNonQueryAsync();
        }

        await SetTenantAsync(db, tenantId, isSuperAdmin: true);
        await using var tenantDeleteCommand = db.CreateCommand();
        tenantDeleteCommand.CommandText = "DELETE FROM dbo.Tenants WHERE Id = @id AND Name LIKE N'TEST_TENANT_%'";
        tenantDeleteCommand.Parameters.AddWithValue("@id", tenantId);
        await tenantDeleteCommand.ExecuteNonQueryAsync();
    }

    private static async Task SetTenantAsync(SqlConnection db, int tenantId, bool isSuperAdmin)
    {
        await using var command = db.CreateCommand();
        command.CommandText = "SET QUOTED_IDENTIFIER ON; EXEC sys.sp_set_session_context @key=N'TenantId', @value=@tenant; EXEC sys.sp_set_session_context @key=N'IsSuperAdmin', @value=@superAdmin";
        command.Parameters.AddWithValue("@tenant", tenantId);
        command.Parameters.AddWithValue("@superAdmin", isSuperAdmin ? 1 : 0);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task SetSuperAdminAsync(SqlConnection db)
    {
        await using var command = db.CreateCommand();
        command.CommandText = "EXEC sys.sp_set_session_context @key=N'IsSuperAdmin', @value=1";
        await command.ExecuteNonQueryAsync();
    }
}
