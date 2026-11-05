using AutoMapper;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamVehicle;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

public class VisitTeamVehicleMappingProfile : Profile
{
    public VisitTeamVehicleMappingProfile()
    {
        CreateMap<VisitTeamVehicleEntity, VisitTeamVehicleResponse>()
            .ForMember(dest => dest.VisitTeam, opt => opt.MapFrom(src => src.VisitTeam == null || src.VisitTeam.Visit == null ? null : src.VisitTeam.Visit.Title))
            .ForMember(dest => dest.Vehicle, opt => opt.MapFrom(src => src.Vehicle == null ? null : src.Vehicle.Plate));

        CreateMap<ListPage<VisitTeamVehicleEntity>, ListPageResponse<VisitTeamVehicleResponse>>();
    }
}
