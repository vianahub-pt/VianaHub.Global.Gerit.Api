using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Application.Services.Billing;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Tests;

public class CanonicalCatalogBulkTests
{
    [Theory]
    [InlineData("AddressType")]
    [InlineData("Plan")]
    public async Task Bulk_MissingEnglishDoesNotFallBackToPortuguese(string catalog)
    {
        var harness = CreateHarness(catalog);
        Assert.False(await harness.Upload(false));
        Assert.Null(harness.Entity);
        harness.Notify.Verify(n => n.Add("Application.Catalog.CanonicalEnglishNameRequired", 400), Times.AtLeastOnce);
    }

    [Theory]
    [InlineData("AddressType")]
    [InlineData("Plan")]
    public async Task Bulk_UsesEnglishNameAndPersistsBothTranslations(string catalog)
    {
        var harness = CreateHarness(catalog);
        Assert.True(await harness.Upload(true));
        Assert.NotNull(harness.Entity);
        Assert.Equal("CANONICAL_ENGLISH", harness.Entity.GetType().GetProperty("Code")!.GetValue(harness.Entity));
    }

    [Theory]
    [InlineData("AddressType")]
    [InlineData("Plan")]
    public async Task Bulk_TranslationCollisionReturns409(string catalog)
    {
        var harness = CreateHarness(catalog, translationCollision: true);
        Assert.False(await harness.Upload(true));
        Assert.Null(harness.Entity);
        harness.Notify.Verify(n => n.Add("Application.Catalog.TranslationAlreadyExists", 409), Times.Once);
    }

    private sealed class Harness
    {
        public object? Entity { get; set; }
        public Mock<INotify> Notify { get; } = new();
        public Func<bool, Task<bool>> Upload { get; set; } = null!;
    }

    private static Harness CreateHarness(string catalog, bool translationCollision = false)
    {
        var harness = new Harness();
        var localization = new Mock<ILocalizationService>();
        localization.Setup(l => l.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        var fileValidation = new Mock<IFileValidationService>();
        fileValidation.Setup(f => f.ValidateFile(It.IsAny<IFormFile>())).Returns(true);
        Func<IFormFile, Task<bool>> upload;
        if (catalog == "AddressType")
        {
            var repo = new Mock<IAddressTypeDataRepository>();
            repo.Setup(r => r.ExistsByTranslationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(translationCollision);
            var domain = new Mock<IAddressTypeDomainService>();
            domain.Setup(d => d.CreateAsync(It.IsAny<AddressTypeEntity>(), It.IsAny<CancellationToken>()))
                .Callback<AddressTypeEntity, CancellationToken>((entity, _) =>
                {
                    harness.Entity = entity;
                    Assert.Equal(2, entity.Translations.Count);
                    Assert.Equal("Canonical English", entity.Translations.Single(t => t.LanguageCode == "en-US").Name);
                }).ReturnsAsync(true);
            var service = new AddressTypeAppService(repo.Object, domain.Object, Mock.Of<IMapper>(), harness.Notify.Object, localization.Object,
                Mock.Of<ICurrentUserService>(), fileValidation.Object, Mock.Of<ILogger<AddressTypeAppService>>());
            upload = file => service.BulkUploadAsync(file, CancellationToken.None);
        }
        else
        {
            var repo = new Mock<IPlanDataRepository>();
            repo.Setup(r => r.ExistsByTranslationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(translationCollision);
            var domain = new Mock<IPlanDomainService>();
            domain.Setup(d => d.CreateAsync(It.IsAny<SubscriptionPlanEntity>(), It.IsAny<CancellationToken>()))
                .Callback<SubscriptionPlanEntity, CancellationToken>((entity, _) =>
                {
                    harness.Entity = entity;
                    Assert.Equal(2, entity.Translations.Count);
                    Assert.Equal("Canonical English", entity.Translations.Single(t => t.LanguageCode == "en-US").Name);
                }).ReturnsAsync(true);
            var service = new PlanAppService(repo.Object, domain.Object, harness.Notify.Object, Mock.Of<IMapper>(), Mock.Of<ICurrentUserService>(), localization.Object,
                Mock.Of<ILogger<PlanAppService>>(), fileValidation.Object);
            upload = file => service.BulkUploadAsync(file, CancellationToken.None);
        }
        harness.Upload = async includeEnglish =>
        {
            var englishColumns = includeEnglish ? ";EnglishName;EnglishDescription" : "";
            var englishValues = includeEnglish ? ";Canonical English;Description" : "";
            var csv = "Name;Description;Currency;MaxUsers;MaxPhotosPerVisit" + englishColumns + "\nNome diferente;Descricao;EUR;1;1" + englishValues + "\n";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
            var file = new FormFile(stream, 0, stream.Length, "file", "catalog.csv");
            return await upload(file);
        };
        return harness;
    }
}
