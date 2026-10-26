using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDomain;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using Xunit;

namespace VianaHub.Global.Gerit.Tests.Application.Services.Business;

public class StatusDomainAppServiceTests
{
    [Fact(DisplayName = "StatusDomainAppService - GetAllAsync retorna lista vazia quando não há registos")]
    [Trait("Application", "StatusDomain")]
    public async Task GetAllAsync_ShouldReturnEmpty_WhenNoEntities()
    {
        var (service, repoMock, _, _, _, _, _) = CreateService();

        repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusDomainEntity>());

        var result = await service.GetAllAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact(DisplayName = "StatusDomainAppService - GetByIdAsync retorna null com notificação 410 quando entidade não existe")]
    [Trait("Application", "StatusDomain")]
    public async Task GetByIdAsync_ShouldNotify410_WhenEntityNotFound()
    {
        var (service, repoMock, _, notifyMock, localizationMock, _, _) = CreateService();

        repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StatusDomainEntity?)null);
        localizationMock.Setup(l => l.GetMessage("Application.Service.StatusDomain.GetById.ResourceNotFound"))
            .Returns("not found");

        var result = await service.GetByIdAsync(1, CancellationToken.None);

        Assert.Null(result);
        notifyMock.Verify(n => n.Add("not found", 410), Times.Once);
    }

    [Fact(DisplayName = "StatusDomainAppService - CreateAsync cria entidade e tradução pt-PT")]
    [Trait("Application", "StatusDomain")]
    public async Task CreateAsync_ShouldCreateEntityWithTranslation_WhenValidRequest()
    {
        var (service, repoMock, domainMock, _, _, currentUserMock, _) = CreateService();

        currentUserMock.Setup(x => x.GetUserId()).Returns(5);

        StatusDomainEntity? capturedEntity = null;
        domainMock.Setup(d => d.CreateAsync(It.IsAny<StatusDomainEntity>(), It.IsAny<CancellationToken>()))
            .Callback<StatusDomainEntity, CancellationToken>((entity, _) =>
            {
                capturedEntity = entity;
                // Simula o EF Core atribuindo o Id após persistência
                typeof(StatusDomainEntity)
                    .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .SetValue(entity, 42);
            })
            .ReturnsAsync(true);

        var request = new CreateStatusDomainRequest { Translations = [new() { LanguageCode = "pt-PT", Name = "Visit Status", Description = "Status for visits" }, new() { LanguageCode = "en-US", Name = "Visit Status", Description = "Status for visits" }] };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal(42, result);
        Assert.NotNull(capturedEntity);
        Assert.Equal("VISIT_STATUS", capturedEntity.Code);
        Assert.Equal(2, capturedEntity.Translations.Count);
        Assert.Equal("pt-PT", capturedEntity.Translations.First().LanguageCode);
        Assert.Equal("Visit Status", capturedEntity.Translations.First().Name);
        Assert.Equal("Status for visits", capturedEntity.Translations.First().Description);
    }

    [Fact(DisplayName = "StatusDomainAppService - CreateAsync retorna 0 com notificação 409 quando código já existe")]
    [Trait("Application", "StatusDomain")]
    public async Task CreateAsync_ShouldReturnZero_WhenCodeAlreadyExists()
    {
        var (service, repoMock, _, notifyMock, localizationMock, _, _) = CreateService();

        repoMock.Setup(r => r.ExistsByCodeAsync("VISIT_STATUS", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        localizationMock.Setup(l => l.GetMessage("Application.Service.StatusDomain.Create.CodeAlreadyExists"))
            .Returns("already exists");

        var request = new CreateStatusDomainRequest { Translations = [new() { LanguageCode = "pt-PT", Name = "Visit Status", Description = "Desc" }, new() { LanguageCode = "en-US", Name = "Visit Status", Description = "Desc" }] };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal(0, result);
        notifyMock.Verify(n => n.Add("already exists", 409), Times.Once);
    }

    [Fact(DisplayName = "StatusDomainAppService - UpdateAsync atualiza tradução pt-PT existente")]
    [Trait("Application", "StatusDomain")]
    public async Task UpdateAsync_ShouldUpdateTranslation_WhenTranslationExists()
    {
        var (service, repoMock, domainMock, _, _, currentUserMock, _) = CreateService();

        currentUserMock.Setup(x => x.GetUserId()).Returns(5);

        var entity = new StatusDomainEntity("VISIT", 1);
        entity.Translations.Add(new StatusDomainTranslationsEntity(0, "pt-PT", "Old Name", "Old Desc"));
        typeof(StatusDomainEntity)
            .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(entity, 10);

        repoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        domainMock.Setup(d => d.UpdateAsync(It.IsAny<StatusDomainEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var request = new UpdateStatusDomainRequest { Name = "New Name", Description = "New Desc" };

        var result = await service.UpdateAsync(10, request, CancellationToken.None);

        Assert.True(result);
        Assert.Equal("VISIT", entity.Code);
        var translation = entity.Translations.First();
        Assert.Equal("New Name", translation.Name);
        Assert.Equal("New Desc", translation.Description);
    }

    [Fact(DisplayName = "StatusDomainAppService - UpdateAsync retorna false com 410 quando entidade não existe")]
    [Trait("Application", "StatusDomain")]
    public async Task UpdateAsync_ShouldReturnFalse_WhenEntityNotFound()
    {
        var (service, repoMock, _, notifyMock, localizationMock, _, _) = CreateService();

        repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StatusDomainEntity?)null);
        localizationMock.Setup(l => l.GetMessage("Application.Service.StatusDomain.Update.ResourceNotFound"))
            .Returns("not found");

        var request = new UpdateStatusDomainRequest { Name = "Name", Description = "Desc" };
        var result = await service.UpdateAsync(99, request, CancellationToken.None);

        Assert.False(result);
        notifyMock.Verify(n => n.Add("not found", 410), Times.Once);
    }

    [Fact(DisplayName = "StatusDomainAppService - ExistsByCodeAsync verifica duplicidade corretamente")]
    [Trait("Application", "StatusDomain")]
    public async Task ExistsByCode_ShouldCheckRepository_WhenCalled()
    {
        var (_, repoMock, _, _, _, _, _) = CreateService();

        repoMock.Setup(r => r.ExistsByCodeAsync("EQUIP", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var exists = await repoMock.Object.ExistsByCodeAsync("EQUIP", CancellationToken.None);

        Assert.True(exists);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static (
        StatusDomainAppService service,
        Mock<IStatusDomainDataRepository> repoMock,
        Mock<IStatusDomainDomainService> domainMock,
        Mock<INotify> notifyMock,
        Mock<ILocalizationService> localizationMock,
        Mock<ICurrentUserService> currentUserMock,
        Mock<ILogger<StatusDomainAppService>> loggerMock
    ) CreateService()
    {
        var repoMock = new Mock<IStatusDomainDataRepository>();
        var domainMock = new Mock<IStatusDomainDomainService>();
        var mapperMock = new Mock<IMapper>();
        var notifyMock = new Mock<INotify>();
        var localizationMock = new Mock<ILocalizationService>();
        var currentUserMock = new Mock<ICurrentUserService>();
        var loggerMock = new Mock<ILogger<StatusDomainAppService>>();

        var service = new StatusDomainAppService(
            repoMock.Object,
            domainMock.Object,
            mapperMock.Object,
            notifyMock.Object,
            localizationMock.Object,
            currentUserMock.Object,
            loggerMock.Object);

        return (service, repoMock, domainMock, notifyMock, localizationMock, currentUserMock, loggerMock);
    }
}
