using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class VisitTeamEmployeeDataRepository : IVisitTeamEmployeeDataRepository
{
    private readonly GeritDbContext _context;

    public VisitTeamEmployeeDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<VisitTeamEmployeeEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<VisitTeamEmployeeEntity>()
            .AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.Function)
            .Include(x => x.VisitTeam)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<VisitTeamEmployeeEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<VisitTeamEmployeeEntity>()
            .AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.Function)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.StartDateTime)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<VisitTeamEmployeeEntity>> GetByVisitTeamIdAsync(int visitTeamId, CancellationToken ct)
    {
        return await _context.Set<VisitTeamEmployeeEntity>()
            .AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.Function)
            .Where(x => x.VisitTeamId == visitTeamId && !x.IsDeleted)
            .OrderByDescending(x => x.IsLeader)
            .ThenBy(x => x.Employee.Name)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<VisitTeamEmployeeEntity>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct)
    {
        return await _context.Set<VisitTeamEmployeeEntity>()
            .AsNoTracking()
            .Include($"{nameof(VisitTeamEmployeeEntity.VisitTeam)}.{nameof(VisitTeamEntity.Visit)}")
            .Include(x => x.Function)
            .Where(x => x.EmployeeId == employeeId && !x.IsDeleted)
            .OrderByDescending(x => x.StartDateTime)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<VisitTeamEmployeeEntity>> GetActiveByVisitTeamIdAsync(int visitTeamId, CancellationToken ct)
    {
        return await _context.Set<VisitTeamEmployeeEntity>()
            .AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.Function)
            .Where(x => x.VisitTeamId == visitTeamId && x.EndDateTime == null && !x.IsDeleted)
            .OrderByDescending(x => x.IsLeader)
            .ThenBy(x => x.Employee.Name)
            .ToListAsync(ct);
    }

    public async Task<ListPage<VisitTeamEmployeeEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<VisitTeamEmployeeEntity>()
            .AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.Function)
            .Include(x => x.VisitTeam)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => (x.Employee != null && x.Employee.Name != null && EF.Functions.Like(x.Employee.Name.ToLower(), $"%{search}%")) ||
                                     (x.Function != null && x.Function.Name != null && EF.Functions.Like(x.Function.Name.ToLower(), $"%{search}%")));
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

        return new ListPage<VisitTeamEmployeeEntity>
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
        return await _context.Set<VisitTeamEmployeeEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsActiveAssignmentAsync(int visitTeamId, int employeeId, CancellationToken ct)
    {
        return await _context.Set<VisitTeamEmployeeEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.VisitTeamId == visitTeamId &&
                          x.EmployeeId == employeeId &&
                          x.EndDateTime == null &&
                          !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(VisitTeamEmployeeEntity entity, CancellationToken ct)
    {
        await _context.Set<VisitTeamEmployeeEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(VisitTeamEmployeeEntity entity, CancellationToken ct)
    {
        _context.Set<VisitTeamEmployeeEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
