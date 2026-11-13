using Microsoft.Data.SqlClient;
using VianaHub.Global.Gerit.Infra.Data.Security;

namespace VianaHub.Global.Gerit.Tests.Infrastructure;

[Collection("Roles SQL HTTP")]
public sealed class SqlTestTenantFixtureIntegrationTests
{
    [SqlServerIntegrationFact]
    public async Task DeleteAsync_RemovesJwtKeyAndTenantCreatedByThisFixtureOnly()
    {
        var fixture = new SqlTestTenantFixture();
        var (tenantId, _) = await fixture.CreateAsync();
        var removed = false;
        await using var connection = new SqlConnection(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"));
        await connection.OpenAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.Parameters.AddWithValue("@tenant", tenantId);
            command.Parameters.AddWithValue("@key", Guid.NewGuid());
            command.CommandText = """
                EXEC sys.sp_set_session_context @key=N'IsSuperAdmin', @value=0;
                EXEC sys.sp_set_session_context @key=N'TenantId', @value=@tenant;
                INSERT dbo.JwtKeys (TenantId, KeyId, PublicKey, PrivateKeyEncrypted, ExpiresAt, NextRotationAt, CreatedBy)
                VALUES (@tenant, @key, N'fixture-public-placeholder', N'fixture-not-a-private-key',
                    DATEADD(day, 90, SYSUTCDATETIME()), DATEADD(day, 83, SYSUTCDATETIME()), 1);
                SELECT COUNT(*) FROM dbo.JwtKeys WHERE TenantId=@tenant AND KeyId=@key;
                """;
            Assert.Equal(1, Assert.IsType<int>(await command.ExecuteScalarAsync()));
            // Conhecer o ID de outro teste não autoriza esta instância a removê-lo.
            await Assert.ThrowsAsync<InvalidOperationException>(() => new SqlTestTenantFixture().DeleteAsync(tenantId));
            command.CommandText = "SELECT COUNT(*) FROM dbo.Tenants WHERE Id=@tenant";
            Assert.Equal(1, Assert.IsType<int>(await command.ExecuteScalarAsync()));
            await fixture.DeleteAsync(tenantId);
            removed = true;
            command.CommandText = """
                SELECT (SELECT COUNT(*) FROM dbo.JwtKeys WHERE TenantId=@tenant AND KeyId=@key)
                     + (SELECT COUNT(*) FROM dbo.Tenants WHERE Id=@tenant);
                """;
            Assert.Equal(0, Assert.IsType<int>(await command.ExecuteScalarAsync()));
        }
        finally
        {
            try
            {
                if (!removed)
                    await fixture.DeleteAsync(tenantId);
            }
            finally
            {
                try
                {
                    await SqlRlsSessionContext.ResetAsync(connection, CancellationToken.None);
                }
                catch
                {
                    SqlConnection.ClearPool(connection);
                    throw;
                }
            }
        }
    }
}
