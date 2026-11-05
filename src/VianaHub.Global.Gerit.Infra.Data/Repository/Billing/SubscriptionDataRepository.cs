using VianaHub.Global.Gerit.Domain.Entities.Business;
using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Billing;

public class SubscriptionDataRepository : ISubscriptionDataRepository
{
    private readonly GeritDbContext _context;

    public SubscriptionDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<SubscriptionEntity>()
            .AsNoTracking()
            .Include(x => x.SubscriptionPlan)
                .ThenInclude(x => x.Translations)
            .Include(x => x.Tenant)
            .Include($"{nameof(SubscriptionEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(SubscriptionEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<SubscriptionEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<SubscriptionEntity>()
            .AsNoTracking()
            .Include(x => x.SubscriptionPlan)
                .ThenInclude(x => x.Translations)
            .Include(x => x.Tenant)
            .Include($"{nameof(SubscriptionEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(SubscriptionEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ListPage<SubscriptionEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<SubscriptionEntity>()
            .AsNoTracking()
            .Include(x => x.SubscriptionPlan)
                .ThenInclude(x => x.Translations)
            .Include(x => x.Tenant)
            .Include($"{nameof(SubscriptionEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(SubscriptionEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .Where(x => !x.IsDeleted);

        // Filtro de busca
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();

            query = query.Where(x =>
                x.SubscriptionPlan.Translations.Any(t =>
                    (t.Name != null && EF.Functions.Like(t.Name.ToLower(), $"%{search}%")))
                || (x.StripeId != null && EF.Functions.Like(x.StripeId.ToLower(), $"%{search}%"))
                || (x.StripeCustomerId != null && EF.Functions.Like(x.StripeCustomerId.ToLower(), $"%{search}%"))
            );
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        var count = await query.CountAsync(ct);

        var orderedQuery = CreateSort.ApplyOrdering(query, request);

        var pageNumber = request.PageNumber ?? 1;
        var pageSize = request.PageSize ?? Paging.MinPageSize();

        var result = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new ListPage<SubscriptionEntity>
        {
            Items = result,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = (int)Math.Ceiling((double)count / pageSize)
        };
    }

    public async Task<bool> ExistsByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<SubscriptionEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByTenantIdAsync(int tenantId, CancellationToken ct)
    {
        return await _context.Set<SubscriptionEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && !x.IsDeleted, ct);
    }

    public async Task<SubscriptionEntity?> GetByTenantIdAsync(int tenantId, CancellationToken ct)
    {
        return await _context.Set<SubscriptionEntity>()
            .AsNoTracking()
            .Include(x => x.SubscriptionPlan)
                .ThenInclude(x => x.Translations)
            .Include(x => x.Tenant)
            .Include($"{nameof(SubscriptionEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(SubscriptionEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<SubscriptionEntity>> GetBySubscriptionPlanIdAsync(int subscriptionPlanId, CancellationToken ct)
    {
        return await _context.Set<SubscriptionEntity>()
            .AsNoTracking()
            .Include(x => x.SubscriptionPlan)
                .ThenInclude(x => x.Translations)
            .Include(x => x.Tenant)
            .Include($"{nameof(SubscriptionEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(SubscriptionEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .Where(x => x.SubscriptionPlanId == subscriptionPlanId && !x.IsDeleted)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<SubscriptionEntity>> GetActiveSubscriptionsAsync(CancellationToken ct)
    {
        return await _context.Set<SubscriptionEntity>()
            .AsNoTracking()
            .Include(x => x.SubscriptionPlan)
                .ThenInclude(x => x.Translations)
            .Include(x => x.Tenant)
            .Include($"{nameof(SubscriptionEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(SubscriptionEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .Where(x => x.IsActive && !x.IsDeleted)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<SubscriptionEntity>> GetExpiringSubscriptionsAsync(int daysBeforeExpiration, CancellationToken ct)
    {
        var targetDate = DateTime.UtcNow.AddDays(daysBeforeExpiration);

        return await _context.Set<SubscriptionEntity>()
            .AsNoTracking()
            .Include(x => x.SubscriptionPlan)
                .ThenInclude(x => x.Translations)
            .Include(x => x.Tenant)
            .Include($"{nameof(SubscriptionEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(SubscriptionEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .Where(x => x.IsActive
                && !x.IsDeleted
                && x.CurrentPeriodEnd <= targetDate
                && x.CurrentPeriodEnd > DateTime.UtcNow)
            .ToListAsync(ct);
    }

    public async Task<bool> IsTrialAsync(int tenantId, CancellationToken ct)
    {
        return await _context.Set<SubscriptionEntity>()
            .AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.TrialStart.HasValue && x.TrialEnd.HasValue, ct);
    }
    public async Task<bool> IsDeletedAsync(int tenantId, CancellationToken ct)
    {
        return await _context.Set<SubscriptionEntity>()
            .AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.IsDeleted, ct);
    }
    public async Task<bool> IsActiveAsync(int tenantId, CancellationToken ct)
    {
        return await _context.Set<SubscriptionEntity>()
            .AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.IsActive, ct);
    }
    public async Task<bool> IsCanceledAsync(int tenantId, CancellationToken ct)
    {
        return await _context.Set<SubscriptionEntity>()
            .AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.CanceledAt.HasValue && x.CancelAtPeriodEnd, ct);
    }
    public async Task<bool> IsTrialPeriodExpiredAsync(int tenantId, CancellationToken ct)
    {
        return await _context.Set<SubscriptionEntity>()
            .AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.TrialEnd.HasValue && x.TrialEnd.Value < DateTime.UtcNow, ct);
    }
    public async Task<bool> IsSubscriptionPeriodExpiredAsync(int tenantId, CancellationToken ct)
    {
        return await _context.Set<SubscriptionEntity>()
            .AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.CurrentPeriodEnd < DateTime.UtcNow, ct);
    }

    public async Task<bool> AddAsync(SubscriptionEntity entity, CancellationToken ct)
    {
        await _context.Set<SubscriptionEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(SubscriptionEntity entity, CancellationToken ct)
    {
        _context.Set<SubscriptionEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
