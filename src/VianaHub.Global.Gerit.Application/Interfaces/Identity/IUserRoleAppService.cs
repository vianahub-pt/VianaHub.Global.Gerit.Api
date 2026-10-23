using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.UserRole;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.UserRole;

namespace VianaHub.Global.Gerit.Application.Interfaces.Identity;

public interface IUserRoleAppService
{
    Task<UserRoleDetailResponse> GetByIdAsync(int userId, int roleId, CancellationToken ct);
    Task<IList<UserRoleResponse>> GetByUserAsync(int userId, CancellationToken ct);
    Task<IList<UserRoleResponse>> GetByRoleAsync(int roleId, CancellationToken ct);
    Task<IList<UserRoleResponse>> GetAllAsync(CancellationToken ct);
    Task<UserRoleResponse> CreateAsync(CreateUserRoleRequest request, CancellationToken ct);
    Task DeleteAsync(int userId, int roleId, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile file, CancellationToken ct);
}
