using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.Subscription;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Billing;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class SubscriptionMapper
{
    public static SubscriptionResponse ToResponse(SubscriptionEntity source, string culture)
    {
        var response = MapResponse(source);
        response.SubscriptionPlanName = TranslationResolver.Resolve(source.SubscriptionPlan?.Translations, culture, t => t.LanguageCode, t => t.Name) ?? string.Empty;
        response.StatusDomainName = TranslationResolver.Resolve(source.StatusDefinition?.StatusDomain?.Translations, culture, t => t.LanguageCode, t => t.Name);
        return response;
    }

    public static SubscriptionDetailResponse? ToDetail(SubscriptionEntity? source, string culture, DateTime utcNow)
    {
        if (source is null)
            return null;

        var response = MapDetail(source);
        response.SubscriptionPlanName = TranslationResolver.Resolve(source.SubscriptionPlan?.Translations, culture, t => t.LanguageCode, t => t.Name) ?? string.Empty;
        response.StatusDefinitionName = TranslationResolver.Resolve(source.StatusDefinition?.Translations, culture, t => t.LanguageCode, t => t.Name);
        response.StatusDomainName = TranslationResolver.Resolve(source.StatusDefinition?.StatusDomain?.Translations, culture, t => t.LanguageCode, t => t.Name);
        response.IsTrial = source.TrialEnd.HasValue && source.TrialEnd.Value > utcNow;
        response.DaysRemaining = Math.Max(0, (source.CurrentPeriodEnd - utcNow).Days);
        return response;
    }

    public static IEnumerable<SubscriptionResponse> ToResponses(IEnumerable<SubscriptionEntity> source, string culture) =>
        source.Select(entity => ToResponse(entity, culture));

    public static ListPageResponse<SubscriptionResponse> ToPaged(ListPage<SubscriptionEntity> source, string culture) =>
        new(ToResponses(source.Items, culture).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(SubscriptionResponse.SubscriptionPlanName))]
    [MapperIgnoreTarget(nameof(SubscriptionResponse.StatusDomainName))]
    private static partial SubscriptionResponse MapResponse(SubscriptionEntity source);

    [MapperIgnoreTarget(nameof(SubscriptionDetailResponse.SubscriptionPlanName))]
    [MapperIgnoreTarget(nameof(SubscriptionDetailResponse.StatusDefinitionName))]
    [MapperIgnoreTarget(nameof(SubscriptionDetailResponse.StatusDomainName))]
    [MapperIgnoreTarget(nameof(SubscriptionDetailResponse.IsTrial))]
    [MapperIgnoreTarget(nameof(SubscriptionDetailResponse.DaysRemaining))]
    private static partial SubscriptionDetailResponse MapDetail(SubscriptionEntity source);
}
