using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Exceptions;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Repository.Identity;
using VianaHub.Global.Gerit.Tests.Infrastructure;

namespace VianaHub.Global.Gerit.Tests.Users;

public sealed class UserBulkIntegrationTests
{
    [SqlServerIntegrationFact]
    public Task CreateMany_RollsBackEntireBatchOnNormalizedEmailConflict() =>
        WithTenantAsync(async (options, tenantId) =>
        {
            await using var context = new GeritDbContext(options);
            var repository = new UserDataRepository(context);
            var hash = DomainExtensions.HashClientSecret(Guid.NewGuid().ToString("N"));
            Assert.True(await repository.CreateAsync(
                new UserEntity(tenantId, "Existing", "existing@example.invalid", hash, null, 1), CancellationToken.None));
            UserEntity[] batch =
            [
                new(tenantId, "New", "new@example.invalid", hash, null, 1),
                new(tenantId, "Duplicate", "EXISTING@example.invalid", hash, null, 1)
            ];

            await Assert.ThrowsAsync<UserEmailConflictException>(
                () => repository.CreateManyAsync(batch, CancellationToken.None));

            var persisted = await context.Set<UserEntity>().AsNoTracking()
                .Where(user => user.TenantId == tenantId)
                .Select(user => user.Email)
                .ToListAsync();
            Assert.Equal("existing@example.invalid", Assert.Single(persisted));
        });

    [SqlServerIntegrationFact]
    public Task Create_MapsPersistenceConflictAfterBothPrechecksMiss() =>
        WithTenantAsync(async (options, tenantId) =>
        {
            await using var firstContext = new GeritDbContext(options);
            await using var secondContext = new GeritDbContext(options);
            var first = new UserDataRepository(firstContext);
            var second = new UserDataRepository(secondContext);
            const string email = "race@example.invalid";
            Assert.False(await first.ExistsByEmailAsync(email, CancellationToken.None));
            Assert.False(await second.ExistsByEmailAsync(email, CancellationToken.None));
            var hash = DomainExtensions.HashClientSecret(Guid.NewGuid().ToString("N"));
            Assert.True(await first.CreateAsync(new UserEntity(tenantId, "Winner", email, hash, null, 1), CancellationToken.None));
            await Assert.ThrowsAsync<UserEmailConflictException>(() => second.CreateAsync(
                new UserEntity(tenantId, "Loser", email.ToUpperInvariant(), hash, null, 1), CancellationToken.None));
            Assert.Equal(1, await secondContext.Set<UserEntity>().CountAsync(user => user.TenantId == tenantId));
        });

    [SqlServerIntegrationFact]
    public Task Update_RejectsStaleProfileAfterPasswordChange() =>
        WithTenantAsync(async (options, tenantId) =>
        {
            await using var profileContext = new GeritDbContext(options);
            await using var passwordContext = new GeritDbContext(options);
            var profiles = new UserDataRepository(profileContext);
            var passwords = new UserDataRepository(passwordContext);
            var original = new UserEntity(tenantId, "Original", "update@example.invalid",
                DomainExtensions.HashClientSecret(Guid.NewGuid().ToString("N")), null, 1);
            Assert.True(await profiles.CreateAsync(original, CancellationToken.None));
            var passwordSnapshot = await passwords.GetByIdAsync(original.Id, CancellationToken.None);
            Assert.NotNull(passwordSnapshot);
            var replacement = Guid.NewGuid().ToString("N");
            passwordSnapshot.UpdatePassword(DomainExtensions.HashClientSecret(replacement), 1);
            Assert.True(await passwords.UpdateAsync(passwordSnapshot, CancellationToken.None));

            original.Update("Stale profile", null, 1);
            Assert.False(await profiles.UpdateAsync(original, CancellationToken.None));

            var saved = await profiles.GetByIdAsync(original.Id, CancellationToken.None);
            Assert.NotNull(saved);
            Assert.Equal("Original", saved.Name);
            Assert.True(DomainExtensions.VerifyClientSecret(saved.PasswordHash, replacement));
        });

    private static async Task WithTenantAsync(Func<DbContextOptions<GeritDbContext>, int, Task> test)
    {
        var tenants = new SqlTestTenantFixture();
        var (tenantId, _) = await tenants.CreateAsync();
        try
        {
            await using var connection = new SqlConnection(
                Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"));
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                EXEC sys.sp_set_session_context @key=N'TenantId', @value=@tenant;
                EXEC sys.sp_set_session_context @key=N'IsSuperAdmin', @value=0;
                """;
            command.Parameters.AddWithValue("@tenant", tenantId);
            await command.ExecuteNonQueryAsync();
            var options = new DbContextOptionsBuilder<GeritDbContext>().UseSqlServer(connection).Options;
            await test(options, tenantId);
        }
        finally
        {
            await tenants.DeleteAsync(tenantId);
        }
    }
}
