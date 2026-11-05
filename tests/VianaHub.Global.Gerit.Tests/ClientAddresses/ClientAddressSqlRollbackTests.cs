using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Exceptions;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Repository.Business;

namespace VianaHub.Global.Gerit.Tests.ClientAddresses;

public sealed class ClientAddressSqlServerFactAttribute : FactAttribute
{
    public ClientAddressSqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING")))
            Skip = "SQL Server integration requires GERIT_RLS_TEST_CONNECTIONSTRING.";
    }
}

public sealed class ClientAddressSqlRollbackTests
{
    [ClientAddressSqlServerFact]
    public async Task RlsDoesNotExposeTenantOneAddressToTenantTwo()
    {
        var connectionString = Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING")!;
        var options = new DbContextOptionsBuilder<GeritDbContext>().UseSqlServer(connectionString).Options;
        await using var db = new GeritDbContext(options);
        await db.Database.OpenConnectionAsync();
        await SetTenantAsync(db, 1);
        var repository = new ClientAddressRepository(db);
        var tenantOne = (await repository.GetAllAsync(1, default)).FirstOrDefault();
        Assert.NotNull(tenantOne);

        await SetTenantAsync(db, 2);
        Assert.Empty(await repository.GetAllAsync(tenantOne!.ClientId, default));
        Assert.Null(await repository.GetByIdAsync(tenantOne.ClientId, tenantOne.Id, default));
    }

    [ClientAddressSqlServerFact]
    public async Task CreateMany_RollsBackWhenFilteredPrimaryConstraintFails()
    {
        var connectionString = Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING")!;
        var options = new DbContextOptionsBuilder<GeritDbContext>().UseSqlServer(connectionString).Options;
        var suffix = Guid.NewGuid().ToString("N");
        await using var db = new GeritDbContext(options);
        await db.Database.OpenConnectionAsync();
        await using (var context = db.Database.GetDbConnection().CreateCommand())
        {
            context.CommandText = "EXEC sys.sp_set_session_context @key=N'TenantId', @value=1;";
            await context.ExecuteNonQueryAsync();
        }

        var repository = new ClientAddressRepository(db);
        var first = new ClientAddressesEntity(1, 1, 1, "PT", $"Rollback-{suffix}-1", null, null, "Centre", "Lisboa", null, "1000", null, null, null, true, 1);
        var second = new ClientAddressesEntity(1, 1, 1, "PT", $"Rollback-{suffix}-2", null, null, "Centre", "Lisboa", null, "1001", null, null, null, true, 1);

        await Assert.ThrowsAsync<ClientAddressConflictException>(() => repository.CreateManyAsync([first, second], default));

        Assert.Equal(0, await db.ClientAddresses.CountAsync(x => x.ClientId == 1 && x.Street.StartsWith($"Rollback-{suffix}")));
    }

    private static async Task SetTenantAsync(GeritDbContext db, int tenantId)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "EXEC sys.sp_set_session_context @key=N'TenantId', @value=@tenant";
        var parameter = command.CreateParameter(); parameter.ParameterName = "@tenant"; parameter.Value = tenantId; command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync();
    }
}
