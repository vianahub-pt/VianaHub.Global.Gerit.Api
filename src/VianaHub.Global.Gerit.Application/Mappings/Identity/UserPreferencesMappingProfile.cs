using AutoMapper;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.UserPreferences;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Identity;

public class UserPreferencesMappingProfile : Profile
{
    public UserPreferencesMappingProfile()
    {
        CreateMap<UserPreferencesEntity, UserPreferencesResponse>()
            .ForMember(dest => dest.Tenant, opt => opt.MapFrom(src => src.Tenant == null ? null : src.Tenant.Name))
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User == null ? null : src.User.Name))
            .ForMember(dest => dest.DayStart, opt => opt.MapFrom(src => src.DayStart.ToString(@"hh\:mm")))
            .ForMember(dest => dest.DayEnd, opt => opt.MapFrom(src => src.DayEnd.ToString(@"hh\:mm")));

        CreateMap<UserPreferencesEntity, UserPreferencesDetailResponse>()
            .ForMember(dest => dest.Tenant, opt => opt.MapFrom(src => src.Tenant == null ? null : src.Tenant.Name))
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User == null ? null : src.User.Name))
            .ForMember(dest => dest.DayStart, opt => opt.MapFrom(src => src.DayStart.ToString(@"hh\:mm")))
            .ForMember(dest => dest.DayEnd, opt => opt.MapFrom(src => src.DayEnd.ToString(@"hh\:mm")));

        CreateMap<ListPage<UserPreferencesEntity>, ListPageResponse<UserPreferencesResponse>>();
    }
}

