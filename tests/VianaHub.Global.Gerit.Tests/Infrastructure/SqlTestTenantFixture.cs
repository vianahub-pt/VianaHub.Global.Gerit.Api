using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;
using System.Data;
using VianaHub.Global.Gerit.Infra.Data.Security;

namespace VianaHub.Global.Gerit.Tests.Infrastructure;

public sealed class SqlTestTenantFixture
{
    private readonly string _connectionString;
    private readonly ConcurrentDictionary<int, string> _createdTenants = new();

    public SqlTestTenantFixture() : this(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING")
        ?? throw new InvalidOperationException("GERIT_RLS_TEST_CONNECTIONSTRING is required."))
    {
    }

    internal SqlTestTenantFixture(string connectionString) => _connectionString = connectionString;

    public async Task<(int Id, string Name)> CreateAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var name = $"TEST_TENANT_{suffix}";
        var connectionSettings = new SqlConnectionStringBuilder(_connectionString) { Encrypt = true };
        await using var db = new SqlConnection(connectionSettings.ConnectionString);
        await db.OpenAsync();
        try
        {
            await SetSuperAdminAsync(db);
            await using var command = db.CreateCommand();
            command.CommandText = "INSERT INTO dbo.Tenants (PartyTypeId, AcquisitionSourceTypeId, Name, Email, Note, CreatedBy, CreatedAt) OUTPUT INSERTED.Id VALUES (2, 8, @name, @email, N'Test tenant only', 1, SYSUTCDATETIME())";
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@email", $"{suffix}@example.invalid");
            var id = (int)(await command.ExecuteScalarAsync())!;
            _createdTenants[id] = name;
            return (id, name);
        }
        finally
        {
            await ResetSessionAsync(db);
        }
    }

    public async Task DeleteAsync(int tenantId)
    {
        // A propriedade é verificada antes de abrir a conexão ou remover dependências.
        if (!_createdTenants.TryGetValue(tenantId, out var expectedName))
            throw new InvalidOperationException("Only tenants created by this fixture instance can be deleted.");

        var connectionSettings = new SqlConnectionStringBuilder(_connectionString) { Encrypt = true };
        await using var db = new SqlConnection(connectionSettings.ConnectionString);
        await db.OpenAsync();
        try
        {
            await SetTenantAsync(db, tenantId, isSuperAdmin: true);
            await using var transaction = (SqlTransaction)await db.BeginTransactionAsync(IsolationLevel.Serializable);
            await using var command = db.CreateCommand();
            command.Transaction = transaction;
            command.Parameters.AddWithValue("@id", tenantId);
            command.Parameters.AddWithValue("@name", expectedName);
            // O bloqueio do pai impede novas dependências entre a verificação e o DELETE.
            command.CommandText = "SELECT Name FROM dbo.Tenants WITH (XLOCK, HOLDLOCK) WHERE Id = @id";
            var actualName = await command.ExecuteScalarAsync() as string;
            if (!string.Equals(expectedName, actualName, StringComparison.Ordinal))
                throw new InvalidOperationException("The tenant no longer matches the tenant created by this fixture instance.");

            // Lista explícita: dependências desconhecidas fazem a transação inteira reverter.
            command.CommandText = """
                DELETE FROM dbo.RolePermissions WHERE TenantId = @id;
                DELETE FROM dbo.UserRoles WHERE TenantId = @id;
                DELETE FROM dbo.UserPreferences WHERE TenantId = @id;
                DELETE FROM dbo.RefreshTokens WHERE TenantId = @id;
                DELETE FROM dbo.JwtKeys WHERE TenantId = @id;
                DELETE FROM dbo.Users WHERE TenantId = @id;
                DELETE FROM dbo.Roles WHERE TenantId = @id;
                """;
            await command.ExecuteNonQueryAsync();
            command.CommandText = "DELETE FROM dbo.Tenants WHERE Id = @id AND Name = @name";
            if (await command.ExecuteNonQueryAsync() != 1)
                throw new InvalidOperationException("The fixture tenant could not be deleted.");
            await transaction.CommitAsync();
            _createdTenants.TryRemove(tenantId, out _);
        }
        finally
        {
            // Dispose da transação reverte qualquer falha antes de devolver a sessão ao pool.
            await ResetSessionAsync(db);
        }
    }

    private static async Task ResetSessionAsync(SqlConnection db)
    {
        try
        {
            if (db.State == ConnectionState.Open)
                await SqlRlsSessionContext.ResetAsync(db, CancellationToken.None);
            else
                SqlConnection.ClearPool(db);
        }
        catch
        {
            SqlConnection.ClearPool(db);
            throw;
        }
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
