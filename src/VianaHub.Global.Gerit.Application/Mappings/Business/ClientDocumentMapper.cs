using System.Globalization;
using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientDocument;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper]
public static partial class ClientDocumentMapper
{
    [MapProperty(nameof(ClientDocumentsEntity.DocumentType), nameof(ClientDocumentResponse.DocumentType), Use = nameof(MapDocumentType))]
    [MapperIgnoreSource(nameof(ClientDocumentsEntity.TenantId))]
    [MapperIgnoreSource(nameof(ClientDocumentsEntity.ClientId))]
    [MapperIgnoreSource(nameof(ClientDocumentsEntity.IsDeleted))]
    [MapperIgnoreSource(nameof(ClientDocumentsEntity.Client))]
    [MapperIgnoreSource(nameof(ClientDocumentsEntity.Tenant))]
    [MapperIgnoreSource(nameof(ClientDocumentsEntity.CreatedBy))]
    [MapperIgnoreSource(nameof(ClientDocumentsEntity.CreatedAt))]
    [MapperIgnoreSource(nameof(ClientDocumentsEntity.ModifiedBy))]
    [MapperIgnoreSource(nameof(ClientDocumentsEntity.ModifiedAt))]
    public static partial ClientDocumentResponse ToResponse(ClientDocumentsEntity source);

    [MapProperty("Client.Name", nameof(ClientDocumentDetailResponse.Client))]
    [MapProperty(nameof(ClientDocumentsEntity.DocumentType), nameof(ClientDocumentDetailResponse.DocumentType), Use = nameof(MapDocumentType))]
    [MapperIgnoreSource(nameof(ClientDocumentsEntity.Tenant))]
    public static partial ClientDocumentDetailResponse ToDetail(ClientDocumentsEntity source);

    public static ListPageResponse<ClientDocumentResponse> ToPaged(ListPage<ClientDocumentsEntity> source) =>
        new(source.Items.Select(ToResponse).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [UserMapping]
    private static string? MapDocumentType(DocumentTypeEntity? source) => source?.Translations == null
        ? null
        : TranslationResolver.Resolve(source.Translations, CultureInfo.CurrentCulture.Name, x => x.LanguageCode, x => x.Name);
}
