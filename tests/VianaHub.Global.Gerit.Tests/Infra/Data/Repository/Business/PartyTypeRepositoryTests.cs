using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Repository.Business;

namespace VianaHub.Global.Gerit.Tests.Infra.Data.Repository.Business;

public class PartyTypeRepositoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExistsByCode_IncludesDeletedRowsBecauseUniqueIndexIsUnfiltered(bool deleted)
    {
        var options = new DbContextOptionsBuilder<GeritDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new GeritDbContext(options);
        var entity = new PartyTypeEntity("INDIVIDUAL", 1);
        context.PartyTypes.Add(entity);
        context.Entry(entity).Property("Id").CurrentValue = (byte)1;
        if (deleted) entity.Delete(1);
        await context.SaveChangesAsync();

        var repository = new PartyTypeDataRepository(context);
        Assert.Equal(!deleted, await context.PartyTypes.AnyAsync());
        Assert.True(await repository.ExistsByCodeAsync("INDIVIDUAL", CancellationToken.None));
        Assert.False(await repository.ExistsByCodeAsync("ORGANIZATION", CancellationToken.None));
    }
}
