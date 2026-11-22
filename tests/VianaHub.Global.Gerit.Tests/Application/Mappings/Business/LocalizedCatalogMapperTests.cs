using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.AcquisitionSourceType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.AddressType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.DocumentType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.FileType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.PartyType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.StatusDefinition;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.StatusDomain;
using VianaHub.Global.Gerit.Application.Mappings.Business;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Tests.Application.Mappings.Business;

public sealed class LocalizedCatalogMapperTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;

    [Theory]
    [InlineData("pt-PT", "PT", "pt-PT")]
    [InlineData("pt-BR", "BR", "pt-BR")]
    [InlineData("en-US", "EN", "en-US")]
    [InlineData("es-ES", "ES", "es-ES")]
    [InlineData("fr-FR", "PT", "pt-PT")]
    public void Responses_Details_AndPages_PreserveLocalizedValues(string culture, string suffix, string expectedLanguage)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;

        Compare(PartyType(), "Party", suffix, expectedLanguage, LocalizedCatalogMapper.ToPartyTypeResponse, LocalizedCatalogMapper.ToPartyTypeDetail, LocalizedCatalogMapper.ToPartyTypePaged);
        Compare(AddressType(), "Address", suffix, expectedLanguage, LocalizedCatalogMapper.ToAddressTypeResponse, LocalizedCatalogMapper.ToAddressTypeDetail, LocalizedCatalogMapper.ToAddressTypePaged);
        Compare(DocumentType(), "Document", suffix, expectedLanguage, LocalizedCatalogMapper.ToDocumentTypeResponse, LocalizedCatalogMapper.ToDocumentTypeDetail, LocalizedCatalogMapper.ToDocumentTypePaged);
        Compare(FileType(), "File", suffix, expectedLanguage, LocalizedCatalogMapper.ToFileTypeResponse, LocalizedCatalogMapper.ToFileTypeDetail, LocalizedCatalogMapper.ToFileTypePaged);
        Compare(StatusDomain(), "Status", suffix, expectedLanguage, LocalizedCatalogMapper.ToStatusDomainResponse, LocalizedCatalogMapper.ToStatusDomainDetail, LocalizedCatalogMapper.ToStatusDomainPaged);
        Compare(StatusDefinition(), "Definition", suffix, expectedLanguage, LocalizedCatalogMapper.ToStatusDefinitionResponse, LocalizedCatalogMapper.ToStatusDefinitionDetail, LocalizedCatalogMapper.ToStatusDefinitionPaged);
        Compare(AcquisitionSourceType(), "Source", suffix, expectedLanguage, LocalizedCatalogMapper.ToAcquisitionSourceTypeResponse, LocalizedCatalogMapper.ToAcquisitionSourceTypeDetail, LocalizedCatalogMapper.ToAcquisitionSourceTypePaged);
    }

    [Fact]
    public void NullEmptyAndMissingTranslationsKeepExistingFallbackSemantics()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;

        var fallback = new PartyTypeEntity("FALLBACK", 1);
        fallback.Translations.Add(new PartyTypeTranslationsEntity(0, "pt-PT", "Fallback PT", "Descrição PT"));
        AssertPartyResult(fallback, "Fallback PT", "Descrição PT", "pt-PT");

        var nullRequested = new PartyTypeEntity("NULL", 1);
        nullRequested.Translations.Add(new PartyTypeTranslationsEntity(0, "en-US", null, null));
        nullRequested.Translations.Add(new PartyTypeTranslationsEntity(0, "pt-PT", "Fallback PT", "Descrição PT"));
        AssertPartyResult(nullRequested, "Fallback PT", "Descrição PT", "en-US");

        var emptyRequested = new PartyTypeEntity("EMPTY", 1);
        emptyRequested.Translations.Add(new PartyTypeTranslationsEntity(0, "en-US", string.Empty, string.Empty));
        emptyRequested.Translations.Add(new PartyTypeTranslationsEntity(0, "pt-PT", "Fallback PT", "Descrição PT"));
        AssertPartyResult(emptyRequested, string.Empty, string.Empty, "en-US");

        var noTranslations = new PartyTypeEntity("NONE", 1);
        AssertPartyResult(noTranslations, null, null, null);

        var statusWithoutTenant = new StatusDefinitionEntity(8, 2, "ACTIVE", 1, false, 1);
        statusWithoutTenant.Translations.Add(new StatusDefinitionTranslationsEntity(8, 2, 1, "pt-PT", "Ativo", null));
        Assert.Null(LocalizedCatalogMapper.ToStatusDefinitionDetail(statusWithoutTenant, "pt-PT").TenantName);
    }

    private static void Compare<TEntity, TResponse, TDetail>(
        TEntity entity,
        string prefix,
        string suffix,
        string expectedLanguage,
        Func<TEntity, TResponse> mapResponse,
        Func<TEntity, TDetail> mapDetail,
        Func<ListPage<TEntity>, ListPageResponse<TResponse>> mapPage)
    {
        var expectedName = $"{prefix} {suffix}";
        var response = mapResponse(entity);
        Assert.Equal(expectedName, ReadString(response, "Name"));
        Assert.Equal(expectedLanguage, ReadString(response, "LanguageCode"));

        var detail = mapDetail(entity);
        Assert.Equal(expectedName, ReadString(detail, "Name"));
        Assert.Equal(expectedLanguage, ReadString(detail, "LanguageCode"));
        if (HasProperty(detail, "Description"))
            Assert.Equal(suffix == "BR" ? $"Desc PT" : $"Desc {suffix}", ReadString(detail, "Description"));
        if (HasProperty(detail, "TenantName"))
            Assert.Equal("Tenant", ReadString(detail, "TenantName"));

        var page = mapPage(new ListPage<TEntity>([entity], 2, 7, 15, 3));
        var item = Assert.Single(page.Items);
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(7, page.PageSize);
        Assert.Equal(15, page.TotalItems);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(expectedName, ReadString(item, "Name"));

        var emptyPage = mapPage(new ListPage<TEntity>(Array.Empty<TEntity>(), 1, 10, 0, 0));
        Assert.Empty(emptyPage.Items);
        Assert.Equal(1, emptyPage.PageNumber);
        Assert.Equal(10, emptyPage.PageSize);
        Assert.Equal(0, emptyPage.TotalItems);
        Assert.Equal(0, emptyPage.TotalPages);
    }

    private static void AssertPartyResult(PartyTypeEntity entity, string? expectedName, string? expectedDescription, string? expectedLanguage)
    {
        var response = LocalizedCatalogMapper.ToPartyTypeResponse(entity);
        Assert.Equal(expectedName, response.Name);
        Assert.Equal(expectedLanguage, response.LanguageCode);
        var detail = LocalizedCatalogMapper.ToPartyTypeDetail(entity);
        Assert.Equal(expectedName, detail.Name);
        Assert.Equal(expectedDescription, detail.Description);
        Assert.Equal(expectedLanguage, detail.LanguageCode);
    }

    private static string? ReadString(object? value, string name) => value?.GetType().GetProperty(name)?.GetValue(value) as string;
    private static bool HasProperty(object? value, string name) => value?.GetType().GetProperty(name) != null;

    private static PartyTypeEntity PartyType() { var e = new PartyTypeEntity("PARTY", 1); e.Translations.Add(new(0, "pt-PT", "Party PT", "Desc PT")); e.Translations.Add(new(0, "pt-BR", "Party BR", null)); e.Translations.Add(new(0, "en-US", "Party EN", "Desc EN")); e.Translations.Add(new(0, "es-ES", "Party ES", "Desc ES")); return e; }
    private static AddressTypeEntity AddressType() { var e = new AddressTypeEntity("ADDRESS", 1); e.Translations.Add(new(0, "pt-PT", "Address PT", "Desc PT")); e.Translations.Add(new(0, "pt-BR", "Address BR", null)); e.Translations.Add(new(0, "en-US", "Address EN", "Desc EN")); e.Translations.Add(new(0, "es-ES", "Address ES", "Desc ES")); return e; }
    private static DocumentTypeEntity DocumentType() { var e = new DocumentTypeEntity("DOCUMENT", 1); e.Translations.Add(new(0, "pt-PT", "Document PT", "Desc PT")); e.Translations.Add(new(0, "pt-BR", "Document BR", null)); e.Translations.Add(new(0, "en-US", "Document EN", "Desc EN")); e.Translations.Add(new(0, "es-ES", "Document ES", "Desc ES")); return e; }
    private static FileTypeEntity FileType() { var e = new FileTypeEntity("application/pdf", ".pdf", "PDF", 1); e.Translations.Add(new(0, "pt-PT", "File PT", "Desc PT")); e.Translations.Add(new(0, "pt-BR", "File BR", null)); e.Translations.Add(new(0, "en-US", "File EN", "Desc EN")); e.Translations.Add(new(0, "es-ES", "File ES", "Desc ES")); return e; }
    private static StatusDomainEntity StatusDomain() { var e = new StatusDomainEntity("STATUS", 1); e.Translations.Add(new(0, "pt-PT", "Status PT", "Desc PT")); e.Translations.Add(new(0, "pt-BR", "Status BR", null)); e.Translations.Add(new(0, "en-US", "Status EN", "Desc EN")); e.Translations.Add(new(0, "es-ES", "Status ES", "Desc ES")); return e; }
    private static StatusDefinitionEntity StatusDefinition() { var e = new StatusDefinitionEntity(8, 2, "ACTIVE", 1, false, 1); e.Translations.Add(new(8, 2, 1, "pt-PT", "Definition PT", "Desc PT")); e.Translations.Add(new(8, 2, 1, "pt-BR", "Definition BR", null)); e.Translations.Add(new(8, 2, 1, "en-US", "Definition EN", "Desc EN")); e.Translations.Add(new(8, 2, 1, "es-ES", "Definition ES", "Desc ES")); var tenant = new TenantEntity(1, 1, "Tenant", "t@test.local", null, null, null, 1); typeof(TenantEntity).GetProperty("Id")!.SetValue(tenant, 8); typeof(StatusDefinitionEntity).GetProperty("Tenant")!.SetValue(e, tenant); return e; }
    private static AcquisitionSourceTypeEntity AcquisitionSourceType() { var e = new AcquisitionSourceTypeEntity("SOURCE", 1); e.Translations.Add(new(0, "pt-PT", "Source PT", "Desc PT")); e.Translations.Add(new(0, "pt-BR", "Source BR", null)); e.Translations.Add(new(0, "en-US", "Source EN", "Desc EN")); e.Translations.Add(new(0, "es-ES", "Source ES", "Desc ES")); return e; }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        CultureInfo.CurrentUICulture = _originalUiCulture;
    }
}
