using System.Globalization;
using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.Plan;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Billing;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class PlanMapper
{
    public static PlanResponse ToResponse(SubscriptionPlanEntity source, string culture)
    {
        var response = MapResponse(source);
        response.LanguageCode = TranslationResolver.ResolveUsedLanguageCode(source.Translations, culture, t => t.LanguageCode);
        response.Name = TranslationResolver.Resolve(source.Translations, culture, t => t.LanguageCode, t => t.Name);
        return response;
    }

    public static PlanDetailResponse ToDetail(SubscriptionPlanEntity source, string culture)
    {
        var response = MapDetail(source);
        response.LanguageCode = TranslationResolver.ResolveUsedLanguageCode(source.Translations, culture, t => t.LanguageCode);
        response.Name = TranslationResolver.Resolve(source.Translations, culture, t => t.LanguageCode, t => t.Name);
        response.Description = TranslationResolver.ResolveDescription(source.Translations, culture, t => t.LanguageCode, t => t.Description);
        return response;
    }

    public static IEnumerable<PlanResponse> ToResponses(IEnumerable<SubscriptionPlanEntity> source, string culture) =>
        source.Select(entity => ToResponse(entity, culture));

    public static ListPageResponse<PlanResponse> ToPaged(ListPage<SubscriptionPlanEntity> source, string culture) =>
        new(ToResponses(source.Items, culture).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(PlanResponse.LanguageCode))]
    [MapperIgnoreTarget(nameof(PlanResponse.Name))]
    private static partial PlanResponse MapResponse(SubscriptionPlanEntity source);

    [MapperIgnoreTarget(nameof(PlanDetailResponse.LanguageCode))]
    [MapperIgnoreTarget(nameof(PlanDetailResponse.Name))]
    [MapperIgnoreTarget(nameof(PlanDetailResponse.Description))]
    private static partial PlanDetailResponse MapDetail(SubscriptionPlanEntity source);
}
