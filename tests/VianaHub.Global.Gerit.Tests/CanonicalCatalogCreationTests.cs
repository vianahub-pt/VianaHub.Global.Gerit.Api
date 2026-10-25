using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Application.Services.Billing;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.AddressType;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.AcquisitionSourceType;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.PartyType;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDomain;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.Plan;

namespace VianaHub.Global.Gerit.Tests;

public class CanonicalCatalogCreationTests
{
    public static TheoryData<string> Catalogs => new() { "AddressType", "AcquisitionSourceType", "PartyType", "StatusDomain", "Plan" };

    [Theory]
    [MemberData(nameof(Catalogs))]
    public async Task Create_UsesEnglishRegardlessOfPortugueseAndRequestCulture(string catalog)
    {
        var harness = CreateHarness(catalog);
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            await harness.Create(true);
            Assert.Equal("CANONICAL_ENGLISH", harness.Code());
            harness.RenameEnglish!();
            Assert.Equal("CANONICAL_ENGLISH", harness.Code());
            harness.Notify.Verify(n => n.Add(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }
        finally { CultureInfo.CurrentCulture = originalCulture; }
    }

    [Theory]
    [MemberData(nameof(Catalogs))]
    public async Task Create_MissingEnglishReturns400WithoutPersistence(string catalog)
    {
        var harness = CreateHarness(catalog);
        await harness.Create(false);
        Assert.Null(harness.Entity);
        harness.Notify.Verify(n => n.Add("Application.Catalog.CanonicalEnglishNameRequired", 400), Times.Once);
    }

    [Theory]
    [MemberData(nameof(Catalogs))]
    public async Task Create_CodeCollisionReturns409WithoutPersistence(string catalog)
    {
        var harness = CreateHarness(catalog, codeCollision: true);
        await harness.Create(true);
        Assert.Null(harness.Entity);
        harness.Notify.Verify(n => n.Add(It.IsAny<string>(), 409), Times.Once);
    }

    [Theory]
    [MemberData(nameof(Catalogs))]
    public async Task Create_TranslationCollisionWithDifferentCodeReturns409(string catalog)
    {
        var harness = CreateHarness(catalog, translationCollision: true);
        await harness.Create(true);
        Assert.Null(harness.Entity);
        harness.Notify.Verify(n => n.Add("Application.Catalog.TranslationAlreadyExists", 409), Times.Once);
    }

    private sealed class Harness
    {
        public object? Entity { get; set; }
        public Mock<INotify> Notify { get; } = new();
        public Func<bool, Task> Create { get; set; } = null!;
        public Action? RenameEnglish { get; set; }
        public string? Code() => (string?)Entity?.GetType().GetProperty("Code")?.GetValue(Entity);
    }

    private static Harness CreateHarness(string catalog, bool codeCollision = false, bool translationCollision = false)
    {
        var harness = new Harness();
        var localization = new Mock<ILocalizationService>();
        localization.Setup(l => l.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        var currentUser = Mock.Of<ICurrentUserService>();
        switch (catalog)
        {
            case "AddressType":
                {
                    var repo = new Mock<IAddressTypeDataRepository>();
                    repo.Setup(r => r.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(codeCollision);
                    repo.Setup(r => r.ExistsByTranslationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(translationCollision);
                    var domain = new Mock<IAddressTypeDomainService>();
                    domain.Setup(d => d.CreateAsync(It.IsAny<AddressTypeEntity>(), It.IsAny<CancellationToken>()))
                        .Callback<AddressTypeEntity, CancellationToken>((entity, _) =>
                        {
                            harness.Entity = entity;
                            Assert.Equal(2, entity.Translations.Count);
                            harness.RenameEnglish = () => entity.Translations.Single(t => t.LanguageCode == "en-US").Update("New marketing name", "Description");
                        }).ReturnsAsync(true);
                    var service = new AddressTypeAppService(repo.Object, domain.Object, Mock.Of<IMapper>(), harness.Notify.Object, localization.Object, currentUser, Mock.Of<IFileValidationService>(), Mock.Of<ILogger<AddressTypeAppService>>());
                    harness.Create = async includeEnglish =>
                    {
                        var translations = new List<CreateAddressTypeTranslationRequest> { new() { LanguageCode = "pt-PT", Name = "Nome português diferente", Description = "Descrição" } };
                        if (includeEnglish) translations.Add(new() { LanguageCode = "en-US", Name = "Canonical English", Description = "Description" });
                        await service.CreateAsync(new CreateAddressTypeRequest { Translations = translations }, CancellationToken.None);
                    };
                    break;
                }
            case "AcquisitionSourceType":
                {
                    var repo = new Mock<IAcquisitionSourceTypeDataRepository>();
                    repo.Setup(r => r.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(codeCollision);
                    repo.Setup(r => r.ExistsByTranslationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(translationCollision);
                    var domain = new Mock<IAcquisitionSourceTypeDomainService>();
                    domain.Setup(d => d.CreateAsync(It.IsAny<AcquisitionSourceTypeEntity>(), It.IsAny<CancellationToken>()))
                        .Callback<AcquisitionSourceTypeEntity, CancellationToken>((entity, _) =>
                        {
                            harness.Entity = entity;
                            Assert.Equal(2, entity.Translations.Count);
                            harness.RenameEnglish = () => entity.Translations.Single(t => t.LanguageCode == "en-US").Update("New marketing name", "Description");
                        }).ReturnsAsync(true);
                    var service = new AcquisitionSourceTypeAppService(repo.Object, domain.Object, Mock.Of<IMapper>(), harness.Notify.Object, localization.Object, currentUser, Mock.Of<IFileValidationService>(), Mock.Of<ILogger<AcquisitionSourceTypeAppService>>());
                    harness.Create = async includeEnglish =>
                    {
                        var translations = new List<CreateAcquisitionSourceTypeTranslationRequest> { new() { LanguageCode = "pt-PT", Name = "Nome português diferente", Description = "Descrição" } };
                        if (includeEnglish) translations.Add(new() { LanguageCode = "en-US", Name = "Canonical English", Description = "Description" });
                        await service.CreateAsync(new CreateAcquisitionSourceTypeRequest { Translations = translations }, CancellationToken.None);
                    };
                    break;
                }
            case "PartyType":
                {
                    var repo = new Mock<IPartyTypeDataRepository>();
                    repo.Setup(r => r.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(codeCollision);
                    repo.Setup(r => r.ExistsByTranslationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(translationCollision);
                    var domain = new Mock<IPartyTypeDomainService>();
                    domain.Setup(d => d.CreateAsync(It.IsAny<PartyTypeEntity>(), It.IsAny<CancellationToken>()))
                        .Callback<PartyTypeEntity, CancellationToken>((entity, _) =>
                        {
                            harness.Entity = entity;
                            Assert.Equal(2, entity.Translations.Count);
                            harness.RenameEnglish = () => entity.Translations.Single(t => t.LanguageCode == "en-US").Update("New marketing name", "Description");
                        }).ReturnsAsync(true);
                    var service = new PartyTypeAppService(repo.Object, domain.Object, Mock.Of<IMapper>(), harness.Notify.Object, localization.Object, currentUser, Mock.Of<ILogger<PartyTypeAppService>>());
                    harness.Create = async includeEnglish =>
                    {
                        var translations = new List<CreatePartyTypeTranslationRequest> { new() { LanguageCode = "pt-PT", Name = "Nome português diferente", Description = "Descrição" } };
                        if (includeEnglish) translations.Add(new() { LanguageCode = "en-US", Name = "Canonical English", Description = "Description" });
                        await service.CreateAsync(new CreatePartyTypeRequest { Translations = translations }, CancellationToken.None);
                    };
                    break;
                }
            case "StatusDomain":
                {
                    var repo = new Mock<IStatusDomainDataRepository>();
                    repo.Setup(r => r.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(codeCollision);
                    repo.Setup(r => r.ExistsByTranslationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(translationCollision);
                    var domain = new Mock<IStatusDomainDomainService>();
                    domain.Setup(d => d.CreateAsync(It.IsAny<StatusDomainEntity>(), It.IsAny<CancellationToken>()))
                        .Callback<StatusDomainEntity, CancellationToken>((entity, _) =>
                        {
                            harness.Entity = entity;
                            Assert.Equal(2, entity.Translations.Count);
                            harness.RenameEnglish = () => entity.Translations.Single(t => t.LanguageCode == "en-US").Update("New marketing name", "Description");
                        }).ReturnsAsync(true);
                    var service = new StatusDomainAppService(repo.Object, domain.Object, Mock.Of<IMapper>(), harness.Notify.Object, localization.Object, currentUser, Mock.Of<ILogger<StatusDomainAppService>>());
                    harness.Create = async includeEnglish =>
                    {
                        var translations = new List<CreateStatusDomainTranslationRequest> { new() { LanguageCode = "pt-PT", Name = "Nome português diferente", Description = "Descrição" } };
                        if (includeEnglish) translations.Add(new() { LanguageCode = "en-US", Name = "Canonical English", Description = "Description" });
                        await service.CreateAsync(new CreateStatusDomainRequest { Translations = translations }, CancellationToken.None);
                    };
                    break;
                }
            case "Plan":
                {
                    var repo = new Mock<IPlanDataRepository>();
                    repo.Setup(r => r.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(codeCollision);
                    repo.Setup(r => r.ExistsByTranslationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(translationCollision);
                    var domain = new Mock<IPlanDomainService>();
                    domain.Setup(d => d.CreateAsync(It.IsAny<SubscriptionPlanEntity>(), It.IsAny<CancellationToken>()))
                        .Callback<SubscriptionPlanEntity, CancellationToken>((entity, _) =>
                        {
                            harness.Entity = entity;
                            Assert.Equal(2, entity.Translations.Count);
                            harness.RenameEnglish = () => entity.Translations.Single(t => t.LanguageCode == "en-US").Update("New marketing name", "Description");
                        }).ReturnsAsync(true);
                    var service = new PlanAppService(repo.Object, domain.Object, harness.Notify.Object, Mock.Of<IMapper>(), currentUser, localization.Object, Mock.Of<ILogger<PlanAppService>>(), Mock.Of<IFileValidationService>());
                    harness.Create = async includeEnglish =>
                    {
                        var translations = new List<CreatePlanTranslationRequest> { new() { LanguageCode = "pt-PT", Name = "Nome português diferente", Description = "Descrição" } };
                        if (includeEnglish) translations.Add(new() { LanguageCode = "en-US", Name = "Canonical English", Description = "Description" });
                        await service.CreateAsync(new CreatePlanRequest { Translations = translations }, CancellationToken.None);
                    };
                    break;
                }
            default: throw new ArgumentOutOfRangeException(nameof(catalog));
        }
        return harness;
    }
}
