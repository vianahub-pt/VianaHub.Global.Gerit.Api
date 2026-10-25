using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Client;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using Xunit;

namespace VianaHub.Global.Gerit.Tests.Application.Services.Business;

public class ClientAppServiceTests
{
    [Fact(DisplayName = "ClientAppService - GetByIdAsync retorna nulo e notifica quando nao existe")]
    [Trait("Application", "")]
    public async Task GetByIdAsync_ShouldReturnNullAndNotify_WhenAggregateDoesNotExist()
    {
        var repoMock = new Mock<IClientRepository>();
        var domainMock = new Mock<IClientDomainService>();
        var mapperMock = new Mock<IMapper>();
        var notifyMock = new Mock<INotify>();
        var localizationMock = new Mock<ILocalizationService>();
        var currentUserMock = new Mock<ICurrentUserService>();
        var fileValidationMock = new Mock<IFileValidationService>();
        var loggerMock = new Mock<ILogger<ClientAppService>>();

        currentUserMock.Setup(x => x.GetTenantId()).Returns(7);
        currentUserMock.Setup(x => x.GetUserId()).Returns(1);
        localizationMock.Setup(x => x.GetMessage("Application.Service.Client.GetById.ResourceNotFound")).Returns("not-found");
        repoMock.Setup(x => x.GetByIdAsync(12, It.IsAny<CancellationToken>())).ReturnsAsync((ClientEntity?)null);

        var service = new ClientAppService(
            repoMock.Object,
            domainMock.Object,
            mapperMock.Object,
            notifyMock.Object,
            localizationMock.Object,
            currentUserMock.Object,
            fileValidationMock.Object,
            loggerMock.Object);

        var result = await service.GetByIdAsync(12, CancellationToken.None);

        Assert.Null(result);
        notifyMock.Verify(x => x.Add("not-found", 410), Times.Once);
    }

    [Fact(DisplayName = "ClientAppService - CreateAsync chama domain quando request valido")]
    [Trait("Application", "")]
    public async Task CreateAsync_ShouldCallDomainCreate_WhenValidRequestProvided()
    {
        var repoMock = new Mock<IClientRepository>();
        var domainMock = new Mock<IClientDomainService>();
        var mapperMock = new Mock<IMapper>();
        var notifyMock = new Mock<INotify>();
        var localizationMock = new Mock<ILocalizationService>();
        var currentUserMock = new Mock<ICurrentUserService>();
        var fileValidationMock = new Mock<IFileValidationService>();
        var loggerMock = new Mock<ILogger<ClientAppService>>();

        currentUserMock.Setup(x => x.GetTenantId()).Returns(3);
        currentUserMock.Setup(x => x.GetUserId()).Returns(5);

        ClientEntity? capturedEntity = null;
        domainMock.Setup(d => d.CreateAsync(It.IsAny<ClientEntity>(), It.IsAny<CancellationToken>()))
            .Callback<ClientEntity, CancellationToken>((entity, _) =>
            {
                capturedEntity = entity;
                typeof(ClientEntity).BaseType?.GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(entity, 42);
            })
            .ReturnsAsync(true);

        var service = new ClientAppService(
            repoMock.Object,
            domainMock.Object,
            mapperMock.Object,
            notifyMock.Object,
            localizationMock.Object,
            currentUserMock.Object,
            fileValidationMock.Object,
            loggerMock.Object);

        var request = new CreateClientRequest
        {
            PartyTypeId = 1,
            AcquisitionSourceTypeId = 1,
            UrlImage = null,
            Note = null,
            Name = "Client Test",
            PhoneNumber = "999999999",
            CellPhoneNumber = "999999999",
            IsCellPhoneWhatsapp = false,
            Email = "client@gerit.test",
            WebsiteUrl = null,
            BirthDate = DateTime.UtcNow.AddYears(-30),
            Gender = "M",
            Nationality = "PT",
            CompanyRegistrationNumber = null,
            EconomicActivityCode = null,
            NumberOfEmployees = null,
            StatusDefinitionId = null,
            StatusDomainId = null
        };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.True(result > 0);
        domainMock.Verify(x => x.CreateAsync(It.IsAny<ClientEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
