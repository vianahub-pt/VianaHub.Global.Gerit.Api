using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.TenantFiscalData;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.TenantFiscalData;
using VianaHub.Global.Gerit.Domain.ReadModels;

namespace VianaHub.Global.Gerit.Application.Interfaces.Billing;

public interface ITenantFiscalDataAppService
{
    Task<IEnumerable<TenantFiscalDataResponse>> GetByTenantIdAsync(int tenantId, CancellationToken ct);
    Task<TenantFiscalDataDetailResponse?> GetByIdAsync(int tenantId, int id, CancellationToken ct);
    Task<ListPageResponse<TenantFiscalDataResponse>> GetPagedAsync(int tenantId, PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(int tenantId, CreateTenantFiscalDataRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int tenantId, int id, UpdateTenantFiscalDataRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int tenantId, int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int tenantId, int id, CancellationToken ct);
    Task<bool> DeleteAsync(int tenantId, int id, CancellationToken ct);
}
