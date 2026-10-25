using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Billing;

public interface IPlanDataRepository
{
    Task<bool> ExistsByTranslationAsync(string? languageCode, string? name, CancellationToken ct);

    Task<bool> ExistsByCodeAsync(string? code, CancellationToken ct);
    Task<SubscriptionPlanEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<SubscriptionPlanEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<SubscriptionPlanEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByNameAsync(string? name, CancellationToken ct);
    Task<bool> ExistsByNameAsync(string? name, string? languageCode, CancellationToken ct);
    Task<bool> AddAsync(SubscriptionPlanEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(SubscriptionPlanEntity entity, CancellationToken ct);
}
