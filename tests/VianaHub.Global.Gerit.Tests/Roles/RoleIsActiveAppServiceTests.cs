using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Role;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Tests.Roles;

public sealed class RoleIsActiveAppServiceTests
{
    [Fact]
    public async Task CreateAsync_UsesExplicitInitialStateAndPreservesHistoricalDefault()
    {
        var repository = new Mock<IRoleDataRepository>();
        repository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var domain = new Mock<IRoleDomainService>();
        RoleEntity? created = null;
        domain.Setup(x => x.CreateAsync(It.IsAny<RoleEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RoleEntity, CancellationToken>((entity, _) => { created = entity; typeof(RoleEntity).GetProperty("Id")!.SetValue(entity, 9); })
            .ReturnsAsync(true);
        var service = Create(repository, domain);

        Assert.Equal(9, await service.CreateAsync(new CreateRoleRequest { Name = "Inactive", Description = "Role", IsActive = false }, default));
        Assert.NotNull(created);
        Assert.False(created!.IsActive);

        Assert.Equal(9, await service.CreateAsync(new CreateRoleRequest { Name = "Default", Description = "Role" }, default));
        Assert.True(created!.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesStateOrPreservesWhenOmitted()
    {
        var repository = new Mock<IRoleDataRepository>();
        var entity = new RoleEntity(7, "Operator", "Role", 17);
        repository.Setup(x => x.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var domain = new Mock<IRoleDomainService>();
        domain.Setup(x => x.UpdateAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = Create(repository, domain);

        Assert.True(await service.UpdateAsync(9, new UpdateRoleRequest { Description = "Inactive", IsActive = false }, default));
        Assert.False(entity.IsActive);
        Assert.True(await service.UpdateAsync(9, new UpdateRoleRequest { Description = "Omitted" }, default));
        Assert.False(entity.IsActive);
        Assert.True(await service.UpdateAsync(9, new UpdateRoleRequest { Description = "Active", IsActive = true }, default));
        Assert.True(entity.IsActive);
    }

    private static RoleAppService Create(Mock<IRoleDataRepository> repository, Mock<IRoleDomainService> domain)
    {
        var current = new Mock<ICurrentUserService>();
        current.Setup(x => x.GetTenantId()).Returns(7);
        current.Setup(x => x.GetUserId()).Returns(17);
        var localization = new Mock<ILocalizationService>();
        localization.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        localization.Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object?[]>())).Returns((string key, object?[] _) => key);
        return new RoleAppService(repository.Object, domain.Object, new Notify(), current.Object, localization.Object, Mock.Of<IFileValidationService>());
    }
}
