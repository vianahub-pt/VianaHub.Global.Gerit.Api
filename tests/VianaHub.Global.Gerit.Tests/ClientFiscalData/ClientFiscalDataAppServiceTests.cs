using AutoMapper;
using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientFiscalData;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Tests.ClientFiscalData;

public class ClientFiscalDataAppServiceTests
{
    [Fact]
    public async Task Create_RejectsInactiveParentBeforePersistence()
    {
        var clients = new Mock<IClientRepository>();
        clients.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync((ClientEntity?)null);
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

    private static ClientFiscalDataAppService Create(Mock<IClientRepository> clients, Mock<IClientFiscalDataDomainService> domain, Mock<IClientFiscalDataDataRepository>? repo = null)
    {
        var localization = new Mock<ILocalizationService>();
        localization.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        var current = new Mock<ICurrentUserService>();
        current.Setup(x => x.GetTenantId()).Returns(7);
        current.Setup(x => x.GetUserId()).Returns(17);
        return new ClientFiscalDataAppService(repo?.Object ?? Mock.Of<IClientFiscalDataDataRepository>(), clients.Object, domain.Object, Mock.Of<IMapper>(), Mock.Of<INotify>(), localization.Object, current.Object, Mock.Of<IFileValidationService>());
    }
}
