using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Function;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamFunction;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IVisitTeamFunctionAppService
{
    Task<IEnumerable<VisitTeamFunctionResponse>> GetAllAsync(int visitTeamId, CancellationToken ct);
    Task<VisitTeamFunctionDetailResponse?> GetByIdAsync(int visitTeamId, int id, CancellationToken ct);
    Task<ListPageResponse<VisitTeamFunctionResponse>> GetPagedAsync(int visitTeamId, PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(int visitTeamId, CreateVisitTeamFunctionRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int visitTeamId, int id, UpdateVisitTeamFunctionRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int visitTeamId, int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int visitTeamId, int id, CancellationToken ct);
    Task<bool> DeleteAsync(int visitTeamId, int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(int visitTeamId, IFormFile? file, CancellationToken ct);
}
