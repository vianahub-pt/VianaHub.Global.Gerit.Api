using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamEquipments;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class VisitTeamEquipmentMapper
{
    public static VisitTeamEquipmentResponse ToResponse(VisitTeamEquipmentEntity source)
    {
        var response = MapResponse(source);
        response.VisitTeam = source.VisitTeam?.Visit?.Title;
        response.Equipment = source.Equipment?.Name;
        return response;
    }
    public static VisitTeamEquipmentDetailResponse ToDetail(VisitTeamEquipmentEntity source)
    {
        var response = MapDetail(source);
        response.VisitTeam = source.VisitTeam?.Visit?.Title;
        response.Equipment = source.Equipment?.Name;
        return response;
    }
    public static IEnumerable<VisitTeamEquipmentResponse> ToResponses(IEnumerable<VisitTeamEquipmentEntity> source) => source.Select(ToResponse);
    public static ListPageResponse<VisitTeamEquipmentResponse> ToPaged(ListPage<VisitTeamEquipmentEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(VisitTeamEquipmentResponse.VisitTeam))]
    [MapperIgnoreTarget(nameof(VisitTeamEquipmentResponse.Equipment))]
    private static partial VisitTeamEquipmentResponse MapResponse(VisitTeamEquipmentEntity source);
    [MapperIgnoreTarget(nameof(VisitTeamEquipmentDetailResponse.VisitTeam))]
    [MapperIgnoreTarget(nameof(VisitTeamEquipmentDetailResponse.Equipment))]
    private static partial VisitTeamEquipmentDetailResponse MapDetail(VisitTeamEquipmentEntity source);
}
