using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Action;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Identity;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class ActionMapper
{
    public static partial ActionResponse ToResponse(ActionEntity source);
    public static partial ActionDetailResponse ToDetail(ActionEntity source);

    public static IEnumerable<ActionResponse> ToResponses(IEnumerable<ActionEntity> source) => source.Select(ToResponse);

    public static ListPageResponse<ActionResponse> ToPaged(ListPage<ActionEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);
}
