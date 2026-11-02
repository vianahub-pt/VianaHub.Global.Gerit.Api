using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Team;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Services.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Domain.Validators.Business.Team;

namespace VianaHub.Global.Gerit.Tests.Teams;

public class TeamAppServiceTests
{
    [Fact]
    public async Task Create_ReturnsPersistedIdAndUsesAuthenticatedTenantAndUser()
    {
        var fixture = new Fixture();
        TeamEntity? saved = null;
        fixture.Repository.Setup(x => x.AddAsync(It.IsAny<TeamEntity>(), It.IsAny<CancellationToken>()))
            .Callback<TeamEntity, CancellationToken>((entity, _) =>
            {
                saved = entity;
                typeof(TeamEntity).GetProperty(nameof(TeamEntity.Id))!.SetValue(entity, 81);
            }).ReturnsAsync(true);

        Assert.Equal(81, await fixture.App.CreateAsync(new CreateTeamRequest { Name = "New", Description = "New description" }, default));
        Assert.NotNull(saved);
        Assert.Equal(7, saved.TenantId);
        Assert.Equal(17, saved.CreatedBy);
        Assert.Equal("New", saved.Name);
        Assert.Equal("New description", saved.Description);
        Assert.True(saved.IsActive);
        Assert.False(fixture.Notify.HasNotify());
    }

