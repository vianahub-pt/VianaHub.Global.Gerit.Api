using AutoMapper;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamEquipments;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

public class VisitTeamEquipmentMappingProfile : Profile
{
    public VisitTeamEquipmentMappingProfile()
    {
        CreateMap<VisitTeamEquipmentEntity, VisitTeamEquipmentResponse>()
            .ForMember(dest => dest.VisitTeam, opt => opt.MapFrom(src => src.VisitTeam == null || src.VisitTeam.Visit == null ? null : src.VisitTeam.Visit.Title))
            .ForMember(dest => dest.Equipment, opt => opt.MapFrom(src => src.Equipment == null ? null : src.Equipment.Name));
        CreateMap<VisitTeamEquipmentEntity, VisitTeamEquipmentDetailResponse>()
            .ForMember(dest => dest.VisitTeam, opt => opt.MapFrom(src => src.VisitTeam == null || src.VisitTeam.Visit == null ? null : src.VisitTeam.Visit.Title))
            .ForMember(dest => dest.Equipment, opt => opt.MapFrom(src => src.Equipment == null ? null : src.Equipment.Name));
        CreateMap<ListPage<VisitTeamEquipmentEntity>, ListPageResponse<VisitTeamEquipmentResponse>>();
    }
}
