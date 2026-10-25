using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Billing;

public interface ISubscriptionPlanFileRuleDataRepository
{
    Task<IEnumerable<SubscriptionPlanFileRulesEntity>> GetAllAsync(CancellationToken ct);
    Task<SubscriptionPlanFileRulesEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<SubscriptionPlanFileRulesEntity>> GetBySubscriptionPlanIdAsync(int subscriptionPlanId, CancellationToken ct);
    Task<ListPage<SubscriptionPlanFileRulesEntity>> GetPagedAsync(PagedFilter filter, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);

    Task<bool> AddAsync(SubscriptionPlanFileRulesEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(SubscriptionPlanFileRulesEntity entity, CancellationToken ct);
}
