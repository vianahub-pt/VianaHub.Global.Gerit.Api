using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Repository.Business;

namespace VianaHub.Global.Gerit.Tests.ClientContactPersons;

public class ClientContactPersonRepositoryTests
{
    private static GeritDbContext Context() => new(new DbContextOptionsBuilder<GeritDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ClientContactPersonsEntity Entity(int id, int clientId, string email, bool primary = false)
    {
        var entity = new ClientContactPersonsEntity(7, clientId, $"Contact {id}", null, null, false, email, primary, 17);
        typeof(ClientContactPersonsEntity).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }

    private static ClientEntity Client(int id)
    {
        var entity = new ClientEntity(7, 1, 1, null, null, $"Client {id}", null, null, false, null, null,
            null, null, null, null, null, null, 1, 1, 17);
        typeof(ClientEntity).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }

    [Fact]
    public async Task Queries_AreScopedByClientIdAndId()
    {
        await using var db = Context();
        db.AddRange(Client(10), Client(11));
        db.ClientContactPersons.AddRange(Entity(1, 10, "a@example.test"), Entity(2, 11, "b@example.test"));
        await db.SaveChangesAsync();
        var repo = new ClientContactPersonRepository(db);
        Assert.Single(await repo.GetAllAsync(10, default));
        Assert.NotNull(await repo.GetByIdAsync(10, 1, default));
        Assert.Null(await repo.GetByIdAsync(11, 1, default));
    }

    [Fact]
    public async Task ActiveEmailIdentity_IsClientScopedAndSupportsExceptId()
    {
        await using var db = Context();
        db.ClientContactPersons.AddRange(Entity(1, 10, "same@example.test"), Entity(2, 11, "same@example.test"));
        await db.SaveChangesAsync();
        var repo = new ClientContactPersonRepository(db);
        Assert.True(await repo.ExistsActiveByClientAndEmailAsync(10, "same@example.test", null, default));
        Assert.False(await repo.ExistsActiveByClientAndEmailAsync(10, "same@example.test", 1, default));
        Assert.True(await repo.ExistsActiveByClientAndEmailAsync(11, "same@example.test", null, default));
    }

    [Fact]
    public async Task InactiveAndDeletedContacts_ReleaseActiveEmailIdentity()
    {
        await using var db = Context();
        var inactive = Entity(1, 10, "inactive@example.test"); inactive.Deactivate(17);
        var deleted = Entity(2, 10, "deleted@example.test"); deleted.Delete(17);
        db.ClientContactPersons.AddRange(inactive, deleted); await db.SaveChangesAsync();
        var repo = new ClientContactPersonRepository(db);
        Assert.False(await repo.ExistsActiveByClientAndEmailAsync(10, inactive.Email, null, default));
        Assert.False(await repo.ExistsActiveByClientAndEmailAsync(10, deleted.Email, null, default));
    }

    [Fact]
    public async Task ActivePrimary_IsClientScopedAndSupportsExceptId()
    {
        await using var db = Context();
        db.ClientContactPersons.AddRange(Entity(1, 10, "a@example.test", true), Entity(2, 11, "b@example.test", true));
        await db.SaveChangesAsync();
        var repo = new ClientContactPersonRepository(db);
        Assert.True(await repo.ExistsActivePrimaryAsync(10, null, default));
        Assert.False(await repo.ExistsActivePrimaryAsync(10, 1, default));
        Assert.True(await repo.ExistsActivePrimaryAsync(11, null, default));
    }

    [Fact]
    public async Task DeletedContact_IsExcludedFromReads()
    {
        await using var db = Context();
        var entity = Entity(1, 10, "a@example.test"); entity.Delete(17);
        db.ClientContactPersons.Add(entity); await db.SaveChangesAsync();
        var repo = new ClientContactPersonRepository(db);
        Assert.Empty(await repo.GetAllAsync(10, default));
        Assert.Null(await repo.GetByIdAsync(10, 1, default));
    }
}
