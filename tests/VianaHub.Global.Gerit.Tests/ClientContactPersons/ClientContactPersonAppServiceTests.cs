using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientContact;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Tests.ClientContactPersons;

public class ClientContactPersonAppServiceTests
{
    private readonly Mock<IClientContactPersonDataRepository> _repo = new();
    private readonly Mock<IClientContactPersonDomainService> _domain = new();
    private readonly Mock<INotify> _notify = new();
    private readonly Mock<IFileValidationService> _fileValidation = new();
    private readonly Mock<IClientRepository> _clients = new();
    private readonly ClientContactPersonAppService _service;

    public ClientContactPersonAppServiceTests()
    {
        var localization = new Mock<ILocalizationService>();
        localization.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        localization.Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object?[]>())).Returns((string key, object?[] _) => key);
        var current = new Mock<ICurrentUserService>();
        current.Setup(x => x.GetTenantId()).Returns(7);
        current.Setup(x => x.GetUserId()).Returns(17);
        _service = new ClientContactPersonAppService(
            _repo.Object, _domain.Object, _notify.Object,
            localization.Object, current.Object, _fileValidation.Object,
            Mock.Of<ILogger<ClientContactPersonAppService>>(), _clients.Object);
        _clients.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClientEntity(7, 1, 1, null, null, "Client", null, null, false, null, null, null, null, null, null, null, null, 1, 1, 7));
        _clients.Setup(x => x.GetByIdAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClientEntity(7, 1, 1, null, null, "Other Client", null, null, false, null, null, null, null, null, null, null, null, 1, 1, 7));
    }

    private static CreateClientContactRequest Request(bool primary = false) => new()
    {
        Name = "Contact",
        Email = "contact@example.test",
        CellPhoneNumber = "+351900000000",
        IsCellPhoneWhatsapp = true,
        IsPrimary = primary
    };

    private static ClientContactPersonsEntity Entity(int clientId = 10, bool primary = false)
    {
        var entity = new ClientContactPersonsEntity(7, clientId, "Contact", null, "+351900000000", false,
            "contact@example.test", primary, 17, isCellPhoneWhatsapp: true);
        typeof(ClientContactPersonsEntity).GetProperty("Id")!.SetValue(entity, 42);
        return entity;
    }

    [Fact]
    public async Task Create_DuplicateActiveEmail_ReturnsConflictWithoutPersisting()
    {
        _repo.Setup(x => x.ExistsActiveByClientAndEmailAsync(10, "contact@example.test", null, default)).ReturnsAsync(true);
        Assert.Equal(0, await _service.CreateAsync(10, Request(), default));
        _domain.Verify(x => x.CreateAsync(It.IsAny<ClientContactPersonsEntity>(), default), Times.Never);
        _notify.Verify(x => x.Add("Application.Service.ClientContactPerson.Create.ResourceAlreadyExists", 409), Times.Once);
    }

    [Fact]
    public async Task Create_InactiveParentIsBlockedBeforePersistence()
    {
        var parent = new ClientEntity(7, 1, 1, null, null, "Client", null, null, false, null, null, null, null, null, null, null, null, 1, 1, 7);
        parent.Deactivate(17);
        _clients.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(parent);

        Assert.Equal(0, await _service.CreateAsync(10, Request(), default));
        _domain.Verify(x => x.CreateAsync(It.IsAny<ClientContactPersonsEntity>(), default), Times.Never);
    }

    [Fact]
    public async Task Create_PrimaryConflict_Returns409()
    {
        _repo.Setup(x => x.ExistsActivePrimaryAsync(10, null, default)).ReturnsAsync(true);
        Assert.Equal(0, await _service.CreateAsync(10, Request(true), default));
        _notify.Verify(x => x.Add("Application.Service.ClientContactPerson.PrimaryAlreadyExists", 409), Times.Once);
    }

    [Fact]
    public async Task Create_SameEmailInDifferentClient_IsScopedToRequestedParent()
    {
        _domain.Setup(x => x.CreateAsync(It.IsAny<ClientContactPersonsEntity>(), default))
            .Callback<ClientContactPersonsEntity, CancellationToken>((entity, _) => typeof(ClientContactPersonsEntity).GetProperty("Id")!.SetValue(entity, 8))
            .ReturnsAsync(true);
        Assert.Equal(8, await _service.CreateAsync(11, Request(), default));
        _repo.Verify(x => x.ExistsActiveByClientAndEmailAsync(11, "contact@example.test", null, default), Times.Once);
    }

    [Fact]
    public async Task Update_OwnIdentity_ExcludesCurrentIdAndPersistsThroughDomain()
    {
        var entity = Entity();
        _repo.Setup(x => x.GetByIdAsync(10, 42, default)).ReturnsAsync(entity);
        _domain.Setup(x => x.UpdateAsync(entity, default)).ReturnsAsync(true);
        Assert.True(await _service.UpdateAsync(10, 42, new UpdateClientContactPersonRequest
        {
            Name = "Contact",
            Email = "contact@example.test",
            CellPhoneNumber = "+351900000000",
            IsCellPhoneWhatsapp = true
        }, default));
        _repo.Verify(x => x.ExistsActiveByClientAndEmailAsync(10, "contact@example.test", 42, default), Times.Once);
    }

    [Fact]
    public async Task Update_DuplicateEmail_Returns409WithoutMutationPersistence()
    {
        var entity = Entity();
        _repo.Setup(x => x.GetByIdAsync(10, 42, default)).ReturnsAsync(entity);
        _repo.Setup(x => x.ExistsActiveByClientAndEmailAsync(10, "other@example.test", 42, default)).ReturnsAsync(true);
        Assert.False(await _service.UpdateAsync(10, 42, new UpdateClientContactPersonRequest { Name = "Other", Email = "other@example.test" }, default));
        _domain.Verify(x => x.UpdateAsync(It.IsAny<ClientContactPersonsEntity>(), default), Times.Never);
    }

    [Fact]
    public async Task CrossClientLookupAndMutations_DoNotFetchByIdAlone()
    {
        Assert.Null(await _service.GetByIdAsync(11, 42, default));
        Assert.False(await _service.UpdateAsync(11, 42, new UpdateClientContactPersonRequest(), default));
        Assert.False(await _service.ActivateAsync(11, 42, default));
        Assert.False(await _service.DeactivateAsync(11, 42, default));
        Assert.False(await _service.DeleteAsync(11, 42, default));
        _repo.Verify(x => x.GetByIdAsync(11, 42, default), Times.Exactly(5));
    }

    [Fact]
    public async Task Activate_InactiveDuplicateEmail_IsRejectedBeforePersistence()
    {
        var entity = Entity(); entity.Deactivate(17);
        _repo.Setup(x => x.GetByIdAsync(10, 42, default)).ReturnsAsync(entity);
        _repo.Setup(x => x.ExistsActiveByClientAndEmailAsync(10, entity.Email, 42, default)).ReturnsAsync(true);
        Assert.False(await _service.ActivateAsync(10, 42, default));
        _domain.Verify(x => x.ActivateAsync(It.IsAny<ClientContactPersonsEntity>(), default), Times.Never);
    }

    [Fact]
    public void Deactivate_Primary_RemovesPrimaryToRespectSqlConstraint()
    {
        var entity = Entity(primary: true);

        entity.Deactivate(17);

        Assert.False(entity.IsActive);
        Assert.False(entity.IsPrimary);
    }

    [Fact]
    public void LegacyWhatsappAlias_PersistsThroughCanonicalField()
    {
        var entity = new ClientContactPersonsEntity(7, 10, "Contact", null, "+351900000000", true,
            "contact@example.test", false, 17);
        Assert.True(entity.IsWhatsapp);
        Assert.True(entity.IsCellPhoneWhatsapp);
    }

    [Fact]
    public async Task BulkUpload_ValidRows_AreActuallyPersisted()
    {
        _fileValidation.Setup(x => x.ValidateFile(It.IsAny<IFormFile>())).Returns(true);
        _domain.Setup(x => x.CreateAsync(It.IsAny<ClientContactPersonsEntity>(), default))
            .Callback<ClientContactPersonsEntity, CancellationToken>((entity, _) => typeof(ClientContactPersonsEntity).GetProperty("Id")!.SetValue(entity, 1))
            .ReturnsAsync(true);
        var csv = "Name;Email;Phone;JobTitle;Department;CellPhoneNumber;IsCellPhoneWhatsapp;IsPrimary\nA;a@example.test;;;;;false;false\nB;b@example.test;;;;;false;false\n";
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        IFormFile file = new FormFile(stream, 0, stream.Length, "file", "contacts.csv");

        Assert.True(await _service.BulkUploadAsync(10, file, default));
        _domain.Verify(x => x.CreateAsync(It.IsAny<ClientContactPersonsEntity>(), default), Times.Exactly(2));
    }

    [Fact]
    public async Task BulkUpload_PersistenceFailure_ReturnsFalse()
    {
        _fileValidation.Setup(x => x.ValidateFile(It.IsAny<IFormFile>())).Returns(true);
        _domain.Setup(x => x.CreateAsync(It.IsAny<ClientContactPersonsEntity>(), default)).ReturnsAsync(false);
        var csv = "Name;Email;Phone;JobTitle;Department;CellPhoneNumber;IsCellPhoneWhatsapp;IsPrimary\nA;a@example.test;;;;;false;false\n";
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        IFormFile file = new FormFile(stream, 0, stream.Length, "file", "contacts.csv");
        Assert.False(await _service.BulkUploadAsync(10, file, default));
    }

    [Fact]
    public async Task BulkUpload_InvalidFileOrRow_ReturnsFalseWithoutPersistence()
    {
        await using var invalidStream = new MemoryStream([1]);
        IFormFile invalidFile = new FormFile(invalidStream, 0, 1, "file", "contacts.exe");
        Assert.False(await _service.BulkUploadAsync(10, invalidFile, default));

        _fileValidation.Setup(x => x.ValidateFile(It.IsAny<IFormFile>())).Returns(true);
        var csv = "Name;Email;Phone;JobTitle;Department;CellPhoneNumber;IsCellPhoneWhatsapp;IsPrimary\nA;;;;;;false;false\n";
        await using var rowStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        Assert.False(await _service.BulkUploadAsync(10, new FormFile(rowStream, 0, rowStream.Length, "file", "contacts.csv"), default));
        _domain.Verify(x => x.CreateAsync(It.IsAny<ClientContactPersonsEntity>(), default), Times.Never);
    }

    [Fact]
    public async Task BulkUpload_DuplicateRow_ReturnsFalseAfterFirstRowWasPersisted()
    {
        _fileValidation.Setup(x => x.ValidateFile(It.IsAny<IFormFile>())).Returns(true);
        _repo.SetupSequence(x => x.ExistsActiveByClientAndEmailAsync(10, "a@example.test", null, default))
            .ReturnsAsync(false).ReturnsAsync(true);
        _domain.Setup(x => x.CreateAsync(It.IsAny<ClientContactPersonsEntity>(), default))
            .Callback<ClientContactPersonsEntity, CancellationToken>((entity, _) => typeof(ClientContactPersonsEntity).GetProperty("Id")!.SetValue(entity, 1))
            .ReturnsAsync(true);
        var csv = "Name;Email;Phone;JobTitle;Department;CellPhoneNumber;IsCellPhoneWhatsapp;IsPrimary\nA;a@example.test;;;;;false;false\nB;a@example.test;;;;;false;false\n";
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        Assert.False(await _service.BulkUploadAsync(10, new FormFile(stream, 0, stream.Length, "file", "contacts.csv"), default));
        _domain.Verify(x => x.CreateAsync(It.IsAny<ClientContactPersonsEntity>(), default), Times.Once);
    }

    [Fact]
    public async Task BulkUpload_OverMaximumRows_ReturnsFalse()
    {
        _fileValidation.Setup(x => x.ValidateFile(It.IsAny<IFormFile>())).Returns(true);
        var rows = Enumerable.Range(1, VianaHub.Global.Gerit.Domain.Helpers.DomainExtensions.GetMaxCsvRows() + 1)
            .Select(i => $"Contact {i};contact{i}@example.test;;;;;false;false");
        var csv = "Name;Email;Phone;JobTitle;Department;CellPhoneNumber;IsCellPhoneWhatsapp;IsPrimary\n" + string.Join('\n', rows);
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        Assert.False(await _service.BulkUploadAsync(10, new FormFile(stream, 0, stream.Length, "file", "contacts.csv"), default));
    }
}
