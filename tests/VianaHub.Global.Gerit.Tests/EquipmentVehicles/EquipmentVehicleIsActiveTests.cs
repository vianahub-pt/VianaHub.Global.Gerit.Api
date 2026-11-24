using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Equipment;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Vehicle;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Tests.EquipmentVehicles;

public class EquipmentVehicleIsActiveTests
{
    [Fact]
    public async Task Equipment_CreatePersistsRequestedInactiveState()
    {
        var repo = new Mock<IEquipmentDataRepository>();
        var domain = new Mock<IEquipmentDomainService>();
        var current = CurrentUser();
        var service = new EquipmentAppService(Mock.Of<IEquipmentTypeDataRepository>(), repo.Object, domain.Object,
            new Notify(), Localization(), current.Object, Mock.Of<IFileValidationService>());
        EquipmentEntity? created = null;
        domain.Setup(x => x.CreateAsync(It.IsAny<EquipmentEntity>(), default))
            .Callback<EquipmentEntity, CancellationToken>((entity, _) => created = entity).ReturnsAsync(true);

        await service.CreateAsync(new CreateEquipmentRequest { Name = "E", EquipmentTypeId = 1, StatusDefinitionId = 1, StatusDomainId = 1, IsActive = false }, default);

        Assert.NotNull(created);
        Assert.False(created!.IsActive);
    }

    [Fact]
    public async Task Equipment_UpdatePreservesOmittedStateAndUsesOnlyUpdateOperation()
    {
        var repo = new Mock<IEquipmentDataRepository>();
        var domain = new Mock<IEquipmentDomainService>();
        var service = new EquipmentAppService(Mock.Of<IEquipmentTypeDataRepository>(), repo.Object, domain.Object,
            new Notify(), Localization(), CurrentUser().Object, Mock.Of<IFileValidationService>());
        var entity = new EquipmentEntity(7, 1, 1, 1, "E", null, null, 17, false);
        repo.Setup(x => x.GetByIdAsync(42, default)).ReturnsAsync(entity);
        domain.Setup(x => x.UpdateAsync(entity, default)).ReturnsAsync(true);

        Assert.True(await service.UpdateAsync(42, new UpdateEquipmentRequest { EquipmentTypeId = 1, StatusDefinitionId = 1, StatusDomainId = 1, Name = "E" }, default));

        Assert.False(entity.IsActive);
        domain.Verify(x => x.UpdateAsync(entity, default), Times.Once);
        domain.Verify(x => x.ActivateAsync(It.IsAny<EquipmentEntity>(), default), Times.Never);
        domain.Verify(x => x.DeactivateAsync(It.IsAny<EquipmentEntity>(), default), Times.Never);
    }

    [Fact]
    public async Task Vehicle_CreatePersistsRequestedInactiveState()
    {
        var repo = new Mock<IVehicleDataRepository>();
        var domain = new Mock<IVehicleDomainService>();
        var service = new VehicleAppService(repo.Object, domain.Object, new Notify(), Localization(), CurrentUser().Object,
            Mock.Of<IFileValidationService>());
        VehicleEntity? created = null;
        domain.Setup(x => x.CreateAsync(It.IsAny<VehicleEntity>(), default))
            .Callback<VehicleEntity, CancellationToken>((entity, _) => created = entity).ReturnsAsync(true);

        await service.CreateAsync(new CreateVehicleRequest { Plate = "AA-00-AA", StatusDefinitionId = 1, StatusDomainId = 1, IsActive = false }, default);

        Assert.NotNull(created);
        Assert.False(created!.IsActive);
    }

    [Fact]
    public async Task Vehicle_UpdatePreservesOmittedStateAndUsesOnlyUpdateOperation()
    {
        var repo = new Mock<IVehicleDataRepository>();
        var domain = new Mock<IVehicleDomainService>();
        var service = new VehicleAppService(repo.Object, domain.Object, new Notify(), Localization(), CurrentUser().Object,
            Mock.Of<IFileValidationService>());
        var entity = new VehicleEntity(7, 1, 1, "AA-00-AA", null, null, 2020, null, null, 17, false);
        repo.Setup(x => x.GetByIdAsync(42, default)).ReturnsAsync(entity);
        domain.Setup(x => x.UpdateAsync(entity, default)).ReturnsAsync(true);

        Assert.True(await service.UpdateAsync(42, new UpdateVehicleRequest { Plate = "AA-00-AA", StatusDefinitionId = 1, StatusDomainId = 1, Year = 2020 }, default));

        Assert.False(entity.IsActive);
        domain.Verify(x => x.UpdateAsync(entity, default), Times.Once);
        domain.Verify(x => x.ActivateAsync(It.IsAny<VehicleEntity>(), default), Times.Never);
        domain.Verify(x => x.DeactivateAsync(It.IsAny<VehicleEntity>(), default), Times.Never);
    }

    private static Mock<ICurrentUserService> CurrentUser()
    {
        var current = new Mock<ICurrentUserService>();
        current.Setup(x => x.GetTenantId()).Returns(7);
        current.Setup(x => x.GetUserId()).Returns(17);
        return current;
    }

    private static ILocalizationService Localization()
    {
        var localization = new Mock<ILocalizationService>();
        localization.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        return localization.Object;
    }
}
