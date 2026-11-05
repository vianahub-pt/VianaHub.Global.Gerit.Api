using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeamEmployee;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamEmployee;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IVisitTeamEmployeeAppService
{
    Task<VisitTeamEmployeeDetailResponse?> GetByIdAsync(int visitTeamId, int id, CancellationToken ct);
    Task<IEnumerable<VisitTeamEmployeeResponse>> GetAllAsync(int visitTeamId, CancellationToken ct);
    Task<IEnumerable<VisitTeamEmployeeResponse>> GetActiveAsync(int visitTeamId, CancellationToken ct);
    Task<ListPageResponse<VisitTeamEmployeeResponse>> GetPagedAsync(int visitTeamId, PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(int visitTeamId, CreateVisitTeamEmployeeRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int visitTeamId, int id, UpdateVisitTeamEmployeeRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int visitTeamId, int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int visitTeamId, int id, CancellationToken ct);
    Task<bool> DeleteAsync(int visitTeamId, int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(int visitTeamId, IFormFile? file, CancellationToken ct);
}
