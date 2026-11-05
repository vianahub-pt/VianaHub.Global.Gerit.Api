using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class VisitDataRepository : IVisitDataRepository
{
    private readonly GeritDbContext _context;

    public VisitDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<VisitEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<VisitEntity>()
            .AsNoTracking()
            .Include(x => x.Client)
            .Include($"{nameof(VisitEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(VisitEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<VisitEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<VisitEntity>()
            .AsNoTracking()
            .Include(x => x.Client)
            .Include($"{nameof(VisitEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(VisitEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.StartDateTime)
            .ToListAsync(ct);
    }

    public async Task<ListPage<VisitEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<VisitEntity>()
            .AsNoTracking()
            .Include(x => x.Client)
            .Include($"{nameof(VisitEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(VisitEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                (x.Title != null && EF.Functions.Like(x.Title.ToLower(), $"%{search}%")) ||
                (x.Description != null && EF.Functions.Like(x.Description.ToLower(), $"%{search}%")));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        var count = await query.CountAsync(ct);
        var orderedQuery = CreateSort.ApplyOrdering(query, request);
        var pageNumber = request.PageNumber ?? 1;
        var pageSize = request.PageSize ?? Paging.MinPageSize();

        var result = await orderedQuery.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new ListPage<VisitEntity>
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
        return await _context.Set<VisitEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByTenantAndTitleAsync(int tenantId, string? title, int? excludeId, CancellationToken ct)
    {
        var query = _context.Set<VisitEntity>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Title == title && !x.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync(ct);
    }

    public async Task<bool> AddAsync(VisitEntity entity, CancellationToken ct)
    {
        await _context.Set<VisitEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(VisitEntity entity, CancellationToken ct)
    {
        _context.Set<VisitEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
