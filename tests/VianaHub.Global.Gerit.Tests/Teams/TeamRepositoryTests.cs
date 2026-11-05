using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Repository.Business;

namespace VianaHub.Global.Gerit.Tests.Teams;

public class TeamRepositoryTests
{
    [Fact]
    public async Task NameChecks_RespectTenantOwnIdAndSoftDelete()
    {
        await using var context = Context();
        var own = new TeamEntity(7, "Own", "Description", 17);
        var other = new TeamEntity(7, "Other", "Description", 17);
        var otherTenant = new TeamEntity(8, "Foreign", "Description", 17);
        var deleted = new TeamEntity(7, "Deleted", "Description", 17);
        deleted.Delete(17);
        context.Teams.AddRange(own, other, otherTenant, deleted);
        await context.SaveChangesAsync();
        var repository = new TeamDataRepository(context);

        Assert.True(await repository.ExistsByNameAsync(7, "Own", default));
        Assert.False(await repository.ExistsByNameAsync(7, "Foreign", default));
        Assert.False(await repository.ExistsByNameAsync(7, "Deleted", default));
        Assert.False(await repository.ExistsByNameExceptIdAsync(7, "Own", own.Id, default));
        Assert.True(await repository.ExistsByNameExceptIdAsync(7, "Other", own.Id, default));
        Assert.False(await repository.ExistsByNameExceptIdAsync(7, "Foreign", own.Id, default));
        Assert.False(await repository.ExistsByNameExceptIdAsync(7, "Deleted", own.Id, default));
    }

    [Fact]
    public async Task Reads_FilterSoftDeleteAndPageByState()
    {
        await using var context = Context();
        var active = new TeamEntity(7, "Active", "Description", 17);
        var inactive = new TeamEntity(7, "Inactive", "Description", 17);
        inactive.Deactivate(17);
        var deleted = new TeamEntity(7, "Deleted", "Description", 17);
        deleted.Delete(17);
        context.Teams.AddRange(active, inactive, deleted);
        await context.SaveChangesAsync();
        var repository = new TeamDataRepository(context);

        Assert.Equal(2, (await repository.GetAllAsync(default)).Count());
        Assert.Null(await repository.GetByIdAsync(deleted.Id, default));
        Assert.False(await repository.ExistsByIdAsync(deleted.Id, default));
        var page = await repository.GetPagedAsync(new PagedFilter(null, false, 1, 10, "Name", "asc"), default);
        Assert.Equal(inactive.Id, Assert.Single(page.Items).Id);
        Assert.Equal(1, page.TotalItems);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(1, page.TotalPages);
    }

    [Fact]
    public async Task Writes_ReturnGeneratedIdAndPersistEditableFieldsAndSoftDelete()
    {
        await using var context = Context();
        var repository = new TeamDataRepository(context);
        var team = new TeamEntity(7, "Original", "Original description", 17);
        Assert.True(await repository.AddAsync(team, default));
        Assert.True(team.Id > 0);
        context.ChangeTracker.Clear();
        var loaded = await repository.GetByIdAsync(team.Id, default);
        Assert.NotNull(loaded);
        loaded.Update("Renamed", "Edited description", 17);
        Assert.True(await repository.UpdateAsync(loaded, default));
        context.ChangeTracker.Clear();
        var edited = await repository.GetByIdAsync(team.Id, default);
        Assert.NotNull(edited);
        Assert.Equal("Renamed", edited.Name);
        Assert.Equal("Edited description", edited.Description);
        edited.Delete(17);
        Assert.True(await repository.UpdateAsync(edited, default));
        context.ChangeTracker.Clear();
        Assert.Null(await repository.GetByIdAsync(team.Id, default));
        var stored = await context.Teams.IgnoreQueryFilters().SingleAsync();
        Assert.True(stored.IsDeleted);
        Assert.False(stored.IsActive);
    }

    [Fact]
    public async Task Repository_ReportsFalseWhenSaveAffectsNoRows()
    {
        var control = new TeamSaveControl { Suppress = true };
        await using var context = Context(control);
        var repository = new TeamDataRepository(context);
        Assert.False(await repository.AddAsync(new TeamEntity(7, "New", "Description", 17), default));
        context.ChangeTracker.Clear();
        Assert.Empty(await context.Teams.ToListAsync());
    }

    private static GeritDbContext Context(TeamSaveControl? control = null)
    {
        var options = new DbContextOptionsBuilder<GeritDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString());
        if (control is not null) options.AddInterceptors(control);
        return new GeritDbContext(options.Options);
    }
}
