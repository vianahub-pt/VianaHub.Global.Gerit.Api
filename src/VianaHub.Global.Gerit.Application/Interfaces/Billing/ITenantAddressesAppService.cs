using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.TenantAddress;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.TenantAddress;
using VianaHub.Global.Gerit.Domain.ReadModels;

namespace VianaHub.Global.Gerit.Application.Interfaces.Billing;

public interface ITenantAddressesAppService
{
    Task<IEnumerable<TenantAddressResponse>> GetByTenantIdAsync(int tenantId, CancellationToken ct);
    Task<TenantAddressDetailResponse?> GetByIdAsync(int tenantId, int id, CancellationToken ct);
    Task<ListPageResponse<TenantAddressResponse>> GetPagedAsync(int tenantId, PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(int tenantId, CreateTenantAddressRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int tenantId, int id, UpdateTenantAddressRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int tenantId, int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int tenantId, int id, CancellationToken ct);
    Task<bool> DeleteAsync(int tenantId, int id, CancellationToken ct);
}
