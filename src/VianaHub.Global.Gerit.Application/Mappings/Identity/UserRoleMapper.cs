using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.UserRole;
using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Application.Mappings.Identity;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class UserRoleMapper
{
    [MapProperty(nameof(UserRolesEntity.User), nameof(UserRoleResponse.UserName), Use = nameof(UserName))]
    [MapProperty(nameof(UserRolesEntity.Role), nameof(UserRoleResponse.RoleName), Use = nameof(RoleName))]
    public static partial UserRoleResponse ToResponse(UserRolesEntity source);

    [MapProperty(nameof(UserRolesEntity.User), nameof(UserRoleDetailResponse.UserName), Use = nameof(UserName))]
    [MapProperty(nameof(UserRolesEntity.Role), nameof(UserRoleDetailResponse.RoleName), Use = nameof(RoleName))]
    [MapperIgnoreTarget(nameof(UserRoleDetailResponse.IsDeleted))]
    [MapperIgnoreTarget(nameof(UserRoleDetailResponse.CreatedBy))]
    [MapperIgnoreTarget(nameof(UserRoleDetailResponse.CreatedAt))]
    [MapperIgnoreTarget(nameof(UserRoleDetailResponse.ModifiedBy))]
    [MapperIgnoreTarget(nameof(UserRoleDetailResponse.ModifiedAt))]
    public static partial UserRoleDetailResponse ToDetail(UserRolesEntity source);

    public static IList<UserRoleResponse> ToResponses(IEnumerable<UserRolesEntity> source) => source.Select(ToResponse).ToList();

    private static string UserName(UserEntity? source) => source?.Name ?? string.Empty;
    private static string RoleName(RoleEntity? source) => source?.Name ?? string.Empty;
}
