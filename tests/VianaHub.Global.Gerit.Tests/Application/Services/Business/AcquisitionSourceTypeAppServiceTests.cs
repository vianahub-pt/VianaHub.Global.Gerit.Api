using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.AcquisitionSourceType;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using Xunit;

namespace VianaHub.Global.Gerit.Tests.Application.Services.Business;

public class AcquisitionSourceTypeAppServiceTests
{
    [Fact(DisplayName = "AcquisitionSourceTypeAppService - GetAllAsync retorna entidades mapeadas")]
    [Trait("Application", "AcquisitionSourceType")]
    public async Task GetAllAsync_ShouldReturnMappedEntities_WhenEntitiesExist()
    {
        var repoMock = new Mock<IAcquisitionSourceTypeDataRepository>();
        var domainMock = new Mock<IAcquisitionSourceTypeDomainService>();
        var mapperMock = new Mock<IMapper>();
        var notifyMock = new Mock<INotify>();
        var localizationMock = new Mock<ILocalizationService>();
        var currentUserMock = new Mock<ICurrentUserService>();
        var fileValidationMock = new Mock<IFileValidationService>();
        var loggerMock = new Mock<ILogger<AcquisitionSourceTypeAppService>>();

        var entities = new List<AcquisitionSourceTypeEntity>
        {
            new("CODE1", 1),
            new("CODE2", 1)
        };

        repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entities);

        var service = CreateService(repoMock, domainMock, mapperMock, notifyMock, localizationMock, currentUserMock, fileValidationMock, loggerMock);

        await service.GetAllAsync(CancellationToken.None);

