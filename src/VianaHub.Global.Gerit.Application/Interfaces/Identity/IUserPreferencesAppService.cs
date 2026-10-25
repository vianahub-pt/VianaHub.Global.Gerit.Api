using VianaHub.Global.Gerit.Application.Dtos.Base;
using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.UserPreferences;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.UserPreferences;

namespace VianaHub.Global.Gerit.Application.Interfaces.Identity;

public interface IUserPreferencesAppService
{
    Task<IEnumerable<UserPreferencesResponse>> GetAllAsync(CancellationToken ct);
    Task<UserPreferencesDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<UserPreferencesResponse?> GetByUserAsync(int userId, CancellationToken ct);
    Task<ListPageResponse<UserPreferencesResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateUserPreferencesRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateUserPreferencesRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
