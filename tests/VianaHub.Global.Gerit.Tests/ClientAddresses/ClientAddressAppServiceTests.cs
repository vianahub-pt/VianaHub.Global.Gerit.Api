using AutoMapper;
using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientAddress;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Tests.ClientAddresses;

public class ClientAddressAppServiceTests
{
    private readonly Mock<IClientAddressDataRepository> _repo = new();
    private readonly Mock<IClientRepository> _clients = new();
    private readonly Mock<IAddressTypeDataRepository> _addressTypes = new();
    private readonly Mock<IClientAddressDomainService> _domain = new();
    private readonly Mock<INotify> _notify = new();
    private readonly ClientAddressAppService _service;

    public ClientAddressAppServiceTests()
    {
        var localization = new Mock<ILocalizationService>();
        localization.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        var current = new Mock<ICurrentUserService>();
        current.Setup(x => x.GetTenantId()).Returns(7);
        current.Setup(x => x.GetUserId()).Returns(17);
        _clients.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(Client());
        _addressTypes.Setup(x => x.ExistsByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _service = new ClientAddressAppService(_repo.Object, _clients.Object, _addressTypes.Object,
            _domain.Object, Mock.Of<IMapper>(), _notify.Object, localization.Object, current.Object,
            Mock.Of<IFileValidationService>());
    }

    private static ClientEntity Client() => new(7, 2, 8, null, null, "Client", null, null, false,
        "client@example.test", null, null, null, null, null, null, null, 1, 1, 17);

    private static CreateClientAddressRequest Request() => new()
    {
        AddressTypeId = 2,
        CountryCode = "PT",
        Street = "Street",
        StreetNumber = "12",
        Complement = "Suite",
        Neighborhood = "Neighbourhood",
        City = "City",
        District = "District",
        PostalCode = "1000-000",
        Latitude = 10,
        Longitude = 20,
        Note = "Note",
        IsPrimary = false
    };

    [Fact]
    public async Task Create_MapsEveryFieldAndUsesCanonicalIdentity()
    {
        ClientAddressesEntity? captured = null;
        _domain.Setup(x => x.CreateAsync(It.IsAny<ClientAddressesEntity>(), default))
            .Callback<ClientAddressesEntity, CancellationToken>((entity, _) =>
            {
                captured = entity;
                typeof(ClientAddressesEntity).GetProperty("Id")!.SetValue(entity, 42);
            }).ReturnsAsync(true);

        var result = await _service.CreateAsync(10, Request(), default);

        Assert.Equal(42, result);
        Assert.NotNull(captured);
        Assert.Equal("PT", captured!.CountryCode);
        Assert.Equal("Street", captured.Street);
        Assert.Equal("12", captured.StreetNumber);
        Assert.Equal("Suite", captured.Complement);
        Assert.Equal("Neighbourhood", captured.Neighborhood);
        Assert.Equal("City", captured.City);
        Assert.Equal("District", captured.District);
        Assert.Equal("1000-000", captured.PostalCode);
        Assert.Equal(10, captured.Latitude);
        Assert.Equal(20, captured.Longitude);
        Assert.Equal("Note", captured.Note);
    }

    [Fact]
    public void DeactivatePrimary_RemovesPrimary()
    {
        var entity = new ClientAddressesEntity(7, 10, 2, "PT", "Street", null, null, "N", "City", null, "1000", null, null, null, true, 17);
        entity.Deactivate(17);
        Assert.False(entity.IsActive);
        Assert.False(entity.IsPrimary);
    }
}
