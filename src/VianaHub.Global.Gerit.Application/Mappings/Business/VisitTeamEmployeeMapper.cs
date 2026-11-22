using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamEmployee;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class VisitTeamEmployeeMapper
{
    public static VisitTeamEmployeeResponse ToResponse(VisitTeamEmployeeEntity source)
    {
        return MapResponse(source) with
        {
            EmployeeName = source.Employee?.Name,
            VisitTeamFunctionName = source.Function?.Name
        };
    }
    public static VisitTeamEmployeeDetailResponse ToDetail(VisitTeamEmployeeEntity source)
    {
        var response = MapDetail(source);
        response.EmployeeName = source.Employee?.Name;
        response.VisitTeamFunctionName = source.Function?.Name;
        return response;
    }
    public static IEnumerable<VisitTeamEmployeeResponse> ToResponses(IEnumerable<VisitTeamEmployeeEntity> source) => source.Select(ToResponse);
    public static ListPageResponse<VisitTeamEmployeeResponse> ToPaged(ListPage<VisitTeamEmployeeEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(VisitTeamEmployeeResponse.EmployeeName))]
    [MapperIgnoreTarget(nameof(VisitTeamEmployeeResponse.VisitTeamFunctionName))]
    private static partial VisitTeamEmployeeResponse MapResponse(VisitTeamEmployeeEntity source);
    [MapperIgnoreTarget(nameof(VisitTeamEmployeeDetailResponse.EmployeeName))]
    [MapperIgnoreTarget(nameof(VisitTeamEmployeeDetailResponse.VisitTeamFunctionName))]
    private static partial VisitTeamEmployeeDetailResponse MapDetail(VisitTeamEmployeeEntity source);
}
