using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientFiscalData;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Application.Mappings.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Domain.Validators.Business.ClientFiscalData;

namespace VianaHub.Global.Gerit.Tests.ClientFiscalData;

public class ClientFiscalDataAppServiceTests
{
    [Fact]
    public async Task Create_RejectsInactiveParentBeforePersistence()
    {
        var clients = new Mock<IClientRepository>();
        clients.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>())).Returns(Task.FromResult<ClientEntity?>(null));
        var domain = new Mock<IClientFiscalDataDomainService>();
        var service = Create(clients, domain);

        var result = await service.CreateAsync(10, new CreateClientFiscalDataRequest { TaxNumber = "PT123", FiscalCountry = "PT" }, default);

        Assert.Equal(0, result);
        domain.Verify(x => x.CreateAsync(It.IsAny<ClientFiscalDataEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_PreservesFiscalFieldsAndUsesDomain()
    {
        var clients = new Mock<IClientRepository>();
        clients.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new ClientEntity(7, 2, 8, null, null, "Client", null, null, false, null, null, null, null, null, null, null, null, 1, 1, 17));
        var repo = new Mock<IClientFiscalDataDataRepository>();
        repo.Setup(x => x.ExistsByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(x => x.ExistsByTaxIdentityAsync(7, "PT", "PT123", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var domain = new Mock<IClientFiscalDataDomainService>();
        domain.Setup(x => x.CreateAsync(It.IsAny<ClientFiscalDataEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(true).Callback<ClientFiscalDataEntity, CancellationToken>((e, _) => typeof(ClientFiscalDataEntity).GetProperty("Id")!.SetValue(e, 44));
        var service = Create(clients, domain, repo);

        var result = await service.CreateAsync(10, new CreateClientFiscalDataRequest { TaxNumber = "PT123", FiscalCountry = "PT", VatNumber = "VAT", IsVatRegistered = true, IBAN = "PT50000000000000000000000", FiscalEmail = "fiscal@example.test" }, default);

        Assert.Equal(44, result);
        domain.Verify(x => x.CreateAsync(It.Is<ClientFiscalDataEntity>(e => e.TaxNumber == "PT123" && e.VatNumber == "VAT" && e.FiscalCountry == "PT" && e.IsVatRegistered), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_RejectsTaxIdentityConflictBeforePersistence()
    {
        var clients = new Mock<IClientRepository>();
        clients.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new ClientEntity(7, 2, 8, null, null, "Client", null, null, false, null, null, null, null, null, null, null, null, 1, 1, 17));
        var repo = new Mock<IClientFiscalDataDataRepository>();
        repo.Setup(x => x.ExistsByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(x => x.ExistsByTaxIdentityAsync(7, "PT", "PT123", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var domain = new Mock<IClientFiscalDataDomainService>();
        var service = Create(clients, domain, repo);

        var result = await service.CreateAsync(10, new CreateClientFiscalDataRequest { TaxNumber = "PT123", FiscalCountry = "PT" }, default);

        Assert.Equal(0, result);
        domain.Verify(x => x.CreateAsync(It.IsAny<ClientFiscalDataEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DomainValidator_UsesLocalizedVatRequiredMessage()
    {
        var localization = new Mock<ILocalizationService>();
        localization.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key == "VatNumber_Required" ? "VAT_REQUIRED_LOCALIZED" : key);
        localization.Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object[]>())).Returns((string key, object[] _) => key == "VatNumber_Required" ? "VAT_REQUIRED_LOCALIZED" : key);
        var validator = new ClientFiscalDataValidator(localization.Object);
        var entity = new ClientFiscalDataEntity(7, 10, "PT123", null, "PT", true, null, null, 17);

        var result = await validator.ValidateForCreateAsync(entity);

        Assert.Contains(result.Errors, error => error.ErrorMessage == "VAT_REQUIRED_LOCALIZED");
    }

    [Fact]
    public void DetailMapping_ResolvesClientTypeTranslationAndRemainsSafeWithoutNavigation()
    {
        var partyType = new PartyTypeEntity("ORG", 17);
        partyType.Translations.Add(new PartyTypeTranslationsEntity(2, "pt-PT", "Organização", ""));
        typeof(PartyTypeEntity).GetProperties().Single(p => p.Name == "Id" && p.PropertyType == typeof(byte)).SetValue(partyType, (byte)2);
        var client = new ClientEntity(7, 2, 8, null, null, "Client", null, null, false, null, null, null, null, null, null, null, null, 1, 1, 17);
        typeof(ClientEntity).GetProperty("PartyType")!.SetValue(client, partyType);
        var fiscal = new ClientFiscalDataEntity(7, 10, "PT123", "VAT", "PT", true, "PT50000000000000000000000", "fiscal@example.test", 17);
        typeof(ClientFiscalDataEntity).GetProperty("Id")!.SetValue(fiscal, 44);
        typeof(ClientFiscalDataEntity).GetProperty("Client")!.SetValue(fiscal, client);

        var response = ClientFiscalDataMapper.ToDetail(fiscal);

        Assert.Equal(44, response.Id);
        Assert.Equal(10, response.ClientId);
        Assert.Equal("Client", response.Client);
        Assert.Equal(2, response.ClientTypeId);
        Assert.Equal("Organização", response.ClientType);
        Assert.Equal("PT123", response.TaxNumber);
        Assert.Equal("PT", response.FiscalCountry);
    }

    [Fact]
    public void Mapperly_MapsSimpleResponseAndPaginationMetadata()
    {
        var entity = new ClientFiscalDataEntity(7, 10, "PT123", null, "PT", false, null, null, 17);
        typeof(ClientFiscalDataEntity).GetProperty("Id")!.SetValue(entity, 44);
        var page = new ListPage<ClientFiscalDataEntity>(new[] { entity }, 2, 1, 3, 3);

        var response = ClientFiscalDataMapper.ToResponse(entity);
        var paged = ClientFiscalDataMapper.ToPaged(page);

        Assert.Equal(44, response.Id);
        Assert.Equal("PT123", response.TaxNumber);
        Assert.True(response.IsActive);
        Assert.Equal(2, paged.PageNumber);
        Assert.Equal(1, paged.PageSize);
        Assert.Equal(3, paged.TotalItems);
        Assert.Equal(3, paged.TotalPages);
        Assert.Single(paged.Items);
    }

    private static ClientFiscalDataAppService Create(Mock<IClientRepository> clients, Mock<IClientFiscalDataDomainService> domain, Mock<IClientFiscalDataDataRepository>? repo = null)
    {
        var localization = new Mock<ILocalizationService>();
        localization.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        var current = new Mock<ICurrentUserService>();
        current.Setup(x => x.GetTenantId()).Returns(7);
        current.Setup(x => x.GetUserId()).Returns(17);
        return new ClientFiscalDataAppService(repo?.Object ?? Mock.Of<IClientFiscalDataDataRepository>(), clients.Object, domain.Object, Mock.Of<INotify>(), localization.Object, current.Object, Mock.Of<IFileValidationService>());
    }
}
