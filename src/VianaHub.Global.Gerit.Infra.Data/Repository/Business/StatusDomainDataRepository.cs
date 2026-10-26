using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class StatusDomainDataRepository : IStatusDomainDataRepository
{
    public Task<bool> ExistsByTranslationAsync(string? languageCode, string? name, CancellationToken ct) => _context.Set<StatusDomainTranslationsEntity>().AnyAsync(x => x.LanguageCode == languageCode && x.Name == name, ct);

    private readonly GeritDbContext _context;

    public StatusDomainDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<StatusDomainEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<StatusDomainEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Code)
            .ToListAsync(ct);
    }

    public async Task<StatusDomainEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<StatusDomainEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<ListPage<StatusDomainEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<StatusDomainEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                (x.Code != null && EF.Functions.Like(x.Code.ToLower(), $"%{search}%")));
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

        return new ListPage<StatusDomainEntity>
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
        return await _context.Set<StatusDomainEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByCodeAsync(string? code, CancellationToken ct)
    {
        return await _context.Set<StatusDomainEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Code == code && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(StatusDomainEntity entity, CancellationToken ct)
    {
        await _context.Set<StatusDomainEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(StatusDomainEntity entity, CancellationToken ct)
    {
        _context.Set<StatusDomainEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
