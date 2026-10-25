using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Services.Billing;

public class SubscriptionDomainService : ISubscriptionDomainService
{
    private readonly ISubscriptionDataRepository _repo;
    private readonly IEntityDomainValidator<SubscriptionEntity> _validator;
    private readonly INotify _notify;

    public SubscriptionDomainService(
        ISubscriptionDataRepository repo,
        IEntityDomainValidator<SubscriptionEntity> validator,
        INotify notify)
    {
        _repo = repo;
        _validator = validator;
        _notify = notify;
    }

    public async Task<SubscriptionEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _repo.GetByIdAsync(id, ct);
    }
    public async Task<IEnumerable<SubscriptionEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _repo.GetAllAsync(ct);
    }
    public async Task<ListPage<SubscriptionEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        return await _repo.GetPagedAsync(request, ct);
    }
    public async Task<bool> ExistsByIdAsync(int id, CancellationToken ct)
    {
        return await _repo.ExistsByIdAsync(id, ct);
    }
    public async Task<bool> ExistsByTenantIdAsync(int tenantId, CancellationToken ct)
    {
        return await _repo.ExistsByTenantIdAsync(tenantId, ct);
    }
    public async Task<SubscriptionEntity?> GetByTenantIdAsync(int tenantId, CancellationToken ct)
    {
        return await _repo.GetByTenantIdAsync(tenantId, ct);
    }
    public async Task<IEnumerable<SubscriptionEntity>> GetBySubscriptionPlanIdAsync(int subscriptionPlanId, CancellationToken ct)
    {
        return await _repo.GetBySubscriptionPlanIdAsync(subscriptionPlanId, ct);
    }
    public async Task<IEnumerable<SubscriptionEntity>> GetActiveSubscriptionsAsync(CancellationToken ct)
    {
        return await _repo.GetActiveSubscriptionsAsync(ct);
    }
    public async Task<IEnumerable<SubscriptionEntity>> GetExpiringSubscriptionsAsync(int daysBeforeExpiration, CancellationToken ct)
    {
        return await _repo.GetExpiringSubscriptionsAsync(daysBeforeExpiration, ct);
    }

    public async Task<bool> CreateAsync(SubscriptionEntity entity, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateForCreateAsync(entity);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                _notify.Add(error.ErrorMessage, 400);
            }
            return false;
        }

        return await _repo.AddAsync(entity, ct);
    }
    public async Task<bool> UpdateAsync(SubscriptionEntity entity, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateForUpdateAsync(entity);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                _notify.Add(error.ErrorMessage, 400);
            }
            return false;
        }

        return await _repo.UpdateAsync(entity, ct);
    }
    public async Task<bool> ActivateAsync(SubscriptionEntity entity, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateForActivateAsync(entity);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                _notify.Add(error.ErrorMessage, 400);
            }
            return false;
        }

        return await _repo.UpdateAsync(entity, ct);
    }
    public async Task<bool> DeactivateAsync(SubscriptionEntity entity, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateForDeactivateAsync(entity);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                _notify.Add(error.ErrorMessage, 400);
            }
            return false;
        }

        return await _repo.UpdateAsync(entity, ct);
    }
    public async Task<bool> DeleteAsync(SubscriptionEntity entity, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateForDeleteAsync(entity);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                _notify.Add(error.ErrorMessage, 400);
            }
            return false;
        }

        return await _repo.UpdateAsync(entity, ct);
    }
    public async Task<bool> CancelAsync(SubscriptionEntity entity, CancellationToken ct)
    {
        return await _repo.UpdateAsync(entity, ct);
    }
    public async Task<bool> RenewAsync(SubscriptionEntity entity, CancellationToken ct)
    {
        return await _repo.UpdateAsync(entity, ct);
    }
    public async Task<bool> IsTrialAsync(int tenantId, CancellationToken ct)
    {
        return await _repo.IsTrialAsync(tenantId, ct);
    }
    public async Task<bool> IsDeletedAsync(int tenantId, CancellationToken ct)
    {
        return await _repo.IsDeletedAsync(tenantId, ct);
    }
    public async Task<bool> IsActiveAsync(int tenantId, CancellationToken ct)
    {
        return await _repo.IsActiveAsync(tenantId, ct);
    }
    public async Task<bool> IsCanceledAsync(int tenantId, CancellationToken ct)
    {
        return await _repo.IsCanceledAsync(tenantId, ct);
    }
    public async Task<bool> IsTrialPeriodExpiredAsync(int tenantId, CancellationToken ct)
    {
        return await _repo.IsTrialPeriodExpiredAsync(tenantId, ct);
    }
    public async Task<bool> IsSubscriptionPeriodExpiredAsync(int tenantId, CancellationToken ct)
    {
        return await _repo.IsSubscriptionPeriodExpiredAsync(tenantId, ct);
    }

}
