using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Interceptors;
using VianaHub.Global.Gerit.Infra.Data.Repository.Business;

namespace VianaHub.Global.Gerit.Tests;

public sealed class ClientRootSqlServerFactAttribute : FactAttribute
{
    public ClientRootSqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING")))
            Skip = "SQL Server Client root integration requires GERIT_RLS_TEST_CONNECTIONSTRING.";
    }
}

public sealed class ClientRootSqlRlsTests
{
    [ClientRootSqlServerFact]
    public async Task TenantFilter_HidesClientAcrossTenants_AndBlocksCrossTenantUpdate()
    {
        await using var db = CreateContext();
        await db.Database.OpenConnectionAsync();
        await SetTenantAsync(db, 1);
        Assert.True(await db.Clients.AnyAsync(client => !client.IsDeleted));

        await SetTenantAsync(db, 2);
        Assert.False(await db.Clients.AnyAsync(client => !client.IsDeleted));

        try
        {
            var affected = await db.Database.ExecuteSqlRawAsync("UPDATE dbo.Clients SET Name = Name WHERE Id = 1");
            Assert.Equal(0, affected);
        }
        catch (SqlException exception)
        {
            Assert.Contains(exception.Number, new[] { 33504, 33505, 33506 });
        }
    }

    [ClientRootSqlServerFact]
    public async Task TenantFilter_HidesAllClientChildrenAcrossTenants()
    {
        await using var db = CreateContext();
        await db.Database.OpenConnectionAsync();
        await SetTenantAsync(db, 1);
        var clientId = await db.Clients.Where(client => !client.IsDeleted).Select(client => client.Id).FirstAsync();
        Assert.True(await db.ClientContactPersons.AnyAsync(child => child.ClientId == clientId));
        Assert.True(await db.ClientAddresses.AnyAsync(child => child.ClientId == clientId));
        Assert.True(await db.ClientFiscalData.AnyAsync(child => child.ClientId == clientId));
        Assert.True(await db.ClientDocuments.AnyAsync(child => child.ClientId == clientId));

        await SetTenantAsync(db, 2);
        Assert.False(await db.Clients.AnyAsync(client => client.Id == clientId));
        Assert.False(await db.ClientContactPersons.AnyAsync(child => child.ClientId == clientId));
        Assert.False(await db.ClientAddresses.AnyAsync(child => child.ClientId == clientId));
        Assert.False(await db.ClientFiscalData.AnyAsync(child => child.ClientId == clientId));
        Assert.False(await db.ClientDocuments.AnyAsync(child => child.ClientId == clientId));
    }

    [ClientRootSqlServerFact]
    public async Task AddMany_RollsBackClientBatchWhenStatusForeignKeyFails()
    {
        var suffix = Guid.NewGuid().ToString("N");
        await using var db = CreateContext();
        await db.Database.OpenConnectionAsync();
        await SetTenantAsync(db, 1);
        var repository = new ClientRepository(db);
        var valid = new ClientEntity(1, 1, 1, null, null, $"ClientRootRollback-{suffix}-1", null, null, false, null, null, null, null, null, null, null, null, 1, 1, 1);
        var invalid = new ClientEntity(1, 1, 1, null, null, $"ClientRootRollback-{suffix}-2", null, null, false, null, null, null, null, null, null, null, null, 999999, 1, 1);

        await Assert.ThrowsAnyAsync<Exception>(() => repository.AddManyAsync([valid, invalid], CancellationToken.None));

        Assert.Equal(0, await db.Clients.CountAsync(client => client.Name!.StartsWith($"ClientRootRollback-{suffix}")));
    }

