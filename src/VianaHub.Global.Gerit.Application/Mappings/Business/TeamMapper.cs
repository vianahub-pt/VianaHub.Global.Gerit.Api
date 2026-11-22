using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Team;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class TeamMapper
{
    public static partial TeamResponse ToResponse(TeamEntity source);
    public static partial TeamDetailResponse ToDetail(TeamEntity source);
    public static IEnumerable<TeamResponse> ToResponses(IEnumerable<TeamEntity> source) => source.Select(ToResponse);
    public static ListPageResponse<TeamResponse> ToPaged(ListPage<TeamEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);
}
