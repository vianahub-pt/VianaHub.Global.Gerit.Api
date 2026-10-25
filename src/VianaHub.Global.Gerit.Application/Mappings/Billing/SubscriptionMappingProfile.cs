using AutoMapper;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.Subscription;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Billing;

/// <summary>
/// Perfil de mapeamento do AutoMapper para SubscriptionEntity.
/// As traduções são resolvidas com base na cultura atual da requisição.
/// </summary>
public class SubscriptionMappingProfile : Profile
{
    public SubscriptionMappingProfile()
    {
        // Mapeia SubscriptionEntity -> SubscriptionResponse
        CreateMap<SubscriptionEntity, SubscriptionResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SubscriptionPlanName, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.SubscriptionPlan?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)
                ?? string.Empty))
            .ForMember(dest => dest.StatusDomainName, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.StatusDefinition?.StatusDomain?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

        // Mapeia SubscriptionEntity -> SubscriptionDetailResponse (com audit fields)
        CreateMap<SubscriptionEntity, SubscriptionDetailResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
            .ForMember(dest => dest.SubscriptionPlanId, opt => opt.MapFrom(src => src.SubscriptionPlanId))
            .ForMember(dest => dest.SubscriptionPlanName, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.SubscriptionPlan?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)
                ?? string.Empty))
            .ForMember(dest => dest.StatusDefinitionId, opt => opt.MapFrom(src => src.StatusDefinitionId))
            .ForMember(dest => dest.StatusDefinitionName, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.StatusDefinition?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)))
            .ForMember(dest => dest.StatusDomainId, opt => opt.MapFrom(src => src.StatusDomainId))
            .ForMember(dest => dest.StatusDomainName, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.StatusDefinition?.StatusDomain?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)))
            .ForMember(dest => dest.AgreedAmount, opt => opt.MapFrom(src => src.AgreedAmount))
            .ForMember(dest => dest.BillingInterval, opt => opt.MapFrom(src => src.BillingInterval))
            .ForMember(dest => dest.CurrencyCode, opt => opt.MapFrom(src => src.CurrencyCode))
            .ForMember(dest => dest.StripeId, opt => opt.MapFrom(src => src.StripeId))
            .ForMember(dest => dest.CurrentPeriodStart, opt => opt.MapFrom(src => src.CurrentPeriodStart))
            .ForMember(dest => dest.CurrentPeriodEnd, opt => opt.MapFrom(src => src.CurrentPeriodEnd))
            .ForMember(dest => dest.TrialStart, opt => opt.MapFrom(src => src.TrialStart))
            .ForMember(dest => dest.TrialEnd, opt => opt.MapFrom(src => src.TrialEnd))
            .ForMember(dest => dest.CancelAtPeriodEnd, opt => opt.MapFrom(src => src.CancelAtPeriodEnd))
            .ForMember(dest => dest.CanceledAt, opt => opt.MapFrom(src => src.CanceledAt))
            .ForMember(dest => dest.CancellationReason, opt => opt.MapFrom(src => src.CancellationReason))
            .ForMember(dest => dest.StripeCustomerId, opt => opt.MapFrom(src => src.StripeCustomerId))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.IsTrial, opt => opt.MapFrom(src =>
                src.TrialEnd.HasValue && src.TrialEnd.Value > DateTime.UtcNow))
            .ForMember(dest => dest.DaysRemaining, opt => opt.MapFrom(src =>
                (src.CurrentPeriodEnd - DateTime.UtcNow).Days > 0
                    ? (src.CurrentPeriodEnd - DateTime.UtcNow).Days
                    : 0))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<ListPage<SubscriptionEntity>, ListPageResponse<SubscriptionResponse>>();
    }
}
