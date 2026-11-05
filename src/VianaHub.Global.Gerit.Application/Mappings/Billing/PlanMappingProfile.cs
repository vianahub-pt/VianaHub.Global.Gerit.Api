using AutoMapper;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.Plan;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Billing;

/// <summary>
/// Perfil de mapeamento do AutoMapper para SubscriptionPlanEntity.
/// Name e Description são resolvidos a partir das traduções com base na cultura atual.
/// </summary>
public class PlanMappingProfile : Profile
{
    public PlanMappingProfile()
    {
        // Mapeia SubscriptionPlanEntity -> PlanResponse
        // Name e Description são resolvidos via TranslationResolver com base na cultura atual
        CreateMap<SubscriptionPlanEntity, PlanResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.LanguageCode, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.ResolveUsedLanguageCode(src.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode)))
            .ForMember(dest => dest.Name, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)))
            .ForMember(dest => dest.PricePerMonth, opt => opt.MapFrom(src => src.PricePerMonth))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

        CreateMap<SubscriptionPlanEntity, PlanDetailResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.LanguageCode, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.ResolveUsedLanguageCode(src.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode)))
            .ForMember(dest => dest.Name, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)))
            .ForMember(dest => dest.Description, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.ResolveDescription(src.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Description)))
            .ForMember(dest => dest.PricePerHour, opt => opt.MapFrom(src => src.PricePerHour))
            .ForMember(dest => dest.PricePerDay, opt => opt.MapFrom(src => src.PricePerDay))
            .ForMember(dest => dest.PricePerMonth, opt => opt.MapFrom(src => src.PricePerMonth))
            .ForMember(dest => dest.PricePerYear, opt => opt.MapFrom(src => src.PricePerYear))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
            .ForMember(dest => dest.MaxUsers, opt => opt.MapFrom(src => src.MaxUsers))
            .ForMember(dest => dest.MaxPhotosPerVisit, opt => opt.MapFrom(src => src.MaxPhotosPerVisit))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<ListPage<SubscriptionPlanEntity>, ListPageResponse<PlanResponse>>();
    }
}
