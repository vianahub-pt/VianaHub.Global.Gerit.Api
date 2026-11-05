using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.AddressType;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using Xunit;

namespace VianaHub.Global.Gerit.Tests.Application.Services.Business;

public class AddressTypeAppServiceTests
{
    [Fact(DisplayName = "AddressTypeAppService - CreateAsync cria entidade e tradução pt-PT")]
    [Trait("Application", "AddressType")]
    public async Task CreateAsync_ShouldCreateEntityWithTranslation_WhenValidRequest()
    {
        var repoMock = new Mock<IAddressTypeDataRepository>();
        var domainMock = new Mock<IAddressTypeDomainService>();
        var mapperMock = new Mock<IMapper>();
        var notifyMock = new Mock<INotify>();
        var localizationMock = new Mock<ILocalizationService>();
        var currentUserMock = new Mock<ICurrentUserService>();
        var fileValidationMock = new Mock<IFileValidationService>();
        var loggerMock = new Mock<ILogger<AddressTypeAppService>>();

        currentUserMock.Setup(x => x.GetTenantId()).Returns(1);
        currentUserMock.Setup(x => x.GetUserId()).Returns(5);

        AddressTypeEntity? capturedEntity = null;
        domainMock.Setup(d => d.CreateAsync(It.IsAny<AddressTypeEntity>(), It.IsAny<CancellationToken>()))
            .Callback<AddressTypeEntity, CancellationToken>((entity, _) =>
            {
                capturedEntity = entity;
                typeof(AddressTypeEntity)
                    .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .SetValue(entity, 42);
            })
            .ReturnsAsync(true);

        var service = CreateService(repoMock, domainMock, mapperMock, notifyMock, localizationMock, currentUserMock, fileValidationMock, loggerMock);

        var request = new CreateAddressTypeRequest { Translations = [new() { LanguageCode = "pt-PT", Name = "Residencial", Description = "Endereço residencial" }, new() { LanguageCode = "en-US", Name = "Residential", Description = "Endereço residencial" }] };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal(42, result);
        Assert.NotNull(capturedEntity);
        Assert.Equal(2, capturedEntity.Translations.Count);
        Assert.Equal("pt-PT", capturedEntity.Translations.First().LanguageCode);
        Assert.Equal("Residencial", capturedEntity.Translations.First().Name);
        Assert.Equal("Endereço residencial", capturedEntity.Translations.First().Description);
    }

    [Fact(DisplayName = "AddressTypeAppService - CreateAsync retorna 0 quando já existe")]
    [Trait("Application", "AddressType")]
    public async Task CreateAsync_ShouldReturnZero_WhenNameAlreadyExists()
    {
        var repoMock = new Mock<IAddressTypeDataRepository>();
        var domainMock = new Mock<IAddressTypeDomainService>();
        var mapperMock = new Mock<IMapper>();
        var notifyMock = new Mock<INotify>();
        var localizationMock = new Mock<ILocalizationService>();
        var currentUserMock = new Mock<ICurrentUserService>();
        var fileValidationMock = new Mock<IFileValidationService>();
        var loggerMock = new Mock<ILogger<AddressTypeAppService>>();

        currentUserMock.Setup(x => x.GetTenantId()).Returns(1);
        repoMock.Setup(r => r.ExistsByTranslationAsync("pt-PT", "Residencial", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        localizationMock.Setup(l => l.GetMessage("Application.Catalog.TranslationAlreadyExists")).Returns("exists");

        var service = CreateService(repoMock, domainMock, mapperMock, notifyMock, localizationMock, currentUserMock, fileValidationMock, loggerMock);

        var request = new CreateAddressTypeRequest { Translations = [new() { LanguageCode = "pt-PT", Name = "Residencial", Description = "Desc" }, new() { LanguageCode = "en-US", Name = "Residential", Description = "Desc" }] };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal(0, result);
        notifyMock.Verify(n => n.Add("exists", 409), Times.Once);
    }

    [Fact(DisplayName = "AddressTypeAppService - UpdateAsync atualiza tradução pt-PT existente")]
    [Trait("Application", "AddressType")]
    public async Task UpdateAsync_ShouldUpdateTranslation_WhenTranslationExists()
    {
        var repoMock = new Mock<IAddressTypeDataRepository>();
        var domainMock = new Mock<IAddressTypeDomainService>();
        var mapperMock = new Mock<IMapper>();
        var notifyMock = new Mock<INotify>();
        var localizationMock = new Mock<ILocalizationService>();
        var currentUserMock = new Mock<ICurrentUserService>();
        var fileValidationMock = new Mock<IFileValidationService>();
        var loggerMock = new Mock<ILogger<AddressTypeAppService>>();

        currentUserMock.Setup(x => x.GetTenantId()).Returns(1);
        currentUserMock.Setup(x => x.GetUserId()).Returns(5);

        var entity = new AddressTypeEntity(1);
        entity.AddTranslation(new AddressTypeTranslationsEntity(0, "pt-PT", "Old Name", "Old Desc"));
        typeof(AddressTypeEntity)
            .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(entity, 10);

        repoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        domainMock.Setup(d => d.UpdateAsync(It.IsAny<AddressTypeEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateService(repoMock, domainMock, mapperMock, notifyMock, localizationMock, currentUserMock, fileValidationMock, loggerMock);

        var request = new UpdateAddressTypeRequest { Name = "New Name", Description = "New Desc" };

        var result = await service.UpdateAsync(10, request, CancellationToken.None);

        Assert.True(result);
        var translation = entity.Translations.First();
        Assert.Equal("New Name", translation.Name);
        Assert.Equal("New Desc", translation.Description);
    }

    private static AddressTypeAppService CreateService(
        Mock<IAddressTypeDataRepository> repoMock,
        Mock<IAddressTypeDomainService> domainMock,
        Mock<IMapper> mapperMock,
        Mock<INotify> notifyMock,
        Mock<ILocalizationService> localizationMock,
        Mock<ICurrentUserService> currentUserMock,
        Mock<IFileValidationService> fileValidationMock,
        Mock<ILogger<AddressTypeAppService>> loggerMock)
    {
        return new AddressTypeAppService(
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
