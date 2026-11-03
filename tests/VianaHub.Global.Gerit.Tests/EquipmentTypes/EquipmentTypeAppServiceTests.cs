using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.EquipmentType;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Services.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Domain.Validators.Business.EquipmentType;

namespace VianaHub.Global.Gerit.Tests.EquipmentTypes;

public class EquipmentTypeAppServiceTests
{
    [Fact]
    public async Task Create_ReturnsPersistedIdAndUsesAuthenticatedTenantAndUser()
    {
        var fixture = new Fixture();
        EquipmentTypeEntity? saved = null;
        fixture.Repository.Setup(x => x.AddAsync(It.IsAny<EquipmentTypeEntity>(), It.IsAny<CancellationToken>()))
            .Callback<EquipmentTypeEntity, CancellationToken>((entity, _) =>
            {
                saved = entity;
                typeof(EquipmentTypeEntity).GetProperty(nameof(EquipmentTypeEntity.Id))!.SetValue(entity, 81);
            }).ReturnsAsync(true);

        Assert.Equal(81, await fixture.App.CreateAsync(new CreateEquipmentTypeRequest { Name = "New", Description = "New description" }, default));
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
        Assert.Equal(0, await fixture.App.CreateAsync(new CreateEquipmentTypeRequest { Name = "Crew", Description = "Description" }, default));
        Assert.Equal(HttpStatusCode.Conflict, fixture.Notify.GetStatusCode());
        fixture.Repository.Verify(x => x.AddAsync(It.IsAny<EquipmentTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Crew")]
    [InlineData("Renamed crew")]
    public async Task Update_AllowsOwnNameAndRenameAndChangesDescription(string name)
    {
        var fixture = new Fixture();
        Assert.True(await fixture.App.UpdateAsync(42, new UpdateEquipmentTypeRequest { Name = name, Description = "Edited" }, default));
        Assert.Equal(name, fixture.EquipmentType!.Name);
        Assert.Equal("Edited", fixture.EquipmentType.Description);
        Assert.Equal(17, fixture.EquipmentType.ModifiedBy);
        fixture.Repository.Verify(x => x.ExistsByNameExceptIdAsync(7, name, 42, It.IsAny<CancellationToken>()), Times.Once);
        fixture.Repository.Verify(x => x.UpdateAsync(fixture.EquipmentType, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_DuplicateIs409WithoutMutatingOrWriting()
    {
        var fixture = new Fixture();
        fixture.Repository.Setup(x => x.ExistsByNameExceptIdAsync(7, "Other", 42, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Assert.False(await fixture.App.UpdateAsync(42, new UpdateEquipmentTypeRequest { Name = "Other", Description = "Edited" }, default));
        Assert.Equal(HttpStatusCode.Conflict, fixture.Notify.GetStatusCode());
        Assert.Equal("Crew", fixture.EquipmentType!.Name);
        Assert.Equal("Description", fixture.EquipmentType.Description);
        fixture.Repository.Verify(x => x.UpdateAsync(It.IsAny<EquipmentTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAndDetail_ExposeEditableFieldsAndState()
    {
        var fixture = new Fixture();
        fixture.Repository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([fixture.EquipmentType!]);
        var listed = Assert.Single(await fixture.App.GetAllAsync(default));
        Assert.Equal(42, listed.Id);
        Assert.Equal("Crew", listed.Name);
        var detail = await fixture.App.GetByIdAsync(42, default);
        Assert.NotNull(detail);
        Assert.Equal(7, detail.TenantId);
        Assert.Equal("Crew", detail.Name);
        Assert.Equal("Description", detail.Description);
        Assert.Equal(fixture.EquipmentType!.CreatedAt, detail.CreatedAt);
        Assert.True(detail.IsActive);
    }

    [Fact]
    public async Task GetPaged_ForwardsFiltersAndPreservesMetadata()
    {
        var fixture = new Fixture();
        fixture.Repository.Setup(x => x.GetPagedAsync(It.IsAny<PagedFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListPage<EquipmentTypeEntity>([fixture.EquipmentType!], 2, 3, 7, 3));
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
        Assert.Equal(active, fixture.EquipmentType!.IsActive);
        Assert.False(fixture.Notify.HasNotify());
        fixture.Repository.Verify(x => x.UpdateAsync(fixture.EquipmentType, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Delete_IsSoftDeleteAndRepeatIs410WithoutAnotherWrite()
    {
        var fixture = new Fixture();
        Assert.True(await fixture.App.DeleteAsync(42, default));
        Assert.True(fixture.EquipmentType!.IsDeleted);
        Assert.False(fixture.EquipmentType.IsActive);
        Assert.False(await fixture.App.DeleteAsync(42, default));
        Assert.Equal(HttpStatusCode.Gone, fixture.Notify.GetStatusCode());
        fixture.Repository.Verify(x => x.UpdateAsync(fixture.EquipmentType, It.IsAny<CancellationToken>()), Times.Once);
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
        fixture.Repository.Setup(x => x.AddAsync(It.IsAny<EquipmentTypeEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        fixture.Repository.Setup(x => x.UpdateAsync(It.IsAny<EquipmentTypeEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        if (operation == "Create")
            Assert.Equal(0, await fixture.App.CreateAsync(new CreateEquipmentTypeRequest { Name = "New", Description = "Description" }, default));
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
        if (deleted) fixture.EquipmentType!.Delete(17);
        else fixture.EquipmentType = null;
        if (operation == "Get") Assert.Null(await fixture.App.GetByIdAsync(42, default));
        else Assert.False(await fixture.Mutate(operation));
        Assert.Equal(HttpStatusCode.Gone, fixture.Notify.GetStatusCode());
        fixture.Repository.Verify(x => x.UpdateAsync(It.IsAny<EquipmentTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(true, "", "Description")]
    [InlineData(true, "Crew", null)]
    [InlineData(false, "", "Description")]
    [InlineData(false, "Crew", null)]
    public async Task EssentialDomainValidation_PreventsWrite(bool create, string name, string? description)
    {
        var fixture = new Fixture();
        if (create) Assert.Equal(0, await fixture.App.CreateAsync(new CreateEquipmentTypeRequest { Name = name, Description = description }, default));
        else Assert.False(await fixture.App.UpdateAsync(42, new UpdateEquipmentTypeRequest { Name = name, Description = description }, default));
        Assert.Equal(HttpStatusCode.BadRequest, fixture.Notify.GetStatusCode());
        fixture.Repository.Verify(x => x.AddAsync(It.IsAny<EquipmentTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Repository.Verify(x => x.UpdateAsync(It.IsAny<EquipmentTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class Fixture
    {
        public EquipmentTypeEntity? EquipmentType = EquipmentTypeTestSupport.Entity();
        public Mock<IEquipmentTypeDataRepository> Repository { get; } = new();
        public Notify Notify { get; } = new();
        public EquipmentTypeAppService App { get; }

        public Fixture()
        {
            var localization = EquipmentTypeTestSupport.Localization().Object;
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(x => x.GetTenantId()).Returns(7);
            currentUser.Setup(x => x.GetUserId()).Returns(17);
            Repository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((int id, CancellationToken _) => Task.FromResult(EquipmentType is { IsDeleted: false } equipmentType && equipmentType.Id == id ? equipmentType : null));
            Repository.Setup(x => x.UpdateAsync(It.IsAny<EquipmentTypeEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var domain = new EquipmentTypeDomainService(Repository.Object, new EquipmentTypeValidator(localization), Notify);
            App = new EquipmentTypeAppService(Repository.Object, domain, EquipmentTypeTestSupport.Mapper(), Notify, localization,
                currentUser.Object,
                Mock.Of<VianaHub.Global.Gerit.Application.Interfaces.Common.IFileValidationService>());
        }

        public Task<bool> Mutate(string operation) => operation switch
        {
            "Update" => App.UpdateAsync(42, new UpdateEquipmentTypeRequest { Name = "Edited", Description = "Edited description" }, default),
            "Activate" => App.ActivateAsync(42, default),
            "Deactivate" => App.DeactivateAsync(42, default),
            "Delete" => App.DeleteAsync(42, default),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }
}
