using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Resource;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Identity;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class ResourceMapper
{
    public static partial ResourceResponse ToResponse(ResourceEntity source);
    public static partial ResourceDetailResponse ToDetail(ResourceEntity source);

    public static IEnumerable<ResourceResponse> ToResponses(IEnumerable<ResourceEntity> source) => source.Select(ToResponse);

    public static ListPageResponse<ResourceResponse> ToPaged(ListPage<ResourceEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);
}
