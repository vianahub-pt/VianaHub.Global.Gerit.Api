using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitAttachment;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class VisitAttachmentMapper
{
    public static IEnumerable<VisitAttachmentResponse> ToResponses(IEnumerable<VisitAttachmentsEntity> source, string culture) => source.Select(e => ToResponse(e, culture));

    public static ListPageResponse<VisitAttachmentResponse> ToPaged(ListPage<VisitAttachmentsEntity> source, string culture) =>
        new(ToResponses(source.Items, culture).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    public static VisitAttachmentResponse ToResponse(VisitAttachmentsEntity source, string culture)
    {
        return MapResponse(source) with
        {
            FileTypeName = TranslationResolver.Resolve(source.FileType?.Translations, culture, t => t.LanguageCode, t => t.Name),
            MimeType = source.FileType?.MimeType
        };
    }

    public static VisitAttachmentDetailResponse ToDetail(VisitAttachmentsEntity source, string culture)
    {
        var response = MapDetail(source);
        response.FileTypeName = TranslationResolver.Resolve(source.FileType?.Translations, culture, t => t.LanguageCode, t => t.Name);
        response.MimeType = source.FileType?.MimeType;
        response.FormattedFileSize = source.GetFormattedFileSize();
        return response;
    }

    [MapperIgnoreTarget(nameof(VisitAttachmentResponse.FileTypeName))]
    [MapperIgnoreTarget(nameof(VisitAttachmentResponse.MimeType))]
    private static partial VisitAttachmentResponse MapResponse(VisitAttachmentsEntity source);

    [MapperIgnoreTarget(nameof(VisitAttachmentDetailResponse.FileTypeName))]
    [MapperIgnoreTarget(nameof(VisitAttachmentDetailResponse.MimeType))]
    [MapperIgnoreTarget(nameof(VisitAttachmentDetailResponse.FormattedFileSize))]
    private static partial VisitAttachmentDetailResponse MapDetail(VisitAttachmentsEntity source);
}
