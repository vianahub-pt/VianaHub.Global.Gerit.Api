using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Billing;

public interface ISubscriptionDomainService
{
    Task<SubscriptionEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<SubscriptionEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<SubscriptionEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByTenantIdAsync(int tenantId, CancellationToken ct);
    Task<SubscriptionEntity?> GetByTenantIdAsync(int tenantId, CancellationToken ct);
    Task<IEnumerable<SubscriptionEntity>> GetBySubscriptionPlanIdAsync(int subscriptionPlanId, CancellationToken ct);
    Task<IEnumerable<SubscriptionEntity>> GetActiveSubscriptionsAsync(CancellationToken ct);
    Task<IEnumerable<SubscriptionEntity>> GetExpiringSubscriptionsAsync(int daysBeforeExpiration, CancellationToken ct);
    Task<bool> IsTrialAsync(int tenantId, CancellationToken ct);
    Task<bool> IsDeletedAsync(int tenantId, CancellationToken ct);
    Task<bool> IsActiveAsync(int tenantId, CancellationToken ct);
    Task<bool> IsCanceledAsync(int tenantId, CancellationToken ct);
    Task<bool> IsTrialPeriodExpiredAsync(int tenantId, CancellationToken ct);
    Task<bool> IsSubscriptionPeriodExpiredAsync(int tenantId, CancellationToken ct);

    Task<bool> CreateAsync(SubscriptionEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(SubscriptionEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(SubscriptionEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(SubscriptionEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(SubscriptionEntity entity, CancellationToken ct);
    Task<bool> CancelAsync(SubscriptionEntity entity, CancellationToken ct);
    Task<bool> RenewAsync(SubscriptionEntity entity, CancellationToken ct);
}
