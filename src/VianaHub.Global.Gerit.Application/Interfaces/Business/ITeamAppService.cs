using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Team;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Team;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface ITeamAppService
{
    Task<IEnumerable<TeamResponse>> GetAllAsync(CancellationToken ct);
    Task<TeamDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<TeamResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateTeamRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateTeamRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct);
}
