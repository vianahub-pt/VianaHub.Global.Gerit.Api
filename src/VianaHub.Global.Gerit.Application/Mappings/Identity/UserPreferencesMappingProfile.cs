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
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

        CreateMap<ListPage<UserPreferencesEntity>, ListPageResponse<UserPreferencesResponse>>();
    }
}

