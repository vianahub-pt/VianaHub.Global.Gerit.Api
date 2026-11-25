using Moq;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Validators.Business.ClientDocument;

namespace VianaHub.Global.Gerit.Tests.ClientDocuments;

public class ClientDocumentEntityTests
{
    [Fact]
    public void DeactivatePrimary_ClearsPrimaryAndIsIdempotent()
    {
        var entity = Valid(isPrimary: true);
        entity.Deactivate(17);
        entity.Deactivate(17);
        Assert.False(entity.IsPrimary);
        Assert.False(entity.IsActive);
        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public void DeletePrimary_ClearsPrimaryAndMarksDeleted()
    {
        var entity = Valid(isPrimary: true);
        entity.Delete(17);
        Assert.False(entity.IsPrimary);
        Assert.False(entity.IsActive);
        Assert.True(entity.IsDeleted);
    }

    [Fact]
    public void ActivateDoesNotRestorePrimary()
    {
        var entity = Valid(isPrimary: true);
        entity.Deactivate(17);
        entity.Activate(17);
        Assert.True(entity.IsActive);
        Assert.False(entity.IsPrimary);
    }

    [Fact]
    public async Task ValidatorRequiresCountryAndAllowsEqualDates()
    {
        var validator = new CreateClientDocumentValidator(Localization());
        var missingCountry = Valid(country: string.Empty);
        var equalDates = Valid(country: "pt", issued: new DateOnly(2026, 1, 1), expires: new DateOnly(2026, 1, 1));

        Assert.False((await validator.ValidateAsync(missingCountry)).IsValid);
        Assert.True((await validator.ValidateAsync(equalDates)).IsValid);
    }

    [Fact]
    public async Task ValidatorRejectsExpiryBeforeIssueAndLongNumber()
    {
        var validator = new CreateClientDocumentValidator(Localization());
        var invalidDates = Valid(issued: new DateOnly(2026, 2, 1), expires: new DateOnly(2026, 1, 1));
        var longNumber = Valid();
        typeof(ClientDocumentsEntity).GetProperty(nameof(ClientDocumentsEntity.DocumentNumber))!.SetValue(longNumber, new string('X', 101));

        Assert.False((await validator.ValidateAsync(invalidDates)).IsValid);
        Assert.False((await validator.ValidateAsync(longNumber)).IsValid);
    }

    [Fact]
    public void CreateAndUpdateHonorOptionalIsActiveWithoutBreakingPrimaryInvariant()
    {
        var defaultActive = Valid(isPrimary: true);
        var explicitlyInactive = new ClientDocumentsEntity(7, 10, 1, "DOC-2", "PT", null, null, true, 17, false);
        var explicitlyActive = new ClientDocumentsEntity(7, 10, 1, "DOC-3", "PT", null, null, false, 17, true);

        Assert.True(defaultActive.IsActive);
        Assert.True(defaultActive.IsPrimary);
        Assert.False(explicitlyInactive.IsActive);
        Assert.False(explicitlyInactive.IsPrimary);
        Assert.True(explicitlyActive.IsActive);

        explicitlyInactive.Update("DOC-2", "PT", null, null, true, 18);
        Assert.False(explicitlyInactive.IsActive);
        Assert.False(explicitlyInactive.IsPrimary);

        explicitlyInactive.Update("DOC-2", "PT", null, null, false, 18, true);
        Assert.True(explicitlyInactive.IsActive);
        Assert.False(explicitlyInactive.IsPrimary);

        explicitlyInactive.Update("DOC-2", "PT", null, null, true, 18, false);
        Assert.False(explicitlyInactive.IsActive);
        Assert.False(explicitlyInactive.IsPrimary);
    }
    private static ClientDocumentsEntity Valid(bool isPrimary = false, string country = "PT", DateOnly? issued = null, DateOnly? expires = null) =>
        new(7, 10, 1, "DOC-1", country, issued, expires, isPrimary, 17);

    private static ILocalizationService Localization()
    {
        var mock = new Mock<ILocalizationService>();
        mock.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        mock.Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object[]>())).Returns((string key, object[] _) => key);
        return mock.Object;
    }
}
