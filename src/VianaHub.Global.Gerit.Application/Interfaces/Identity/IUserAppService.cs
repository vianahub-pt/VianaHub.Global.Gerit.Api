using VianaHub.Global.Gerit.Application.Dtos.Base;
using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.User;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.User;

namespace VianaHub.Global.Gerit.Application.Interfaces.Identity;

public interface IUserAppService
{
    Task<IEnumerable<UserResponse>> GetAllAsync(CancellationToken ct);
    Task<UserDetailResponse> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<UserResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateUserRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct);
    Task<bool> UpdatePasswordAsync(int id, UpdatePasswordRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct);
}
