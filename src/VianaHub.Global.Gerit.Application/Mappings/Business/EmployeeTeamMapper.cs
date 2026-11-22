using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeTeams;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class EmployeeTeamMapper
{
    public static EmployeeTeamResponse ToResponse(EmployeeTeamEntity source)
    {
        var response = MapResponse(source);
        response.Team = source.Team == null ? null : source.Team.Name;
        response.Employee = source.Employee == null ? null : source.Employee.Name;
        return response;
    }

    public static EmployeeTeamDetailResponse ToDetail(EmployeeTeamEntity source)
    {
        var response = MapDetail(source);
        response.Team = source.Team == null ? null : source.Team.Name;
        response.Employee = source.Employee == null ? null : source.Employee.Name;
        return response;
    }

    public static IEnumerable<EmployeeTeamResponse> ToResponses(IEnumerable<EmployeeTeamEntity> source) => source.Select(ToResponse);
    public static ListPageResponse<EmployeeTeamResponse> ToPaged(ListPage<EmployeeTeamEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(EmployeeTeamResponse.Team))]
    [MapperIgnoreTarget(nameof(EmployeeTeamResponse.Employee))]
    private static partial EmployeeTeamResponse MapResponse(EmployeeTeamEntity source);

    [MapperIgnoreTarget(nameof(EmployeeTeamDetailResponse.Team))]
    [MapperIgnoreTarget(nameof(EmployeeTeamDetailResponse.Employee))]
    private static partial EmployeeTeamDetailResponse MapDetail(EmployeeTeamEntity source);
}
