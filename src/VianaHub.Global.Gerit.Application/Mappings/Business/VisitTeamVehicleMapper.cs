using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamVehicle;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamVehicles;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class VisitTeamVehicleMapper
{
    public static VisitTeamVehicleResponse ToResponse(VisitTeamVehicleEntity source)
    {
        var response = MapResponse(source);
        response.VisitTeam = source.VisitTeam?.Visit?.Title;
        response.Vehicle = source.Vehicle?.Plate;
        return response;
    }
    public static VisitTeamVehicleDetailResponse ToDetail(VisitTeamVehicleEntity source)
    {
        var response = MapDetail(source);
        response.VisitTeam = source.VisitTeam?.Visit?.Title;
        response.Vehicle = source.Vehicle?.Plate;
        return response;
    }
    public static IEnumerable<VisitTeamVehicleResponse> ToResponses(IEnumerable<VisitTeamVehicleEntity> source) => source.Select(ToResponse);
    public static ListPageResponse<VisitTeamVehicleResponse> ToPaged(ListPage<VisitTeamVehicleEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(VisitTeamVehicleResponse.VisitTeam))]
    [MapperIgnoreTarget(nameof(VisitTeamVehicleResponse.Vehicle))]
    private static partial VisitTeamVehicleResponse MapResponse(VisitTeamVehicleEntity source);
    [MapperIgnoreTarget(nameof(VisitTeamVehicleDetailResponse.VisitTeam))]
    [MapperIgnoreTarget(nameof(VisitTeamVehicleDetailResponse.Vehicle))]
    private static partial VisitTeamVehicleDetailResponse MapDetail(VisitTeamVehicleEntity source);
}
