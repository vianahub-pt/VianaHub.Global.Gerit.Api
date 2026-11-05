using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.RolePermission;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.RolePermission;

namespace VianaHub.Global.Gerit.Application.Interfaces.Identity;

public interface IRolePermissionAppService
{
    Task<RolePermissionResponse?> CreateAsync(CreateRolePermissionRequest request, CancellationToken ct);
    Task DeleteAsync(int roleId, int resourceId, int actionId, CancellationToken ct);
    Task<RolePermissionDetailResponse> GetByIdAsync(int roleId, int resourceId, int actionId, CancellationToken ct);
    Task<IList<RolePermissionResponse>> GetByRoleAsync(int roleId, CancellationToken ct);
    Task<IList<RolePermissionResponse>> GetByResourceAsync(int resourceId, CancellationToken ct);
    Task<IList<RolePermissionResponse>> GetAllAsync(CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct);
}
