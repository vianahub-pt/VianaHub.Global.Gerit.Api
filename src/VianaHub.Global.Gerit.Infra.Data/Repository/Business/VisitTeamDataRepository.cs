using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using System;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class VisitTeamDataRepository : IVisitTeamDataRepository
{
    private readonly GeritDbContext _context;

    public VisitTeamDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<VisitTeamEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<VisitTeamEntity>()
            .AsNoTracking()
            .Include(x => x.Team)
            .Include(x => x.Visit)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<VisitTeamEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<VisitTeamEntity>()
            .AsNoTracking()
            .Include(x => x.Team)
            .Include(x => x.Visit)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<ListPage<VisitTeamEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<VisitTeamEntity>()
            .AsNoTracking()
            .Include(x => x.Team)
            .Include(x => x.Visit)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                (x.Team != null && x.Team.Name != null && EF.Functions.Like(x.Team.Name.ToLower(), $"%{search}%")) ||
                (x.Visit != null && x.Visit.Title != null && EF.Functions.Like(x.Visit.Title.ToLower(), $"%{search}%")));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        var count = await query.CountAsync(ct);

        var pageNumber = request.PageNumber ?? 1;
        var pageSize = request.PageSize ?? Paging.MinPageSize();

        var result = await query.OrderByDescending(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new ListPage<VisitTeamEntity>
        {
            Items = result,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = (int)Math.Ceiling((double)count / pageSize)
        };
    }

    public async Task<bool> ExistsByIdAsync(int tenantId, int interventionId, int teamId, CancellationToken ct)
    {
        return await _context.Set<VisitTeamEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.VisitId == interventionId && x.TeamId == teamId && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(VisitTeamEntity entity, CancellationToken ct)
    {
        await _context.Set<VisitTeamEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(VisitTeamEntity entity, CancellationToken ct)
    {
        _context.Set<VisitTeamEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
