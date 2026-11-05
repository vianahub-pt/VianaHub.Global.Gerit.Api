using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.Plan;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.Plan;

namespace VianaHub.Global.Gerit.Application.Interfaces.Billing;

public interface IPlanAppService
{
    Task<IEnumerable<PlanResponse>> GetAllAsync(CancellationToken ct);
    Task<PlanDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<PlanResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreatePlanRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdatePlanRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct);
}
