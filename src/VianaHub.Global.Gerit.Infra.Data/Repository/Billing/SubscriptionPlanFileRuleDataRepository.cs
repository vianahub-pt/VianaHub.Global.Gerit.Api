using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Billing;

public class SubscriptionPlanFileRuleDataRepository : ISubscriptionPlanFileRuleDataRepository
{
    private readonly GeritDbContext _context;

    public SubscriptionPlanFileRuleDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SubscriptionPlanFileRulesEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<SubscriptionPlanFileRulesEntity>()
            .AsNoTracking()
            .Include(x => x.SubscriptionPlan)
                .ThenInclude(x => x.Translations)
            .Include(x => x.FileType)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.SubscriptionPlanId)
            .ThenBy(x => x.FileType.MimeType)
            .ToListAsync(ct);
    }

    public async Task<SubscriptionPlanFileRulesEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<SubscriptionPlanFileRulesEntity>()
            .AsNoTracking()
            .Include(x => x.SubscriptionPlan)
                .ThenInclude(x => x.Translations)
            .Include(x => x.FileType)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<SubscriptionPlanFileRulesEntity>> GetBySubscriptionPlanIdAsync(int subscriptionPlanId, CancellationToken ct)
    {
        return await _context.Set<SubscriptionPlanFileRulesEntity>()
            .AsNoTracking()
            .Include(x => x.FileType)
            .Where(x => x.SubscriptionPlanId == subscriptionPlanId && !x.IsDeleted)
            .OrderBy(x => x.FileType.MimeType)
            .ToListAsync(ct);
    }

    public async Task<ListPage<SubscriptionPlanFileRulesEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<SubscriptionPlanFileRulesEntity>()
            .AsNoTracking()
            .Include(x => x.SubscriptionPlan)
            .Include(x => x.FileType)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.SubscriptionPlan.Translations.Any(t =>
                    (t.Name != null && EF.Functions.Like(t.Name.ToLower(), $"%{search}%"))) ||
                (x.FileType != null && x.FileType.MimeType != null && EF.Functions.Like(x.FileType.MimeType.ToLower(), $"%{search}%")));
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

        return new ListPage<SubscriptionPlanFileRulesEntity>
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
        return await _context.Set<SubscriptionPlanFileRulesEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(SubscriptionPlanFileRulesEntity entity, CancellationToken ct)
    {
        await _context.Set<SubscriptionPlanFileRulesEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(SubscriptionPlanFileRulesEntity entity, CancellationToken ct)
    {
        _context.Set<SubscriptionPlanFileRulesEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
