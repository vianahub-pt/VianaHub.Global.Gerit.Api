using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Function;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Tests.VisitTeamFunctions;

public class VisitTeamFunctionIsActiveTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(null, true)]
    public async Task Create_UsesRequestedStateOrHistoricalDefault(bool? requestedState, bool expectedState)
    {
        var (service, domain, _) = CreateService();
        VisitTeamFunctionsEntity? saved = null;
        domain.Setup(x => x.CreateAsync(It.IsAny<VisitTeamFunctionsEntity>(), default))
            .Callback<VisitTeamFunctionsEntity, CancellationToken>((entity, _) => saved = entity)
            .ReturnsAsync(true);

        await service.CreateAsync(9, new CreateVisitTeamFunctionRequest { Name = "Function", Description = "Description", IsActive = requestedState }, default);

        Assert.NotNull(saved);
        Assert.Equal(expectedState, saved!.IsActive);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(null, false)]
    public async Task Update_UsesRequestedStateOrPreservesExisting(bool? requestedState, bool existingState)
    {
        var (service, domain, repository) = CreateService();
        var entity = new VisitTeamFunctionsEntity(7, "Function", "Description", 17, existingState);
        typeof(VisitTeamFunctionsEntity).GetProperty("Id")!.SetValue(entity, 42);
        repository.Setup(x => x.GetByIdAsync(42, default)).ReturnsAsync(entity);
        domain.Setup(x => x.UpdateAsync(entity, default)).ReturnsAsync(true);

        Assert.True(await service.UpdateAsync(9, 42, new UpdateVisitTeamFunctionRequest { Name = "Function", Description = "Edited", IsActive = requestedState }, default));
        Assert.Equal(requestedState ?? existingState, entity.IsActive);
        domain.Verify(x => x.UpdateAsync(entity, default), Times.Once);
        domain.Verify(x => x.ActivateAsync(It.IsAny<VisitTeamFunctionsEntity>(), default), Times.Never);
        domain.Verify(x => x.DeactivateAsync(It.IsAny<VisitTeamFunctionsEntity>(), default), Times.Never);
    }

    private static (VisitTeamFunctionAppService Service, Mock<IVisitTeamFunctionDomainService> Domain, Mock<IVisitTeamFunctionDataRepository> Repository) CreateService()
    {
        var repository = new Mock<IVisitTeamFunctionDataRepository>();
        var domain = new Mock<IVisitTeamFunctionDomainService>();
        var localization = new Mock<ILocalizationService>();
        localization.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetTenantId()).Returns(7);
        currentUser.Setup(x => x.GetUserId()).Returns(17);
        var service = new VisitTeamFunctionAppService(repository.Object, domain.Object, new Notify(), localization.Object,
            currentUser.Object, NullLogger<VisitTeamFunctionAppService>.Instance, Mock.Of<IFileValidationService>());
        return (service, domain, repository);
    }
}
