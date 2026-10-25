using AutoMapper;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeTeams;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

public class EmployeeTeamMappingProfile : Profile
{
    public EmployeeTeamMappingProfile()
    {
        CreateMap<EmployeeTeamEntity, EmployeeTeamResponse>()
            .ForMember(dest => dest.Team, opt => opt.MapFrom(src => src.Team == null ? null : src.Team.Name))
            .ForMember(dest => dest.Employee, opt => opt.MapFrom(src => src.Employee == null ? null : src.Employee.Name));
        CreateMap<EmployeeTeamEntity, EmployeeTeamDetailResponse>()
            .ForMember(dest => dest.Team, opt => opt.MapFrom(src => src.Team == null ? null : src.Team.Name))
            .ForMember(dest => dest.Employee, opt => opt.MapFrom(src => src.Employee == null ? null : src.Employee.Name));
        CreateMap<ListPage<EmployeeTeamEntity>, ListPageResponse<EmployeeTeamResponse>>();
    }
}
