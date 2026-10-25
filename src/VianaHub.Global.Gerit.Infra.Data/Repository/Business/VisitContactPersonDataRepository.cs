using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class VisitContactPersonDataRepository : IVisitContactDataRepository
{
    private readonly GeritDbContext _context;

    public VisitContactPersonDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<VisitContactPersonsEntity?> GetByIdAsync(int visitId, int id, CancellationToken ct)
    {
        return await _context.Set<VisitContactPersonsEntity>()
            .AsNoTracking()
            .Include(x => x.Visit)
            .FirstOrDefaultAsync(x => x.VisitId == visitId && x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<VisitContactPersonsEntity>> GetAllAsync(int visitId, CancellationToken ct)
    {
        return await _context.Set<VisitContactPersonsEntity>()
            .AsNoTracking()
            .Include(x => x.Visit)
            .Where(x => x.VisitId == visitId && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    public async Task<ListPage<VisitContactPersonsEntity>> GetPagedAsync(int visitId, PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<VisitContactPersonsEntity>()
            .AsNoTracking()
            .Include(x => x.Visit)
            .Where(x => x.VisitId == visitId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                (x.Name != null && EF.Functions.Like(x.Name.ToLower(), $"%{search}%")) ||
                (x.Email != null && EF.Functions.Like(x.Email.ToLower(), $"%{search}%")) ||
                (x.Phone != null && EF.Functions.Like(x.Phone.ToLower(), $"%{search}%")) ||
                (x.Visit != null && x.Visit.Title != null && EF.Functions.Like(x.Visit.Title.ToLower(), $"%{search}%")));
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

        return new ListPage<VisitContactPersonsEntity>
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
        return await _context.Set<VisitContactPersonsEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByVisitAndEmailAsync(int interventionId, string? email, int? excludeId, CancellationToken ct)
    {
        var query = _context.Set<VisitContactPersonsEntity>()
            .AsNoTracking()
            .Where(x => x.VisitId == interventionId && x.Email == email && !x.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync(ct);
    }

    public async Task<bool> AddAsync(VisitContactPersonsEntity entity, CancellationToken ct)
    {
        await _context.Set<VisitContactPersonsEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(VisitContactPersonsEntity entity, CancellationToken ct)
    {
        _context.Set<VisitContactPersonsEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
