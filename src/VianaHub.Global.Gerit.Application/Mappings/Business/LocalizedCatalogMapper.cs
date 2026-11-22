using System.Globalization;
using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.AcquisitionSourceType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.AddressType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.DocumentType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.FileType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.PartyType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.StatusDefinition;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.StatusDomain;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class LocalizedCatalogMapper
{
    public static PartyTypeResponse ToPartyTypeResponse(PartyTypeEntity source) => ToPartyTypeResponse(source, CultureInfo.CurrentCulture.Name);
    public static PartyTypeResponse ToPartyTypeResponse(PartyTypeEntity source, string culture) { var dto = MapPartyTypeResponse(source); dto.Name = Name(source.Translations, culture, t => t.LanguageCode, t => t.Name); dto.LanguageCode = Language(source.Translations, culture, t => t.LanguageCode); return dto; }
    public static PartyTypeDetailResponse ToPartyTypeDetail(PartyTypeEntity source) => ToPartyTypeDetail(source, CultureInfo.CurrentCulture.Name);
    public static PartyTypeDetailResponse ToPartyTypeDetail(PartyTypeEntity source, string culture) { var dto = MapPartyTypeDetail(source); dto.Name = Name(source.Translations, culture, t => t.LanguageCode, t => t.Name); dto.Description = Description(source.Translations, culture, t => t.LanguageCode, t => t.Description); dto.LanguageCode = Language(source.Translations, culture, t => t.LanguageCode); return dto; }
    public static IEnumerable<PartyTypeResponse> ToPartyTypeResponses(IEnumerable<PartyTypeEntity> source) => source.Select(ToPartyTypeResponse);
    public static ListPageResponse<PartyTypeResponse> ToPartyTypePaged(ListPage<PartyTypeEntity> source) => Page(source, ToPartyTypeResponse);

    public static AddressTypeResponse ToAddressTypeResponse(AddressTypeEntity source) => ToAddressTypeResponse(source, CultureInfo.CurrentCulture.Name);
    public static AddressTypeResponse ToAddressTypeResponse(AddressTypeEntity source, string culture) { var dto = MapAddressTypeResponse(source); dto.Name = Name(source.Translations, culture, t => t.LanguageCode, t => t.Name); dto.LanguageCode = Language(source.Translations, culture, t => t.LanguageCode); return dto; }
    public static AddressTypeDetailResponse ToAddressTypeDetail(AddressTypeEntity source) => ToAddressTypeDetail(source, CultureInfo.CurrentCulture.Name);
    public static AddressTypeDetailResponse ToAddressTypeDetail(AddressTypeEntity source, string culture) { var dto = MapAddressTypeDetail(source); dto.Name = Name(source.Translations, culture, t => t.LanguageCode, t => t.Name); dto.Description = Description(source.Translations, culture, t => t.LanguageCode, t => t.Description); dto.LanguageCode = Language(source.Translations, culture, t => t.LanguageCode); return dto; }
    public static IEnumerable<AddressTypeResponse> ToAddressTypeResponses(IEnumerable<AddressTypeEntity> source) => source.Select(ToAddressTypeResponse);
    public static ListPageResponse<AddressTypeResponse> ToAddressTypePaged(ListPage<AddressTypeEntity> source) => Page(source, ToAddressTypeResponse);

    public static DocumentTypeResponse ToDocumentTypeResponse(DocumentTypeEntity source) => ToDocumentTypeResponse(source, CultureInfo.CurrentCulture.Name);
    public static DocumentTypeResponse ToDocumentTypeResponse(DocumentTypeEntity source, string culture) { var dto = MapDocumentTypeResponse(source); dto.Name = Name(source.Translations, culture, t => t.LanguageCode, t => t.Name); dto.LanguageCode = Language(source.Translations, culture, t => t.LanguageCode); return dto; }
    public static DocumentTypeDetailResponse ToDocumentTypeDetail(DocumentTypeEntity source) => ToDocumentTypeDetail(source, CultureInfo.CurrentCulture.Name);
    public static DocumentTypeDetailResponse ToDocumentTypeDetail(DocumentTypeEntity source, string culture) { var dto = MapDocumentTypeDetail(source); dto.Name = Name(source.Translations, culture, t => t.LanguageCode, t => t.Name); dto.Description = Description(source.Translations, culture, t => t.LanguageCode, t => t.Description); dto.LanguageCode = Language(source.Translations, culture, t => t.LanguageCode); return dto; }
    public static IEnumerable<DocumentTypeResponse> ToDocumentTypeResponses(IEnumerable<DocumentTypeEntity> source) => source.Select(ToDocumentTypeResponse);
    public static ListPageResponse<DocumentTypeResponse> ToDocumentTypePaged(ListPage<DocumentTypeEntity> source) => Page(source, ToDocumentTypeResponse);

    public static FileTypeResponse ToFileTypeResponse(FileTypeEntity source) => ToFileTypeResponse(source, CultureInfo.CurrentCulture.Name);
    public static FileTypeResponse ToFileTypeResponse(FileTypeEntity source, string culture) { var dto = MapFileTypeResponse(source); dto.Name = Name(source.Translations, culture, t => t.LanguageCode, t => t.Name); dto.LanguageCode = Language(source.Translations, culture, t => t.LanguageCode); return dto; }
    public static FileTypeDetailResponse ToFileTypeDetail(FileTypeEntity source) => ToFileTypeDetail(source, CultureInfo.CurrentCulture.Name);
    public static FileTypeDetailResponse ToFileTypeDetail(FileTypeEntity source, string culture) { var dto = MapFileTypeDetail(source); dto.Name = Name(source.Translations, culture, t => t.LanguageCode, t => t.Name); dto.Description = Description(source.Translations, culture, t => t.LanguageCode, t => t.Description); dto.LanguageCode = Language(source.Translations, culture, t => t.LanguageCode); return dto; }
    public static IEnumerable<FileTypeResponse> ToFileTypeResponses(IEnumerable<FileTypeEntity> source) => source.Select(ToFileTypeResponse);
    public static ListPageResponse<FileTypeResponse> ToFileTypePaged(ListPage<FileTypeEntity> source) => Page(source, ToFileTypeResponse);

    public static StatusDomainResponse ToStatusDomainResponse(StatusDomainEntity source) => ToStatusDomainResponse(source, CultureInfo.CurrentCulture.Name);
    public static StatusDomainResponse ToStatusDomainResponse(StatusDomainEntity source, string culture) { var dto = MapStatusDomainResponse(source); dto.Name = Name(source.Translations, culture, t => t.LanguageCode, t => t.Name); dto.LanguageCode = Language(source.Translations, culture, t => t.LanguageCode); return dto; }
    public static StatusDomainDetailResponse ToStatusDomainDetail(StatusDomainEntity source) => ToStatusDomainDetail(source, CultureInfo.CurrentCulture.Name);
    public static StatusDomainDetailResponse ToStatusDomainDetail(StatusDomainEntity source, string culture) { var dto = MapStatusDomainDetail(source); dto.Name = Name(source.Translations, culture, t => t.LanguageCode, t => t.Name); dto.Description = Description(source.Translations, culture, t => t.LanguageCode, t => t.Description); dto.LanguageCode = Language(source.Translations, culture, t => t.LanguageCode); return dto; }
    public static IEnumerable<StatusDomainResponse> ToStatusDomainResponses(IEnumerable<StatusDomainEntity> source) => source.Select(ToStatusDomainResponse);
    public static ListPageResponse<StatusDomainResponse> ToStatusDomainPaged(ListPage<StatusDomainEntity> source) => Page(source, ToStatusDomainResponse);

    public static StatusDefinitionResponse ToStatusDefinitionResponse(StatusDefinitionEntity source) => ToStatusDefinitionResponse(source, CultureInfo.CurrentCulture.Name);
    public static StatusDefinitionResponse ToStatusDefinitionResponse(StatusDefinitionEntity source, string culture) { var dto = MapStatusDefinitionResponse(source); dto.Name = Name(source.Translations, culture, t => t.LanguageCode, t => t.Name); dto.LanguageCode = Language(source.Translations, culture, t => t.LanguageCode); return dto; }
    public static StatusDefinitionDetailResponse ToStatusDefinitionDetail(StatusDefinitionEntity source) => ToStatusDefinitionDetail(source, CultureInfo.CurrentCulture.Name);
    public static StatusDefinitionDetailResponse ToStatusDefinitionDetail(StatusDefinitionEntity source, string culture) { var dto = MapStatusDefinitionDetail(source); dto.Name = Name(source.Translations, culture, t => t.LanguageCode, t => t.Name); dto.LanguageCode = Language(source.Translations, culture, t => t.LanguageCode); dto.TenantName = source.Tenant?.Name; return dto; }
    public static IEnumerable<StatusDefinitionResponse> ToStatusDefinitionResponses(IEnumerable<StatusDefinitionEntity> source) => source.Select(ToStatusDefinitionResponse);
    public static ListPageResponse<StatusDefinitionResponse> ToStatusDefinitionPaged(ListPage<StatusDefinitionEntity> source) => Page(source, ToStatusDefinitionResponse);

    public static AcquisitionSourceTypeResponse ToAcquisitionSourceTypeResponse(AcquisitionSourceTypeEntity source) => ToAcquisitionSourceTypeResponse(source, CultureInfo.CurrentCulture.Name);
    public static AcquisitionSourceTypeResponse ToAcquisitionSourceTypeResponse(AcquisitionSourceTypeEntity source, string culture) { var dto = MapAcquisitionSourceTypeResponse(source); dto.Name = Name(source.Translations, culture, t => t.LanguageCode, t => t.Name); dto.LanguageCode = Language(source.Translations, culture, t => t.LanguageCode); return dto; }
    public static AcquisitionSourceTypeDetailResponse ToAcquisitionSourceTypeDetail(AcquisitionSourceTypeEntity source) => ToAcquisitionSourceTypeDetail(source, CultureInfo.CurrentCulture.Name);
    public static AcquisitionSourceTypeDetailResponse ToAcquisitionSourceTypeDetail(AcquisitionSourceTypeEntity source, string culture) { var dto = MapAcquisitionSourceTypeDetail(source); dto.Name = Name(source.Translations, culture, t => t.LanguageCode, t => t.Name); dto.Description = Description(source.Translations, culture, t => t.LanguageCode, t => t.Description); dto.LanguageCode = Language(source.Translations, culture, t => t.LanguageCode); return dto; }
    public static IEnumerable<AcquisitionSourceTypeResponse> ToAcquisitionSourceTypeResponses(IEnumerable<AcquisitionSourceTypeEntity> source) => source.Select(ToAcquisitionSourceTypeResponse);
    public static ListPageResponse<AcquisitionSourceTypeResponse> ToAcquisitionSourceTypePaged(ListPage<AcquisitionSourceTypeEntity> source) => Page(source, ToAcquisitionSourceTypeResponse);

    private static string? Name<T>(IEnumerable<T>? translations, string culture, Func<T, string?> languageSelector, Func<T, string?> nameSelector) => TranslationResolver.Resolve(translations, culture, languageSelector, nameSelector);
    private static string? Description<T>(IEnumerable<T>? translations, string culture, Func<T, string?> languageSelector, Func<T, string?> descriptionSelector) => TranslationResolver.ResolveDescription(translations, culture, languageSelector, descriptionSelector);
    private static string? Language<T>(IEnumerable<T>? translations, string culture, Func<T, string?> languageSelector) => TranslationResolver.ResolveUsedLanguageCode(translations, culture, languageSelector);
    private static ListPageResponse<TDto> Page<TEntity, TDto>(ListPage<TEntity> source, Func<TEntity, TDto> map) => new(source.Items.Select(map).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(PartyTypeResponse.Name))][MapperIgnoreTarget(nameof(PartyTypeResponse.LanguageCode))] private static partial PartyTypeResponse MapPartyTypeResponse(PartyTypeEntity source);
    [MapperIgnoreTarget(nameof(PartyTypeDetailResponse.Name))][MapperIgnoreTarget(nameof(PartyTypeDetailResponse.Description))][MapperIgnoreTarget(nameof(PartyTypeDetailResponse.LanguageCode))] private static partial PartyTypeDetailResponse MapPartyTypeDetail(PartyTypeEntity source);
    [MapperIgnoreTarget(nameof(AddressTypeResponse.Name))][MapperIgnoreTarget(nameof(AddressTypeResponse.LanguageCode))] private static partial AddressTypeResponse MapAddressTypeResponse(AddressTypeEntity source);
    [MapperIgnoreTarget(nameof(AddressTypeDetailResponse.TenantId))][MapperIgnoreTarget(nameof(AddressTypeDetailResponse.Name))][MapperIgnoreTarget(nameof(AddressTypeDetailResponse.Description))][MapperIgnoreTarget(nameof(AddressTypeDetailResponse.LanguageCode))] private static partial AddressTypeDetailResponse MapAddressTypeDetail(AddressTypeEntity source);
    [MapperIgnoreTarget(nameof(DocumentTypeResponse.Name))][MapperIgnoreTarget(nameof(DocumentTypeResponse.LanguageCode))] private static partial DocumentTypeResponse MapDocumentTypeResponse(DocumentTypeEntity source);
    [MapperIgnoreTarget(nameof(DocumentTypeDetailResponse.Name))][MapperIgnoreTarget(nameof(DocumentTypeDetailResponse.Description))][MapperIgnoreTarget(nameof(DocumentTypeDetailResponse.LanguageCode))] private static partial DocumentTypeDetailResponse MapDocumentTypeDetail(DocumentTypeEntity source);
    [MapperIgnoreTarget(nameof(FileTypeResponse.Name))][MapperIgnoreTarget(nameof(FileTypeResponse.LanguageCode))] private static partial FileTypeResponse MapFileTypeResponse(FileTypeEntity source);
    [MapperIgnoreTarget(nameof(FileTypeDetailResponse.Name))][MapperIgnoreTarget(nameof(FileTypeDetailResponse.Description))][MapperIgnoreTarget(nameof(FileTypeDetailResponse.LanguageCode))] private static partial FileTypeDetailResponse MapFileTypeDetail(FileTypeEntity source);
    [MapperIgnoreTarget(nameof(StatusDomainResponse.Name))][MapperIgnoreTarget(nameof(StatusDomainResponse.LanguageCode))] private static partial StatusDomainResponse MapStatusDomainResponse(StatusDomainEntity source);
    [MapperIgnoreTarget(nameof(StatusDomainDetailResponse.Name))][MapperIgnoreTarget(nameof(StatusDomainDetailResponse.Description))][MapperIgnoreTarget(nameof(StatusDomainDetailResponse.LanguageCode))] private static partial StatusDomainDetailResponse MapStatusDomainDetail(StatusDomainEntity source);
    [MapperIgnoreTarget(nameof(StatusDefinitionResponse.Name))][MapperIgnoreTarget(nameof(StatusDefinitionResponse.LanguageCode))] private static partial StatusDefinitionResponse MapStatusDefinitionResponse(StatusDefinitionEntity source);
    [MapperIgnoreTarget(nameof(StatusDefinitionDetailResponse.Name))][MapperIgnoreTarget(nameof(StatusDefinitionDetailResponse.LanguageCode))][MapperIgnoreTarget(nameof(StatusDefinitionDetailResponse.TenantName))] private static partial StatusDefinitionDetailResponse MapStatusDefinitionDetail(StatusDefinitionEntity source);
    [MapperIgnoreTarget(nameof(AcquisitionSourceTypeResponse.Name))][MapperIgnoreTarget(nameof(AcquisitionSourceTypeResponse.LanguageCode))] private static partial AcquisitionSourceTypeResponse MapAcquisitionSourceTypeResponse(AcquisitionSourceTypeEntity source);
    [MapperIgnoreTarget(nameof(AcquisitionSourceTypeDetailResponse.Name))][MapperIgnoreTarget(nameof(AcquisitionSourceTypeDetailResponse.Description))][MapperIgnoreTarget(nameof(AcquisitionSourceTypeDetailResponse.LanguageCode))] private static partial AcquisitionSourceTypeDetailResponse MapAcquisitionSourceTypeDetail(AcquisitionSourceTypeEntity source);
}
