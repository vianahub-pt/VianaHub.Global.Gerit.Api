using AutoMapper;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Visit;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

/// <summary>
/// Profile de mapeamento para Visit (Intervention)
/// </summary>
public class VisitMappingProfile : Profile
{
    public VisitMappingProfile()
    {
        CreateMap<VisitEntity, VisitResponse>();

        CreateMap<VisitEntity, VisitDetailResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
            .ForMember(dest => dest.ClientId, opt => opt.MapFrom(src => src.ClientId))
            .ForMember(dest => dest.Client, opt => opt.MapFrom(src => src.Client != null ? src.Client.Name : null))
            .ForMember(dest => dest.StatusDefinitionId, opt => opt.MapFrom(src => src.StatusDefinitionId))
            .ForMember(dest => dest.StatusDefinitionName, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.StatusDefinition?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)))
            .ForMember(dest => dest.StatusDomainId, opt => opt.MapFrom(src => src.StatusDomainId))
            .ForMember(dest => dest.StatusDomainName, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.StatusDefinition?.StatusDomain?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)))
            .ForMember(dest => dest.StatusDefinition, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.StatusDefinition?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)))
            .ForMember(dest => dest.CurrencyCode, opt => opt.MapFrom(src => src.CurrencyCode))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.StartDateTime, opt => opt.MapFrom(src => src.StartDateTime))
            .ForMember(dest => dest.EndDateTime, opt => opt.MapFrom(src => src.EndDateTime))
            .ForMember(dest => dest.EstimatedValue, opt => opt.MapFrom(src => src.EstimatedValue))
            .ForMember(dest => dest.RealValue, opt => opt.MapFrom(src => src.RealValue))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<ListPage<VisitEntity>, ListPageResponse<VisitResponse>>();
    }
}
