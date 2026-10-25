using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Billing;

public class PlanDataRepository : IPlanDataRepository
{
    public Task<bool> ExistsByTranslationAsync(string? languageCode, string? name, CancellationToken ct) => _context.Set<SubscriptionPlanTranslationsEntity>().AnyAsync(x => x.LanguageCode == languageCode && x.Name == name, ct);

    public Task<bool> ExistsByCodeAsync(string? code, CancellationToken ct) => _context.Set<SubscriptionPlanEntity>().AnyAsync(x => x.Code == code, ct);

    private readonly GeritDbContext _context;

    public PlanDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionPlanEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<SubscriptionPlanEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<SubscriptionPlanEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<SubscriptionPlanEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => !x.IsDeleted)
            .ToListAsync(ct);
    }

    public async Task<ListPage<SubscriptionPlanEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<SubscriptionPlanEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => !x.IsDeleted);

        // Filtro de busca — pesquisa nas traduções
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();

            query = query.Where(x =>
                x.Translations.Any(t =>
                    (t.Name != null && EF.Functions.Like(t.Name.ToLower(), $"%{search}%"))
                    || (t.Description != null && (t.Description != null && EF.Functions.Like(t.Description.ToLower(), $"%{search}%")))
                )
                || (x.Currency != null && EF.Functions.Like(x.Currency.ToLower(), $"%{search}%"))
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

        return new ListPage<SubscriptionPlanEntity>
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
        return await _context.Set<SubscriptionPlanEntity>().AsNoTracking().AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByNameAsync(string? name, CancellationToken ct)
    {
        return await _context.Set<SubscriptionPlanTranslationsEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Name == name && !x.SubscriptionPlan.IsDeleted, ct);
    }

    public async Task<bool> ExistsByNameAsync(string? name, string? languageCode, CancellationToken ct)
    {
        return await _context.Set<SubscriptionPlanTranslationsEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Name == name && x.LanguageCode == languageCode && !x.SubscriptionPlan.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(SubscriptionPlanEntity entity, CancellationToken ct)
    {
        await _context.Set<SubscriptionPlanEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(SubscriptionPlanEntity entity, CancellationToken ct)
    {
        _context.Set<SubscriptionPlanEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
