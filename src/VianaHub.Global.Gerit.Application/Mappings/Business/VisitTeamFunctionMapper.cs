using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamFunction;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class VisitTeamFunctionMapper
{
    public static VisitTeamFunctionResponse ToResponse(VisitTeamFunctionsEntity source)
    {
        var response = MapResponse(source);
        response.VisitTeam = null;
        return response;
    }
    public static VisitTeamFunctionDetailResponse ToDetail(VisitTeamFunctionsEntity source) => MapDetail(source);
    public static IEnumerable<VisitTeamFunctionResponse> ToResponses(IEnumerable<VisitTeamFunctionsEntity> source) => source.Select(ToResponse);
    public static ListPageResponse<VisitTeamFunctionResponse> ToPaged(ListPage<VisitTeamFunctionsEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(VisitTeamFunctionResponse.VisitTeam))]
    private static partial VisitTeamFunctionResponse MapResponse(VisitTeamFunctionsEntity source);
    private static partial VisitTeamFunctionDetailResponse MapDetail(VisitTeamFunctionsEntity source);
}