    [ClientRootSqlServerFact]
    public async Task RootDeleteRetainsAllChildrenWithRealTenantInterceptors()
    {
        using var provider = BuildTenantProvider();
        await using var scope = provider.CreateAsyncScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<IRequestTenantContext>();
        tenantContext.SetTenantId(1);
        var db = scope.ServiceProvider.GetRequiredService<GeritDbContext>();
        await db.Database.OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N");
        var client = new ClientEntity(1, 1, 1, null, null, $"ClientRootDelete-{suffix}", null, null, false, null, null, null, null, null, null, null, null, 1, 1, 1);
        await db.Clients.AddAsync(client);
        await db.SaveChangesAsync();
        var clientId = client.Id;
        await db.ClientContactPersons.AddAsync(new ClientContactPersonsEntity(1, clientId, "Contact", null, null, false, $"{suffix}@example.test", false, 1));
        await db.ClientAddresses.AddAsync(new ClientAddressesEntity(1, clientId, 1, "PT", "Street", null, null, "Centre", "Lisboa", null, "1000", null, null, null, false, 1));
        await db.ClientFiscalData.AddAsync(new ClientFiscalDataEntity(1, clientId, $"PT{suffix[..8]}", null, "PT", false, null, null, 1));
        await db.ClientDocuments.AddAsync(new ClientDocumentsEntity(1, clientId, 1, $"DOC-{suffix}", "PT", new DateOnly(2026, 1, 1), null, false, 1));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var session = await ReadSessionAsync(db);
        Assert.Equal(1, session.TenantId);
        Assert.Null(session.IsSuperAdmin);

        var root = await db.Clients.SingleAsync(x => x.Id == clientId);
        Assert.Empty(root.Contacts);
        root.Delete(1);
        db.Clients.Update(root);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        session = await ReadSessionAsync(db);
        Assert.Equal(1, session.TenantId);
        Assert.Null(session.IsSuperAdmin);
        var deletedFlag = await db.Database.SqlQueryRaw<bool>("SELECT IsDeleted AS Value FROM dbo.Clients WHERE Id = {0}", clientId).SingleAsync();
        Assert.True(deletedFlag);
        Assert.True(await db.ClientContactPersons.AnyAsync(x => x.ClientId == clientId && !x.IsDeleted));
        Assert.True(await db.ClientAddresses.AnyAsync(x => x.ClientId == clientId && !x.IsDeleted));
        Assert.True(await db.ClientFiscalData.AnyAsync(x => x.ClientId == clientId && !x.IsDeleted));
        Assert.True(await db.ClientDocuments.AnyAsync(x => x.ClientId == clientId && !x.IsDeleted));
    }


    private static GeritDbContext CreateContext()
    {
        var connectionString = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"))
        {
            Encrypt = true,
            TrustServerCertificate = true,
            ApplicationName = "Gerit.ClientRootSqlTest." + Guid.NewGuid().ToString("N"),
            MaxPoolSize = 2,
            MinPoolSize = 0,
            Pooling = true,
            ConnectRetryCount = 0
        }.ConnectionString;
        return new GeritDbContext(new DbContextOptionsBuilder<GeritDbContext>().UseSqlServer(connectionString).Options);
    }

    private static ServiceProvider BuildTenantProvider()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddLogging();
        services.AddScoped<IRequestTenantContext, RequestTenantContext>();
        services.AddScoped<TenantSessionConnectionInterceptor>();
        services.AddScoped<TenantSessionCommandInterceptor>();
        services.AddDbContext<GeritDbContext>((serviceProvider, options) =>
        {
            var connectionString = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"))
            {
                Encrypt = true,
                TrustServerCertificate = true,
                ApplicationName = "Gerit.ClientRootSqlTenantFixture." + Guid.NewGuid().ToString("N"),
                MaxPoolSize = 2,
                MinPoolSize = 0,
                Pooling = true,
                ConnectRetryCount = 0
            }.ConnectionString;
            options.UseSqlServer(connectionString).AddInterceptors(
                serviceProvider.GetRequiredService<TenantSessionConnectionInterceptor>(),
                serviceProvider.GetRequiredService<TenantSessionCommandInterceptor>());
        });
        return services.BuildServiceProvider();
    }

    private static async Task<(int? TenantId, int? IsSuperAdmin)> ReadSessionAsync(GeritDbContext db)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT TRY_CONVERT(INT, SESSION_CONTEXT(N'TenantId')), TRY_CONVERT(INT, SESSION_CONTEXT(N'IsSuperAdmin'));";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (reader.IsDBNull(0) ? null : reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetInt32(1));
    }

    private static async Task SetTenantAsync(GeritDbContext db, int tenantId)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "EXEC sys.sp_set_session_context @key=N'TenantId', @value=@tenant";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tenant";
        parameter.Value = tenantId;
        command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync();
    }

}
