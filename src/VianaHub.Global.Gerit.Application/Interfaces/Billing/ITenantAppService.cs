using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.Tenant;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.Tenant;

namespace VianaHub.Global.Gerit.Application.Interfaces.Billing;

public interface ITenantAppService
{
    Task<IEnumerable<TenantResponse>> GetAllAsync(CancellationToken ct);
    Task<TenantDetailResponse> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<TenantResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateTenantRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateTenantRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile file, CancellationToken ct);
}
