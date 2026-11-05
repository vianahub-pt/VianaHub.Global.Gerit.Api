using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeams;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeam;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IVisitTeamsAppService
{
    Task<IEnumerable<VisitTeamResponse>> GetAllAsync(CancellationToken ct);
    Task<VisitTeamDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<VisitTeamResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateVisitTeamRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateVisitTeamRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct);
}
