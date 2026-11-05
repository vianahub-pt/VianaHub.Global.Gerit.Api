using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.Subscription;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.Subscription;

namespace VianaHub.Global.Gerit.Application.Interfaces.Billing;

public interface ISubscriptionAppService
{
    Task<IEnumerable<SubscriptionResponse>> GetAllAsync(CancellationToken ct);
    Task<SubscriptionDetailResponse> GetByIdAsync(int id, CancellationToken ct);
    Task<SubscriptionDetailResponse> GetByTenantIdAsync(int tenantId, CancellationToken ct);
    Task<ListPageResponse<SubscriptionResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<IEnumerable<SubscriptionResponse>> GetActiveSubscriptionsAsync(CancellationToken ct);
    Task<IEnumerable<SubscriptionResponse>> GetExpiringSubscriptionsAsync(int daysBeforeExpiration, CancellationToken ct);

    Task<int> CreateAsync(CreateSubscriptionRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateSubscriptionRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> CancelAsync(int id, CancelSubscriptionRequest request, CancellationToken ct);
    Task<bool> RenewAsync(int id, RenewSubscriptionRequest request, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct);
}
