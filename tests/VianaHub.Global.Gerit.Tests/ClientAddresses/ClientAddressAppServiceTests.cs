using Moq;
using Microsoft.AspNetCore.Http;
using System.Text;
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
    private readonly Mock<IFileValidationService> _fileValidation = new();
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
            _domain.Object, _notify.Object, localization.Object, current.Object,
            _fileValidation.Object);
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
    public async Task Update_UsesUpdateDomainMethodAndPropagatesResult()
    {
        var entity = new ClientAddressesEntity(7, 10, 2, "PT", "Street", "12", null, "N", "City", "District", "1000", null, null, null, false, 17);
        typeof(ClientAddressesEntity).GetProperty("Id")!.SetValue(entity, 42);
        _repo.Setup(x => x.GetByIdAsync(10, 42, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _repo.Setup(x => x.ExistsByIdentityAsync(10, 42, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _domain.Setup(x => x.UpdateAsync(It.IsAny<ClientAddressesEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _service.UpdateAsync(10, 42, new UpdateClientAddressRequest
        {
            AddressTypeId = 2,
            CountryCode = "PT",
            Street = "Updated Street",
            StreetNumber = "13",
            Neighborhood = "N",
            City = "City",
            District = "District",
            PostalCode = "1000"
        }, default);

        Assert.False(result);
        _domain.Verify(x => x.UpdateAsync(It.IsAny<ClientAddressesEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _domain.Verify(x => x.ActivateAsync(It.IsAny<ClientAddressesEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void DeactivatePrimary_RemovesPrimary()
    {
        var entity = new ClientAddressesEntity(7, 10, 2, "PT", "Street", null, null, "N", "City", null, "1000", null, null, null, true, 17);
        entity.Deactivate(17);
        Assert.False(entity.IsActive);
        Assert.False(entity.IsPrimary);
    }

    [Fact]
    public async Task Activate_UsesActivateDomainMethod()
    {
        var entity = new ClientAddressesEntity(7, 10, 2, "PT", "Street", null, null, "N", "City", null, "1000", null, null, null, false, 17);
        typeof(ClientAddressesEntity).GetProperty("Id")!.SetValue(entity, 42);
        _repo.Setup(x => x.GetByIdAsync(10, 42, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _domain.Setup(x => x.ActivateAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.ActivateAsync(10, 42, default);

        Assert.True(result);
        _domain.Verify(x => x.ActivateAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        _domain.Verify(x => x.UpdateAsync(It.IsAny<ClientAddressesEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_NormalizesCountryCodeBeforePersistence()
    {
        _domain.Setup(x => x.CreateAsync(It.IsAny<ClientAddressesEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var request = Request(); request.CountryCode = " pt ";

        await _service.CreateAsync(10, request, default);

        _domain.Verify(x => x.CreateAsync(It.Is<ClientAddressesEntity>(a => a.CountryCode == "PT"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithInactiveParentDoesNotPersist()
    {
        _clients.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync((ClientEntity?)null);

        var result = await _service.CreateAsync(10, Request(), default);

        Assert.Equal(0, result);
        _domain.Verify(x => x.CreateAsync(It.IsAny<ClientAddressesEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_PrimaryWithExistingPrimaryReturnsConflictWithoutPersisting()
    {
        _repo.Setup(x => x.ExistsPrimaryAsync(10, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var request = Request(); request.IsPrimary = true;

        var result = await _service.CreateAsync(10, request, default);

        Assert.Equal(0, result);
        _domain.Verify(x => x.CreateAsync(It.IsAny<ClientAddressesEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _notify.Verify(x => x.Add(It.IsAny<string>(), 409), Times.Once);
    }

    [Fact]
    public async Task Bulk_InvalidSecondRowDoesNotPersistFirstRow()
    {
        _fileValidationSetup();
        var csv = "AddressTypeId;CountryCode;Street;City;PostalCode;IsPrimary\n2;pt;Rua 1;Lisboa;1000;false\n2;;Rua 2;Lisboa;1001;false\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var file = new FormFile(stream, 0, stream.Length, "file", "addresses.csv") { Headers = new HeaderDictionary(), ContentType = "text/csv" };

        var result = await _service.BulkUploadAsync(10, file, default);

        Assert.False(result);
        _domain.Verify(x => x.CreateManyAsync(It.IsAny<IReadOnlyCollection<ClientAddressesEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void _fileValidationSetup() =>
        _fileValidation.Setup(x => x.ValidateFile(It.IsAny<IFormFile?>())).Returns(true);
}
