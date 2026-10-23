using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Role;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Role;

namespace VianaHub.Global.Gerit.Application.Interfaces.Identity;

public interface IRoleAppService
{
    Task<IEnumerable<RoleResponse>> GetAllAsync(CancellationToken ct);
    Task<RoleDetailResponse> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<RoleResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateRoleRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile file, CancellationToken ct);
}