        mapperMock.Verify(m => m.Map<IEnumerable<object>>(entities), Times.Once);
    }

    [Fact(DisplayName = "AcquisitionSourceTypeAppService - CreateAsync cria entidade e tradução pt-PT")]
    [Trait("Application", "AcquisitionSourceType")]
    public async Task CreateAsync_ShouldCreateEntityWithTranslation_WhenValidRequest()
    {
        var repoMock = new Mock<IAcquisitionSourceTypeDataRepository>();
        var domainMock = new Mock<IAcquisitionSourceTypeDomainService>();
        var mapperMock = new Mock<IMapper>();
        var notifyMock = new Mock<INotify>();
        var localizationMock = new Mock<ILocalizationService>();
        var currentUserMock = new Mock<ICurrentUserService>();
        var fileValidationMock = new Mock<IFileValidationService>();
        var loggerMock = new Mock<ILogger<AcquisitionSourceTypeAppService>>();

        currentUserMock.Setup(x => x.GetUserId()).Returns(5);

        AcquisitionSourceTypeEntity? capturedEntity = null;
        domainMock.Setup(d => d.CreateAsync(It.IsAny<AcquisitionSourceTypeEntity>(), It.IsAny<CancellationToken>()))
            .Callback<AcquisitionSourceTypeEntity, CancellationToken>((entity, _) =>
            {
                capturedEntity = entity;
                typeof(AcquisitionSourceTypeEntity)
                    .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .SetValue(entity, 42);
            })
            .ReturnsAsync(true);

        var service = CreateService(repoMock, domainMock, mapperMock, notifyMock, localizationMock, currentUserMock, fileValidationMock, loggerMock);

        var request = new CreateAcquisitionSourceTypeRequest { Translations = [new() { LanguageCode = "pt-PT", Name = "Test Name", Description = "Test Description" }, new() { LanguageCode = "en-US", Name = "Test Name", Description = "Test Description" }] };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal(42, result);
        Assert.NotNull(capturedEntity);
        Assert.Equal("TEST_NAME", capturedEntity.Code);
        Assert.Equal(2, capturedEntity.Translations.Count);
        Assert.Equal("pt-PT", capturedEntity.Translations.First().LanguageCode);
        Assert.Equal("Test Name", capturedEntity.Translations.First().Name);
        Assert.Equal("Test Description", capturedEntity.Translations.First().Description);
    }

    [Fact(DisplayName = "AcquisitionSourceTypeAppService - CreateAsync retorna 0 quando nome já existe")]
    [Trait("Application", "AcquisitionSourceType")]
    public async Task CreateAsync_ShouldReturnZero_WhenNameAlreadyExists()
    {
        var repoMock = new Mock<IAcquisitionSourceTypeDataRepository>();
        var domainMock = new Mock<IAcquisitionSourceTypeDomainService>();
        var mapperMock = new Mock<IMapper>();
        var notifyMock = new Mock<INotify>();
        var localizationMock = new Mock<ILocalizationService>();
        var currentUserMock = new Mock<ICurrentUserService>();
        var fileValidationMock = new Mock<IFileValidationService>();
        var loggerMock = new Mock<ILogger<AcquisitionSourceTypeAppService>>();

        repoMock.Setup(r => r.ExistsByTranslationAsync("pt-PT", "Existing", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        localizationMock.Setup(l => l.GetMessage("Application.Catalog.TranslationAlreadyExists")).Returns("name-exists");

        var service = CreateService(repoMock, domainMock, mapperMock, notifyMock, localizationMock, currentUserMock, fileValidationMock, loggerMock);

        var request = new CreateAcquisitionSourceTypeRequest { Translations = [new() { LanguageCode = "pt-PT", Name = "Existing", Description = null }, new() { LanguageCode = "en-US", Name = "Existing", Description = null }] };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal(0, result);
        notifyMock.Verify(n => n.Add("name-exists", 409), Times.Once);
    }

    [Fact(DisplayName = "AcquisitionSourceTypeAppService - UpdateAsync atualiza tradução pt-PT")]
    [Trait("Application", "AcquisitionSourceType")]
    public async Task UpdateAsync_ShouldUpdateTranslation_WhenTranslationExists()
    {
        var repoMock = new Mock<IAcquisitionSourceTypeDataRepository>();
        var domainMock = new Mock<IAcquisitionSourceTypeDomainService>();
        var mapperMock = new Mock<IMapper>();
        var notifyMock = new Mock<INotify>();
        var localizationMock = new Mock<ILocalizationService>();
        var currentUserMock = new Mock<ICurrentUserService>();
        var fileValidationMock = new Mock<IFileValidationService>();
        var loggerMock = new Mock<ILogger<AcquisitionSourceTypeAppService>>();

        currentUserMock.Setup(x => x.GetUserId()).Returns(5);

        var entity = new AcquisitionSourceTypeEntity("CODE", 1);
        entity.AddTranslation(new AcquisitionSourceTypeTranslationsEntity(0, "pt-PT", "Old Name", "Old Desc"));
        typeof(AcquisitionSourceTypeEntity)
            .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(entity, 10);

        repoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        domainMock.Setup(d => d.UpdateAsync(It.IsAny<AcquisitionSourceTypeEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateService(repoMock, domainMock, mapperMock, notifyMock, localizationMock, currentUserMock, fileValidationMock, loggerMock);

        var request = new UpdateAcquisitionSourceTypeRequest
        {
            Name = "New Name",
            Description = "New Desc"
        };

        var result = await service.UpdateAsync(10, request, CancellationToken.None);

        Assert.True(result);
        var translation = entity.Translations.First();
        Assert.Equal("New Name", translation.Name);
        Assert.Equal("New Desc", translation.Description);
    }

    [Fact(DisplayName = "AcquisitionSourceTypeAppService - UpdateAsync cria tradução quando não existe")]
    [Trait("Application", "AcquisitionSourceType")]
    public async Task UpdateAsync_ShouldCreateTranslation_WhenTranslationDoesNotExist()
    {
        var repoMock = new Mock<IAcquisitionSourceTypeDataRepository>();
        var domainMock = new Mock<IAcquisitionSourceTypeDomainService>();
        var mapperMock = new Mock<IMapper>();
        var notifyMock = new Mock<INotify>();
        var localizationMock = new Mock<ILocalizationService>();
        var currentUserMock = new Mock<ICurrentUserService>();
        var fileValidationMock = new Mock<IFileValidationService>();
        var loggerMock = new Mock<ILogger<AcquisitionSourceTypeAppService>>();

        currentUserMock.Setup(x => x.GetUserId()).Returns(5);

        // Entidade sem tradução pt-PT
        var entity = new AcquisitionSourceTypeEntity("CODE", 1);
        typeof(AcquisitionSourceTypeEntity)
            .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(entity, 10);

        repoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        domainMock.Setup(d => d.UpdateAsync(It.IsAny<AcquisitionSourceTypeEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateService(repoMock, domainMock, mapperMock, notifyMock, localizationMock, currentUserMock, fileValidationMock, loggerMock);

        var request = new UpdateAcquisitionSourceTypeRequest { Name = "New Name", Description = "New Desc" };

        var result = await service.UpdateAsync(10, request, CancellationToken.None);

        Assert.True(result);
        Assert.Single(entity.Translations);
        Assert.Equal("pt-PT", entity.Translations.First().LanguageCode);
        Assert.Equal("New Name", entity.Translations.First().Name);
    }

    private static AcquisitionSourceTypeAppService CreateService(
        Mock<IAcquisitionSourceTypeDataRepository> repoMock,
        Mock<IAcquisitionSourceTypeDomainService> domainMock,
        Mock<IMapper> mapperMock,
        Mock<INotify> notifyMock,
        Mock<ILocalizationService> localizationMock,
        Mock<ICurrentUserService> currentUserMock,
        Mock<IFileValidationService> fileValidationMock,
        Mock<ILogger<AcquisitionSourceTypeAppService>> loggerMock)
    {
        return new AcquisitionSourceTypeAppService(
            repoMock.Object,
            domainMock.Object,
            mapperMock.Object,
            notifyMock.Object,
            localizationMock.Object,
            currentUserMock.Object,
            fileValidationMock.Object,
            loggerMock.Object);
    }
}
