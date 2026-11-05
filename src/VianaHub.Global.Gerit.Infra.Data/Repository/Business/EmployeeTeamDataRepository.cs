using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using System;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class EmployeeTeamDataRepository : IEmployeeTeamDataRepository
{
    private readonly GeritDbContext _context;

    public EmployeeTeamDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeTeamEntity?> GetByIdAsync(int employeeId, int id, CancellationToken ct)
    {
        return await _context.Set<EmployeeTeamEntity>()
            .AsNoTracking()
            .Include(x => x.Team)
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<EmployeeTeamEntity>> GetAllAsync(int employeeId, CancellationToken ct)
    {
        return await _context.Set<EmployeeTeamEntity>()
            .AsNoTracking()
            .Include(x => x.Team)
            .Include(x => x.Employee)
            .Where(x => x.EmployeeId == employeeId && !x.IsDeleted).OrderBy(x => x.Id).ToListAsync(ct);
    }

    public async Task<ListPage<EmployeeTeamEntity>> GetPagedAsync(int employeeId, PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<EmployeeTeamEntity>()
            .AsNoTracking()
            .Include(x => x.Team)
            .Include(x => x.Employee)
            .Where(x => x.EmployeeId == employeeId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x => (x.Team != null && x.Team.Name != null && EF.Functions.Like(x.Team.Name.ToString(), $"%{request.Search}%")) ||
                                     (x.Employee != null && x.Employee.Name != null && EF.Functions.Like(x.Employee.Name.ToString(), $"%{request.Search}%")));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        var total = await query.CountAsync(ct);

        var page = request.PageNumber ?? 1;
        var pageSize = request.PageSize ?? Paging.MinPageSize();
        var totalPages = pageSize > 0 ? (int)Math.Ceiling(total / (double)pageSize) : 0;

        var items = await query.OrderByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new ListPage<EmployeeTeamEntity>(items, page, pageSize, total, totalPages);
    }

    public async Task<bool> ExistsByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<EmployeeTeamEntity>().AsNoTracking().AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByTeamAndMemberAsync(int tenantId, int teamId, int EmployeeId, CancellationToken ct)
    {
        return await _context.Set<EmployeeTeamEntity>().AsNoTracking().AnyAsync(x => x.TeamId == teamId && x.EmployeeId == EmployeeId && x.TenantId == tenantId && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(EmployeeTeamEntity entity, CancellationToken ct)
    {
        await _context.AddAsync(entity, ct);
        var result = await _context.SaveChangesAsync(ct);
        return result > 0;
    }

    public async Task<bool> UpdateAsync(EmployeeTeamEntity entity, CancellationToken ct)
    {
        _context.Update(entity);
        var result = await _context.SaveChangesAsync(ct);
        return result > 0;
    }
}
