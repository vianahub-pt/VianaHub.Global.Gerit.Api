using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Helpers;

namespace VianaHub.Global.Gerit.Tests;

public class CatalogCodeGeneratorTests
{
    [Theory]
    [InlineData(" Morada residencial ", "MORADA_RESIDENCIAL")]
    [InlineData("Ação", "ACAO")]
    [InlineData("Acao", "ACAO")]
    [InlineData("Pessoa singular", "PESSOA_SINGULAR")]
    [InlineData("Em andamento", "EM_ANDAMENTO")]
    [InlineData("Standard", "STANDARD")]
    [InlineData("__Olá / mundo---!", "OLA_MUNDO")]
    [InlineData("!!!", "")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Generate_NormalizesLegacySourceWithoutClaimingEnglishSemantics(string? source, string expected)
    {
        Assert.Equal(expected, CatalogCodeGenerator.Generate(source));
        Assert.Equal(expected, CatalogCodeGenerator.Generate(source));
    }

    [Fact]
    public void Generate_RespectsColumnLengthWithoutTrailingSeparator()
    {
        Assert.Equal(new string('A', 50), CatalogCodeGenerator.Generate(new string('a', 90)));
        Assert.Equal("AAAA", CatalogCodeGenerator.Generate("aaaa bbbb", 5));
    }

    [Theory]
    [InlineData(".pdf", "PDF")]
    [InlineData(" .jpg ", "JPEG")]
    [InlineData(" .jpeg ", "JPEG")]
    [InlineData("png", "PNG")]
    [InlineData("gif", "GIF")]
    [InlineData("webp", "WEBP")]
    [InlineData("svg", "SVG")]
    [InlineData("doc", "DOC")]
    [InlineData("docx", "DOCX")]
    [InlineData("xls", "XLS")]
    [InlineData("xlsx", "XLSX")]
    [InlineData("ppt", "PPT")]
    [InlineData("pptx", "PPTX")]
    [InlineData("txt", "TXT")]
    [InlineData("csv", "CSV")]
    [InlineData("json", "JSON")]
    [InlineData("xml", "XML")]
    [InlineData("zip", "ZIP")]
    [InlineData("rar", "RAR")]
    [InlineData("7z", "SEVEN_ZIP")]
    [InlineData("bin", "BINARY")]
    [InlineData(".tar.gz", "TAR_GZ")]
    public void FileCode_UsesTechnicalExtension(string extension, string expected)
    {
        var previousCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "pt-PT", "en-US", "tr-TR" })
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(culture);
                var code = CatalogCodeGenerator.ForFileExtension(extension);
                Assert.Equal(expected, code);
                Assert.Matches("^[A-Z0-9]+(?:_[A-Z0-9]+)*$", code);
                Assert.All(code, character => Assert.InRange((int)character, 0, 127));
            }
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void CanonicalTranslation_RequiresExactlyOneEnglishNameWithoutTranslating()
    {
        var translations = new[] { (Language: "en-US", Name: "English first"), (Language: "pt-PT", Name: "Nome canónico") };
        Assert.Equal("ENGLISH_FIRST", CatalogCodeGenerator.FromCanonicalTranslation(translations, t => t.Language, t => t.Name));
        Assert.Empty(CatalogCodeGenerator.FromCanonicalTranslation(translations.Skip(1), t => t.Language, t => t.Name));
        Assert.Empty(CatalogCodeGenerator.FromCanonicalTranslation(translations.Concat(translations), t => t.Language, t => t.Name));
    }

    [Fact]
    public void CanonicalTranslation_IsIndependentOfCultureAndNullItems()
    {
        var translations = new CanonicalTranslation?[]
        {
            new("pt-PT", "Pessoa singular"),
            new("en-US", "Individual")
        };
        var previousCulture = System.Globalization.CultureInfo.CurrentCulture;
        var previousUiCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            foreach (var culture in new[] { "pt-PT", "pt-BR", "en-US", "es-ES", "tr-TR" })
            {
                System.Globalization.CultureInfo.CurrentCulture = new(culture);
                System.Globalization.CultureInfo.CurrentUICulture = new(culture);
                Assert.Equal("INDIVIDUAL", CatalogCodeGenerator.FromCanonicalTranslation(translations, t => t!.Language, t => t!.Name));
            }
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previousCulture;
            System.Globalization.CultureInfo.CurrentUICulture = previousUiCulture;
        }
        Assert.Empty(CatalogCodeGenerator.FromCanonicalTranslation(new[] { new CanonicalTranslation("en-US", "!!!") }, t => t.Language, t => t.Name));
        Assert.Empty(CatalogCodeGenerator.FromCanonicalTranslation<CanonicalTranslation>(null, t => t.Language, t => t.Name));
        Assert.Empty(CatalogCodeGenerator.FromCanonicalTranslation(translations.Append(null), t => t!.Language, t => t!.Name));
    }

    private sealed record CanonicalTranslation(string Language, string Name);

    [Fact]
    public void TechnicalCodeGeneration_DoesNotNormalizeExternalStandardCodes()
    {
        var translation = new FileTypeTranslationsEntity(0, "pt-PT", "Imagem JPEG", null);
        var plan = new SubscriptionPlanEntity("STANDARD", null, null, 10, null, "EUR", 1, 1, 1);
        Assert.Equal("JPEG", CatalogCodeGenerator.ForFileExtension("jpg"));
        Assert.Equal("pt-PT", translation.LanguageCode);
        Assert.Equal("EUR", plan.Currency);
    }

    [Fact]
    public void DomainUpdates_PreserveExistingCodes()
    {
        var address = new AddressTypeEntity("RESIDENTIAL", 1);
        address.MarkModified(2);
        Assert.Equal("RESIDENTIAL", address.Code);
        var document = new DocumentTypeEntity("PASSPORT", 1);
        document.MarkModified(2);
        Assert.Equal("PASSPORT", document.Code);
        var acquisition = new AcquisitionSourceTypeEntity("INSTAGRAM", 1);
        acquisition.MarkModified(2);
        Assert.Equal("INSTAGRAM", acquisition.Code);
        var party = new PartyTypeEntity("PERSON", 1);
        party.MarkModified(2);
        Assert.Equal("PERSON", party.Code);
        var domain = new StatusDomainEntity("VISIT", 1);
        domain.MarkModified(2);
        Assert.Equal("VISIT", domain.Code);
        var status = new StatusDefinitionEntity(1, 1, "PENDING", 0, false, 1);
        status.Update(5, true, 2);
        Assert.Equal("PENDING", status.Code);
        var file = new FileTypeEntity("application/pdf", ".pdf", "PDF", 1);
        file.Update("application/x-pdf", ".xpdf", 2);
        Assert.Equal("PDF", file.Code);
        var plan = new SubscriptionPlanEntity("STANDARD", null, null, 10, null, "EUR", 1, 1, 1);
        plan.Update(null, null, 20, null, "EUR", 2, 2, 2);
        Assert.Equal("STANDARD", plan.Code);
    }

    [Fact]
    public void CatalogInputContracts_DoNotAcceptCode()
    {
        var types = typeof(global::VianaHub.Global.Gerit.Application.Dtos.Request.Business.AddressType.CreateAddressTypeRequest).Assembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("VianaHub.Global.Gerit.Application.Dtos.Request.") == true)
            .Where(t => new[] { "AddressType", "AcquisitionSourceType", "DocumentType", "PartyType", "StatusDomain", "StatusDefinition", "FileType", "Plan" }
                .Any(name => t.Namespace!.EndsWith("." + name)));
        Assert.NotEmpty(types);
        foreach (var type in types) Assert.Null(type.GetProperty("Code"));
    }
}
