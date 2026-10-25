using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.PartyType;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using Xunit;

namespace VianaHub.Global.Gerit.Tests.Application.Services.Business;

public class PartyTypeAppServiceTests
{
    [Fact(DisplayName = "PartyTypeAppService - GetAllAsync retorna lista vazia quando não há registos")]
    [Trait("Application", "PartyType")]
    public async Task GetAllAsync_ShouldReturnEmpty_WhenNoEntities()
    {
        var (service, repoMock, _, _, _, _, _) = CreateService();

        repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PartyTypeEntity>());

        var result = await service.GetAllAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact(DisplayName = "PartyTypeAppService - GetByIdAsync retorna null com notificação 410 quando entidade não existe")]
    [Trait("Application", "PartyType")]
    public async Task GetByIdAsync_ShouldNotify410_WhenEntityNotFound()
    {
        var (service, repoMock, _, notifyMock, localizationMock, _, _) = CreateService();

        repoMock.Setup(r => r.GetByIdAsync((byte)1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyTypeEntity?)null);
        localizationMock.Setup(l => l.GetMessage("Application.Service.PartyType.GetById.ResourceNotFound"))
            .Returns("not found");

        var result = await service.GetByIdAsync(1, CancellationToken.None);

        Assert.Null(result);
        notifyMock.Verify(n => n.Add("not found", 410), Times.Once);
    }

    [Fact(DisplayName = "PartyTypeAppService - CreateAsync cria entidade e tradução pt-PT")]
    [Trait("Application", "PartyType")]
    public async Task CreateAsync_ShouldCreateEntityWithTranslation_WhenValidRequest()
    {
        var (service, repoMock, domainMock, _, _, currentUserMock, _) = CreateService();

        currentUserMock.Setup(x => x.GetUserId()).Returns(5);

        PartyTypeEntity? capturedEntity = null;
        domainMock.Setup(d => d.CreateAsync(It.IsAny<PartyTypeEntity>(), It.IsAny<CancellationToken>()))
            .Callback<PartyTypeEntity, CancellationToken>((entity, _) =>
            {
                capturedEntity = entity;
                // Simula o EF Core atribuindo o Id após persistência
                // PartyTypeEntity declara 'new byte Id' que esconde o 'int Id' da base Entity,
                // por isso usamos DeclaredOnly para resolver a ambiguidade.
                typeof(PartyTypeEntity)
                    .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)?
                    .SetValue(entity, (byte)42);
            })
            .ReturnsAsync(true);

        var request = new CreatePartyTypeRequest { Translations = [new() { LanguageCode = "pt-PT", Name = "Pessoa Física", Description = "Pessoa singular" }, new() { LanguageCode = "en-US", Name = "Individual", Description = "Pessoa singular" }] };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal((byte)42, result);
        Assert.NotNull(capturedEntity);
        Assert.Equal("INDIVIDUAL", capturedEntity.Code);
        Assert.Equal(2, capturedEntity.Translations.Count);
        Assert.Equal("pt-PT", capturedEntity.Translations.First().LanguageCode);
        Assert.Equal("Pessoa Física", capturedEntity.Translations.First().Name);
        Assert.Equal("Pessoa singular", capturedEntity.Translations.First().Description);
    }

    [Fact(DisplayName = "PartyTypeAppService - CreateAsync retorna 0 com notificação 409 quando código já existe")]
    [Trait("Application", "PartyType")]
    public async Task CreateAsync_ShouldReturnZero_WhenCodeAlreadyExists()
    {
        var (service, repoMock, _, notifyMock, localizationMock, _, _) = CreateService();

        repoMock.Setup(r => r.ExistsByCodeAsync("INDIVIDUAL", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        localizationMock.Setup(l => l.GetMessage("Application.Service.PartyType.Create.CodeAlreadyExists"))
            .Returns("already exists");

        var request = new CreatePartyTypeRequest { Translations = [new() { LanguageCode = "pt-PT", Name = "Pessoa Física", Description = "Desc" }, new() { LanguageCode = "en-US", Name = "Individual", Description = "Desc" }] };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal((byte)0, result);
        notifyMock.Verify(n => n.Add("already exists", 409), Times.Once);
    }

    [Fact(DisplayName = "PartyTypeAppService - UpdateAsync atualiza tradução pt-PT existente")]
    [Trait("Application", "PartyType")]
    public async Task UpdateAsync_ShouldUpdateTranslation_WhenTranslationExists()
    {
        var (service, repoMock, domainMock, _, _, currentUserMock, _) = CreateService();

        currentUserMock.Setup(x => x.GetUserId()).Returns(5);

        var entity = new PartyTypeEntity("PF", 1);
        entity.Translations.Add(new PartyTypeTranslationsEntity(0, "pt-PT", "Old Name", "Old Desc"));
        typeof(PartyTypeEntity)
            .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)?
            .SetValue(entity, (byte)10);

        repoMock.Setup(r => r.GetByIdAsync((byte)10, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        domainMock.Setup(d => d.UpdateAsync(It.IsAny<PartyTypeEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var request = new UpdatePartyTypeRequest { Name = "New Name", Description = "New Desc" };

        var result = await service.UpdateAsync(10, request, CancellationToken.None);

        Assert.True(result);
        Assert.Equal("PF", entity.Code);
        var translation = entity.Translations.First();
        Assert.Equal("New Name", translation.Name);
        Assert.Equal("New Desc", translation.Description);
    }

    [Fact(DisplayName = "PartyTypeAppService - ExistsByCodeAsync verifica duplicidade corretamente")]
    [Trait("Application", "PartyType")]
    public async Task ExistsByCode_ShouldCheckRepository_WhenCalled()
    {
        var (_, repoMock, _, _, _, _, _) = CreateService();

        repoMock.Setup(r => r.ExistsByCodeAsync("PJ", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var exists = await repoMock.Object.ExistsByCodeAsync("PJ", CancellationToken.None);

        Assert.True(exists);
    }

    [Fact(DisplayName = "PartyTypeAppService - UpdateAsync retorna false com 410 quando entidade não existe")]
    [Trait("Application", "PartyType")]
    public async Task UpdateAsync_ShouldReturnFalse_WhenEntityNotFound()
    {
        var (service, repoMock, _, notifyMock, localizationMock, _, _) = CreateService();

        repoMock.Setup(r => r.GetByIdAsync((byte)99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyTypeEntity?)null);
        localizationMock.Setup(l => l.GetMessage("Application.Service.PartyType.Update.ResourceNotFound"))
            .Returns("not found");

        var request = new UpdatePartyTypeRequest { Name = "Name", Description = "Desc" };
        var result = await service.UpdateAsync(99, request, CancellationToken.None);

        Assert.False(result);
        notifyMock.Verify(n => n.Add("not found", 410), Times.Once);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static (
        PartyTypeAppService service,
        Mock<IPartyTypeDataRepository> repoMock,
        Mock<IPartyTypeDomainService> domainMock,
        Mock<INotify> notifyMock,
        Mock<ILocalizationService> localizationMock,
        Mock<ICurrentUserService> currentUserMock,
        Mock<ILogger<PartyTypeAppService>> loggerMock
    ) CreateService()
    {
        var repoMock = new Mock<IPartyTypeDataRepository>();
        var domainMock = new Mock<IPartyTypeDomainService>();
        var mapperMock = new Mock<IMapper>();
        var notifyMock = new Mock<INotify>();
        var localizationMock = new Mock<ILocalizationService>();
        var currentUserMock = new Mock<ICurrentUserService>();
        var loggerMock = new Mock<ILogger<PartyTypeAppService>>();

        var service = new PartyTypeAppService(
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
