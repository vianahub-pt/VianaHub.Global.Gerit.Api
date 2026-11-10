using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Repository.Identity;
using VianaHub.Global.Gerit.Tests.Infrastructure;

namespace VianaHub.Global.Gerit.Tests.UserRoles;

public sealed class UserRoleBulkIntegrationTests
{
    [SqlServerIntegrationFact]
    public async Task CreateMany_PersistsBothAssociationsForEligibleParents()
    {
        var tenants = new SqlTestTenantFixture();
        var (tenantId, _) = await tenants.CreateAsync();
        try
        {
            await using var db = new SqlConnection(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"));
            await db.OpenAsync();
            await using var command = db.CreateCommand();
            command.Parameters.AddWithValue("@tenant", tenantId);
            command.Parameters.AddWithValue("@email", $"bulk-role-{Guid.NewGuid():N}@example.invalid");
            command.CommandText = """
                EXEC sys.sp_set_session_context @key=N'TenantId', @value=@tenant;
                EXEC sys.sp_set_session_context @key=N'IsSuperAdmin', @value=0;
                INSERT dbo.Users (TenantId, Name, Email, NormalizedEmail, PasswordHash, CreatedBy)
                OUTPUT INSERTED.Id VALUES (@tenant, N'TEST_BULK_ROLE_USER', @email, UPPER(@email), N'fixture-only', 1);
                """;
            try
            {
                var userId = Assert.IsType<int>(await command.ExecuteScalarAsync());
                command.CommandText = """
                    INSERT dbo.Roles (TenantId, Name, Description, CreatedBy)
                    OUTPUT INSERTED.Id VALUES
                        (@tenant, N'TEST_ROLE_BULK_A', N'Test only', 1),
                        (@tenant, N'TEST_ROLE_BULK_B', N'Test only', 1);
                    """;
                var ids = new List<int>();
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        ids.Add(reader.GetInt32(0));
                }
                Assert.Equal(2, ids.Count);
                await using var context = new GeritDbContext(
                    new DbContextOptionsBuilder<GeritDbContext>().UseSqlServer(db).Options);
                var repository = new UserRoleDataRepository(context);
                Assert.True(await repository.CreateManyAsync(
                    ids.Select(id => new UserRolesEntity(tenantId, userId, id)).ToArray(), CancellationToken.None));
                var persisted = await repository.GetByUserAsync(tenantId, userId, CancellationToken.None);
                Assert.Equal(ids.Order(), persisted.Select(association => association.RoleId));
            }
            finally
            {
                command.CommandText = "DELETE FROM dbo.UserRoles WHERE TenantId=@tenant; DELETE FROM dbo.Users WHERE TenantId=@tenant";
                await command.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            await tenants.DeleteAsync(tenantId);
        }
    }
}
