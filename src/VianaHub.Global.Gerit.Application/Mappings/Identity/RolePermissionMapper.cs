using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.RolePermission;
using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Application.Mappings.Identity;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class RolePermissionMapper
{
    [MapProperty(nameof(RolePermissionsEntity.Role), nameof(RolePermissionResponse.RoleName), Use = nameof(RoleName))]
    [MapProperty(nameof(RolePermissionsEntity.Resource), nameof(RolePermissionResponse.ResourceName), Use = nameof(ResourceName))]
    [MapProperty(nameof(RolePermissionsEntity.Action), nameof(RolePermissionResponse.ActionName), Use = nameof(ActionName))]
    public static partial RolePermissionResponse ToResponse(RolePermissionsEntity source);

    [MapProperty(nameof(RolePermissionsEntity.Role), nameof(RolePermissionDetailResponse.RoleName), Use = nameof(RoleName))]
    [MapProperty(nameof(RolePermissionsEntity.Resource), nameof(RolePermissionDetailResponse.ResourceName), Use = nameof(ResourceName))]
    [MapProperty(nameof(RolePermissionsEntity.Action), nameof(RolePermissionDetailResponse.ActionName), Use = nameof(ActionName))]
    public static partial RolePermissionDetailResponse ToDetail(RolePermissionsEntity source);

    public static IList<RolePermissionResponse> ToResponses(IEnumerable<RolePermissionsEntity> source) => source.Select(ToResponse).ToList();

    private static string RoleName(RoleEntity? source) => source?.Name ?? string.Empty;
    private static string ResourceName(ResourceEntity? source) => source?.Name ?? string.Empty;
    private static string ActionName(ActionEntity? source) => source?.Name ?? string.Empty;
}
