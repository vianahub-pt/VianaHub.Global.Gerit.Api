using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Repository.Business;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.DocumentType;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDefinition;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Tests;

public class CatalogCreationTests
{
    private readonly Mock<IDocumentTypeDataRepository> _repo = new();
    private readonly Mock<IDocumentTypeDomainService> _domain = new();
    private readonly Mock<IStatusDefinitionDataRepository> _statusRepo = new();
    private readonly Mock<IStatusDefinitionDomainService> _statusDomain = new();
    private readonly Mock<INotify> _notify = new();
    private readonly Mock<ILocalizationService> _localization = new();

    public CatalogCreationTests()
    {
        _localization.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        _localization.Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object[]>())).Returns((string key, object[] args) => key);
    }

    private DocumentTypeAppService CreateService() => new(_repo.Object, _domain.Object, Mock.Of<IMapper>(), _notify.Object,
        _localization.Object, Mock.Of<ICurrentUserService>(), Mock.Of<ILogger<DocumentTypeAppService>>());

    private StatusDefinitionAppService CreateStatusService() => new(_statusRepo.Object, _statusDomain.Object, Mock.Of<IMapper>(), _notify.Object,
        _localization.Object, Mock.Of<ICurrentUserService>(), Mock.Of<ILogger<StatusDefinitionAppService>>());

    [Theory]
    [InlineData("pt-PT")]
    [InlineData("pt-BR")]
    [InlineData("es-ES")]
    public async Task DocumentCreate_UsesEnglishTranslationRegardlessOfRequestCulture(string culture)
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        var previousUi = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new(culture);
            System.Globalization.CultureInfo.CurrentUICulture = new(culture);
            var request = new CreateDocumentTypeRequest
            {
                Translations = [new() { LanguageCode = "pt-PT", Name = "Cartão de cidadão" }, new() { LanguageCode = "en-US", Name = "National ID" }]
            };
            await CreateService().CreateAsync(request, CancellationToken.None);
            _repo.Verify(x => x.ExistsByCodeAsync("NATIONAL_ID", It.IsAny<CancellationToken>()), Times.Once);
            _domain.Verify(x => x.CreateAsync(It.Is<DocumentTypeEntity>(e => e.Code == "NATIONAL_ID"), It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
            System.Globalization.CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("!!!")]
    public async Task DocumentCreate_RejectsMissingOrUnusableEnglishName(string? name)
    {
        var request = new CreateDocumentTypeRequest
        {
            Translations = [new() { LanguageCode = "pt-PT", Name = "Passaporte" }]
        };
        if (name != null) request.Translations.Add(new() { LanguageCode = "en-US", Name = name });
        Assert.Equal(0, await CreateService().CreateAsync(request, CancellationToken.None));
        _notify.Verify(x => x.Add("Application.Catalog.CanonicalEnglishNameRequired", 400), Times.Once);
        _domain.Verify(x => x.CreateAsync(It.IsAny<DocumentTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DocumentCreate_ReturnsConflictForCanonicalCodeCollision()
    {
        _repo.Setup(x => x.ExistsByCodeAsync("NATIONAL_ID", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var request = new CreateDocumentTypeRequest
        {
            Translations = [new() { LanguageCode = "en-US", Name = "National-ID" }]
        };
        Assert.Equal(0, await CreateService().CreateAsync(request, CancellationToken.None));
        _notify.Verify(x => x.Add("Application.Service.DocumentType.Create.CodeAlreadyExists", 409), Times.Once);
        _domain.Verify(x => x.CreateAsync(It.IsAny<DocumentTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StatusCreate_UsesEnglishAndPreservesDomainCollisionScope()
    {
        var request = new CreateStatusDefinitionRequest
        {
            StatusDomainId = 5,
            Translations = [new() { LanguageCode = "pt-PT", Name = "Em andamento" }, new() { LanguageCode = "en-US", Name = "In progress" }]
        };
        await CreateStatusService().CreateAsync(request, CancellationToken.None);
        _statusRepo.Verify(x => x.ExistsByCodeAndDomainAsync(5, "IN_PROGRESS", It.IsAny<CancellationToken>()), Times.Once);
        _statusDomain.Verify(x => x.CreateAsync(It.Is<StatusDefinitionEntity>(e => e.Code == "IN_PROGRESS" && e.StatusDomainId == 5), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StatusCreate_RejectsPortugueseOnlyAndDuplicateEnglishTranslations()
    {
        var request = new CreateStatusDefinitionRequest
        {
            StatusDomainId = 5,
            Translations = [new() { LanguageCode = "pt-PT", Name = "Em andamento" }]
        };
        Assert.Equal(0, await CreateStatusService().CreateAsync(request, CancellationToken.None));
        request.Translations.Add(new() { LanguageCode = "en-US", Name = "In progress" });
        request.Translations.Add(new() { LanguageCode = "en-US", Name = "Working" });
        Assert.Equal(0, await CreateStatusService().CreateAsync(request, CancellationToken.None));
        _notify.Verify(x => x.Add("Application.Catalog.CanonicalEnglishNameRequired", 400), Times.Exactly(2));
        _statusDomain.Verify(x => x.CreateAsync(It.IsAny<StatusDefinitionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StatusCreate_Returns409ForEnglishCollisionWithinDomain()
    {
        _statusRepo.Setup(x => x.ExistsByCodeAndDomainAsync(5, "IN_PROGRESS", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var request = new CreateStatusDefinitionRequest
        {
            StatusDomainId = 5,
            Translations = [new() { LanguageCode = "en-US", Name = "In progress" }]
        };
        Assert.Equal(0, await CreateStatusService().CreateAsync(request, CancellationToken.None));
        _notify.Verify(x => x.Add("Application.Service.StatusDefinition.Create.CodeAlreadyExists", 409), Times.Once);
        _statusDomain.Verify(x => x.CreateAsync(It.IsAny<StatusDefinitionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnglishTranslationUpdates_DoNotRegenerateEitherCatalogCode()
    {
        var document = new DocumentTypeEntity("NATIONAL_ID", 1);
        document.Translations.Add(new(0, "en-US", "National ID", null));
        _repo.Setup(x => x.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(document);
        _repo.Setup(x => x.UpdateAsync(document, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Assert.True(await CreateService().UpdateTranslationAsync(7, 0, new UpdateDocumentTypeTranslationRequest { Name = "Identity card" }, CancellationToken.None));
        Assert.Equal("NATIONAL_ID", document.Code);
        Assert.Equal("Identity card", document.Translations.Single().Name);

        var status = new StatusDefinitionEntity(1, 5, "IN_PROGRESS", 0, false, 1);
        status.Translations.Add(new(1, 5, 0, "en-US", "In progress", null));
        _statusRepo.Setup(x => x.GetByIdAsync(8, It.IsAny<CancellationToken>())).ReturnsAsync(status);
        _statusRepo.Setup(x => x.UpdateAsync(status, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Assert.True(await CreateStatusService().UpdateTranslationAsync(8, 0, new UpdateStatusDefinitionTranslationRequest { Name = "Working" }, CancellationToken.None));
        Assert.Equal("IN_PROGRESS", status.Code);
        Assert.Equal("Working", status.Translations.Single().Name);
    }
    [Fact]
    public async Task ExistingLegacyCodeWithSameTranslation_Returns409BeforePersisting()
    {
        // Seeds preservam WAITING_CLIENT/OTHER; gerar novos códigos não pode repetir seus nomes.
        _repo.Setup(x => x.ExistsByTranslationAsync("en-US", "Other document", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Assert.Equal(0, await CreateService().CreateAsync(new CreateDocumentTypeRequest
        {
            Translations = [new() { LanguageCode = "en-US", Name = "Other document" }]
        }, CancellationToken.None));
        _repo.Verify(x => x.ExistsByCodeAsync("OTHER_DOCUMENT", It.IsAny<CancellationToken>()), Times.Once);
        _statusRepo.Setup(x => x.ExistsByTranslationAndDomainAsync(5, "en-US", "Waiting for client", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Assert.Equal(0, await CreateStatusService().CreateAsync(new CreateStatusDefinitionRequest
        {
            StatusDomainId = 5,
            Translations = [new() { LanguageCode = "en-US", Name = "Waiting for client" }]
        }, CancellationToken.None));
        _statusRepo.Verify(x => x.ExistsByCodeAndDomainAsync(5, "WAITING_FOR_CLIENT", It.IsAny<CancellationToken>()), Times.Once);
        _notify.Verify(x => x.Add("Application.Catalog.TranslationAlreadyExists", 409), Times.Exactly(2));
        _domain.Verify(x => x.CreateAsync(It.IsAny<DocumentTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _statusDomain.Verify(x => x.CreateAsync(It.IsAny<StatusDefinitionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void CreateValidators_RejectNullTranslationItemsAndPortugueseOnlyRequests()
    {
        var documentValidator = new VianaHub.Global.Gerit.Api.Validators.Business.DocumentType.CreateDocumentTypeRouteValidator(_localization.Object);
        var statusValidator = new VianaHub.Global.Gerit.Api.Validators.Business.StatusDefinition.CreateStatusDefinitionRouteValidator(_localization.Object);
        Assert.False(documentValidator.Validate(new CreateDocumentTypeRequest
        {
            Translations = [new() { LanguageCode = "en-US", Name = "Passport" }, null!]
        }).IsValid);
        Assert.False(statusValidator.Validate(new CreateStatusDefinitionRequest
        {
            StatusDomainId = 5,
            Translations = [new() { LanguageCode = "pt-PT", Name = "Em andamento" }]
        }).IsValid);
    }
    [Fact]
    public void BothCreateValidators_RejectNullItemsAndDuplicateOtherLanguagesWithoutThrowing()
    {
        var documentValidator = new VianaHub.Global.Gerit.Api.Validators.Business.DocumentType.CreateDocumentTypeRouteValidator(_localization.Object);
        var statusValidator = new VianaHub.Global.Gerit.Api.Validators.Business.StatusDefinition.CreateStatusDefinitionRouteValidator(_localization.Object);
        var document = new CreateDocumentTypeRequest
        {
            Translations = [new() { LanguageCode = "en-US", Name = "Passport" }, null!]
        };
        var status = new CreateStatusDefinitionRequest
        {
            StatusDomainId = 5,
            Translations = [new() { LanguageCode = "en-US", Name = "In progress" }, null!]
        };
        Assert.False(documentValidator.Validate(document).IsValid);
        Assert.False(statusValidator.Validate(status).IsValid);
        document.Translations.RemoveAt(1);
        document.Translations.Add(new() { LanguageCode = "pt-PT", Name = "Passaporte" });
        document.Translations.Add(new() { LanguageCode = "pt-PT", Name = "Outro passaporte" });
        status.Translations.RemoveAt(1);
        status.Translations.Add(new() { LanguageCode = "pt-PT", Name = "Em andamento" });
        status.Translations.Add(new() { LanguageCode = "pt-PT", Name = "Em execução" });
        Assert.Contains(documentValidator.Validate(document).Errors, error => error.ErrorMessage == "Application.Catalog.TranslationLanguagesUnique");
        Assert.Contains(statusValidator.Validate(status).Errors, error => error.ErrorMessage == "Application.Catalog.TranslationLanguagesUnique");
    }

    [Fact]
    public async Task TranslationRepositories_ReserveNamesOfSoftDeletedParents()
    {
        await using var context = new GeritDbContext(new DbContextOptionsBuilder<GeritDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var document = new DocumentTypeEntity("OTHER", 1);
        document.Translations.Add(new(0, "en-US", "Other document", null));
        document.Delete(1);
        var status = new StatusDefinitionEntity(1, 5, "WAITING_CLIENT", 0, true, 1);
        status.Translations.Add(new(1, 5, 0, "en-US", "Waiting for client", null));
        status.Delete(1);
        context.AddRange(document, status);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        Assert.True(await new DocumentTypeDataRepository(context).ExistsByTranslationAsync("en-US", "Other document", CancellationToken.None));
        var repo = new StatusDefinitionDataRepository(context);
        Assert.True(await repo.ExistsByTranslationAndDomainAsync(5, "en-US", "Waiting for client", CancellationToken.None));
        Assert.False(await repo.ExistsByTranslationAndDomainAsync(6, "en-US", "Waiting for client", CancellationToken.None));
    }
}
