using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeam;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class VisitTeamMapper
{
    public static VisitTeamResponse ToResponse(VisitTeamEntity source)
    {
        var response = MapResponse(source);
        response.Team = source.Team?.Name;
        response.Visit = source.Visit?.Title;
        return response;
    }
    public static VisitTeamDetailResponse ToDetail(VisitTeamEntity source)
    {
        var response = MapDetail(source);
        response.Team = source.Team?.Name;
        response.Visit = source.Visit?.Title;
        return response;
    }
    public static IEnumerable<VisitTeamResponse> ToResponses(IEnumerable<VisitTeamEntity> source) => source.Select(ToResponse);
    public static ListPageResponse<VisitTeamResponse> ToPaged(ListPage<VisitTeamEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(VisitTeamResponse.Team))]
    [MapperIgnoreTarget(nameof(VisitTeamResponse.Visit))]
    private static partial VisitTeamResponse MapResponse(VisitTeamEntity source);
    [MapperIgnoreTarget(nameof(VisitTeamDetailResponse.Team))]
    [MapperIgnoreTarget(nameof(VisitTeamDetailResponse.Visit))]
    private static partial VisitTeamDetailResponse MapDetail(VisitTeamEntity source);
}