    [Fact]
    public async Task Create_DuplicateIs409AndDoesNotWrite()
    {
        var fixture = new Fixture();
        fixture.Repository.Setup(x => x.ExistsByNameAsync(7, "Crew", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Assert.Equal(0, await fixture.App.CreateAsync(new CreateTeamRequest { Name = "Crew", Description = "Description" }, default));
        Assert.Equal(HttpStatusCode.Conflict, fixture.Notify.GetStatusCode());
        fixture.Repository.Verify(x => x.AddAsync(It.IsAny<TeamEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Crew")]
    [InlineData("Renamed crew")]
    public async Task Update_AllowsOwnNameAndRenameAndChangesDescription(string name)
    {
        var fixture = new Fixture();
        Assert.True(await fixture.App.UpdateAsync(42, new UpdateTeamRequest { Name = name, Description = "Edited" }, default));
        Assert.Equal(name, fixture.Team!.Name);
        Assert.Equal("Edited", fixture.Team.Description);
        Assert.Equal(17, fixture.Team.ModifiedBy);
        fixture.Repository.Verify(x => x.ExistsByNameExceptIdAsync(7, name, 42, It.IsAny<CancellationToken>()), Times.Once);
        fixture.Repository.Verify(x => x.UpdateAsync(fixture.Team, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_DuplicateIs409WithoutMutatingOrWriting()
    {
        var fixture = new Fixture();
        fixture.Repository.Setup(x => x.ExistsByNameExceptIdAsync(7, "Other", 42, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Assert.False(await fixture.App.UpdateAsync(42, new UpdateTeamRequest { Name = "Other", Description = "Edited" }, default));
        Assert.Equal(HttpStatusCode.Conflict, fixture.Notify.GetStatusCode());
        Assert.Equal("Crew", fixture.Team!.Name);
        Assert.Equal("Description", fixture.Team.Description);
        fixture.Repository.Verify(x => x.UpdateAsync(It.IsAny<TeamEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAndDetail_ExposeEditableFieldsAndState()
    {
        var fixture = new Fixture();
        fixture.Repository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([fixture.Team!]);
        var listed = Assert.Single(await fixture.App.GetAllAsync(default));
        Assert.Equal(42, listed.Id);
        Assert.Equal("Crew", listed.Name);
        var detail = await fixture.App.GetByIdAsync(42, default);
        Assert.NotNull(detail);
        Assert.Equal(7, detail.TenantId);
        Assert.Equal("Crew", detail.Name);
        Assert.Equal("Description", detail.Description);
        Assert.Equal(fixture.Team!.CreatedAt, detail.CreatedAt);
        Assert.True(detail.IsActive);
    }

    [Fact]
    public async Task GetPaged_ForwardsFiltersAndPreservesMetadata()
    {
        var fixture = new Fixture();
        fixture.Repository.Setup(x => x.GetPagedAsync(It.IsAny<PagedFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListPage<TeamEntity>([fixture.Team!], 2, 3, 7, 3));
        var result = await fixture.App.GetPagedAsync(new PagedFilterRequest
        {
            Search = "Crew",
            IsActive = true,
            PageNumber = 2,
            PageSize = 3,
            SortBy = "Name",
            SortDirection = "desc"
        }, default);
        Assert.Single(result.Items);
        Assert.Equal(7, result.TotalItems);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(3, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        fixture.Repository.Verify(x => x.GetPagedAsync(It.Is<PagedFilter>(f => f.Search == "Crew" &&
            f.IsActive == true && f.PageNumber == 2 && f.PageSize == 3 && f.SortBy == "Name" && f.SortDirection == "desc"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Activate", true)]
    [InlineData("Deactivate", false)]
    public async Task StateTransitions_AreIdempotentThroughRealDomainAndValidators(string operation, bool active)
    {
        var fixture = new Fixture();
        Assert.True(await fixture.Mutate(operation));
        Assert.True(await fixture.Mutate(operation));
        Assert.Equal(active, fixture.Team!.IsActive);
        Assert.False(fixture.Notify.HasNotify());
        fixture.Repository.Verify(x => x.UpdateAsync(fixture.Team, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Delete_IsSoftDeleteAndRepeatIs410WithoutAnotherWrite()
    {
        var fixture = new Fixture();
        Assert.True(await fixture.App.DeleteAsync(42, default));
        Assert.True(fixture.Team!.IsDeleted);
        Assert.False(fixture.Team.IsActive);
        Assert.False(await fixture.App.DeleteAsync(42, default));
        Assert.Equal(HttpStatusCode.Gone, fixture.Notify.GetStatusCode());
        fixture.Repository.Verify(x => x.UpdateAsync(fixture.Team, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Update")]
    [InlineData("Activate")]
    [InlineData("Deactivate")]
    [InlineData("Delete")]
    public async Task PersistenceFalse_PropagatesThroughDomainAndApplication(string operation)
    {
        var fixture = new Fixture();
        fixture.Repository.Setup(x => x.AddAsync(It.IsAny<TeamEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        fixture.Repository.Setup(x => x.UpdateAsync(It.IsAny<TeamEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        if (operation == "Create")
            Assert.Equal(0, await fixture.App.CreateAsync(new CreateTeamRequest { Name = "New", Description = "Description" }, default));
        else
            Assert.False(await fixture.Mutate(operation));
    }

    [Theory]
    [InlineData("Get", false)]
    [InlineData("Get", true)]
    [InlineData("Update", false)]
    [InlineData("Update", true)]
    [InlineData("Activate", false)]
    [InlineData("Activate", true)]
    [InlineData("Deactivate", false)]
    [InlineData("Deactivate", true)]
    [InlineData("Delete", false)]
    [InlineData("Delete", true)]
    public async Task MissingOrDeleted_Is410AndDoesNotWrite(string operation, bool deleted)
    {
        var fixture = new Fixture();
        if (deleted) fixture.Team!.Delete(17);
        else fixture.Team = null;
        if (operation == "Get") Assert.Null(await fixture.App.GetByIdAsync(42, default));
        else Assert.False(await fixture.Mutate(operation));
        Assert.Equal(HttpStatusCode.Gone, fixture.Notify.GetStatusCode());
        fixture.Repository.Verify(x => x.UpdateAsync(It.IsAny<TeamEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(true, "", "Description")]
    [InlineData(true, "Crew", "")]
    [InlineData(false, "", "Description")]
    [InlineData(false, "Crew", "")]
    public async Task EssentialDomainValidation_PreventsWrite(bool create, string name, string description)
    {
        var fixture = new Fixture();
        if (create) Assert.Equal(0, await fixture.App.CreateAsync(new CreateTeamRequest { Name = name, Description = description }, default));
        else Assert.False(await fixture.App.UpdateAsync(42, new UpdateTeamRequest { Name = name, Description = description }, default));
        Assert.Equal(HttpStatusCode.BadRequest, fixture.Notify.GetStatusCode());
        fixture.Repository.Verify(x => x.AddAsync(It.IsAny<TeamEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Repository.Verify(x => x.UpdateAsync(It.IsAny<TeamEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class Fixture
    {
        public TeamEntity? Team = TeamTestSupport.Entity();
        public Mock<ITeamDataRepository> Repository { get; } = new();
        public Notify Notify { get; } = new();
        public TeamAppService App { get; }

        public Fixture()
        {
            var localization = TeamTestSupport.Localization().Object;
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(x => x.GetTenantId()).Returns(7);
            currentUser.Setup(x => x.GetUserId()).Returns(17);
            Repository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((int id, CancellationToken _) => Task.FromResult(Team is { IsDeleted: false } team && team.Id == id ? team : null));
            Repository.Setup(x => x.UpdateAsync(It.IsAny<TeamEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var domain = new TeamDomainService(Repository.Object, new TeamValidator(localization), Notify);
            App = new TeamAppService(Repository.Object, domain, TeamTestSupport.Mapper(), Notify, localization,
                currentUser.Object, NullLogger<TeamAppService>.Instance,
                Mock.Of<VianaHub.Global.Gerit.Application.Interfaces.Common.IFileValidationService>());
        }

        public Task<bool> Mutate(string operation) => operation switch
        {
            "Update" => App.UpdateAsync(42, new UpdateTeamRequest { Name = "Edited", Description = "Edited description" }, default),
            "Activate" => App.ActivateAsync(42, default),
            "Deactivate" => App.DeactivateAsync(42, default),
            "Delete" => App.DeleteAsync(42, default),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }
}
