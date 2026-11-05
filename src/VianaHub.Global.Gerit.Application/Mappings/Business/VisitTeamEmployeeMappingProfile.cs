using AutoMapper;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeamEmployee;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamEmployee;
using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

public class VisitTeamEmployeeMappingProfile : Profile
{
    public VisitTeamEmployeeMappingProfile()
    {
        CreateMap<VisitTeamEmployeeEntity, VisitTeamEmployeeResponse>()
            .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee == null ? null : src.Employee.Name))
            .ForMember(dest => dest.VisitTeamFunctionName, opt => opt.MapFrom(src => src.Function == null ? null : src.Function.Name));

        CreateMap<VisitTeamEmployeeEntity, VisitTeamEmployeeDetailResponse>()
            .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee == null ? null : src.Employee.Name))
            .ForMember(dest => dest.VisitTeamFunctionName, opt => opt.MapFrom(src => src.Function == null ? null : src.Function.Name));

        CreateMap<CreateVisitTeamEmployeeRequest, VisitTeamEmployeeEntity>();

        CreateMap<UpdateVisitTeamEmployeeRequest, VisitTeamEmployeeEntity>();
    }
}
