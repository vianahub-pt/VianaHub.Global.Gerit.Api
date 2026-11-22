using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Visit;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class VisitMapper
{
    public static partial VisitResponse ToResponse(VisitEntity source);

    public static VisitDetailResponse ToDetail(VisitEntity source, string culture)
    {
        var response = MapDetail(source);
        response.Client = source.Client?.Name;
        response.StatusDefinitionName = TranslationResolver.Resolve(source.StatusDefinition?.Translations, culture, t => t.LanguageCode, t => t.Name);
        response.StatusDomainName = TranslationResolver.Resolve(source.StatusDefinition?.StatusDomain?.Translations, culture, t => t.LanguageCode, t => t.Name);
        response.StatusDefinition = response.StatusDefinitionName;
        return response;
    }

    public static IEnumerable<VisitResponse> ToResponses(IEnumerable<VisitEntity> source) => source.Select(ToResponse);

    public static ListPageResponse<VisitResponse> ToPaged(ListPage<VisitEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(VisitDetailResponse.Client))]
    [MapperIgnoreTarget(nameof(VisitDetailResponse.StatusDefinitionName))]
    [MapperIgnoreTarget(nameof(VisitDetailResponse.StatusDomainName))]
    [MapperIgnoreTarget(nameof(VisitDetailResponse.StatusDefinition))]
    private static partial VisitDetailResponse MapDetail(VisitEntity source);
}
