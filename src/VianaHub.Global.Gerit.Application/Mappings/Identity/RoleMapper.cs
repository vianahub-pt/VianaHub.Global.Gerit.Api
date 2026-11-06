using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Role;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Identity;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class RoleMapper
{
    public static partial RoleResponse ToResponse(RoleEntity source);
    public static partial RoleDetailResponse ToDetail(RoleEntity source);

    public static IEnumerable<RoleResponse> ToResponses(IEnumerable<RoleEntity> source) => source.Select(ToResponse);

    public static ListPageResponse<RoleResponse> ToPaged(ListPage<RoleEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);
}
