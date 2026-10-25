using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.TenantContactPerson;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.TenantContactPerson;
using VianaHub.Global.Gerit.Domain.ReadModels;

namespace VianaHub.Global.Gerit.Application.Interfaces.Billing;

public interface ITenantContactPersonAppService
{
    Task<TenantContactPersonDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<TenantContactPersonResponse>> GetByTenantIdAsync(int tenantId, CancellationToken ct);
    Task<TenantContactPersonResponse?> GetPrimaryByTenantIdAsync(int tenantId, CancellationToken ct);
    Task<IEnumerable<TenantContactPersonResponse>> GetAllAsync(CancellationToken ct);
    Task<IEnumerable<TenantContactPersonResponse>> GetActiveAsync(CancellationToken ct);
    Task<ListPageResponse<TenantContactPersonResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(int tenantId, CreateTenantContactPersonRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int tenantId, int id, UpdateTenantContactPersonRequest request, CancellationToken ct);
    Task<bool> SetAsPrimaryAsync(int tenantId, int id, CancellationToken ct);
    Task<bool> ActivateAsync(int tenantId, int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int tenantId, int id, CancellationToken ct);
    Task<bool> DeleteAsync(int tenantId, int id, CancellationToken ct);
}
