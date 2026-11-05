using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class EmployeeContactPersonDataRepository : IEmployeeContactPersonDataRepository
{
    private readonly GeritDbContext _context;

    public EmployeeContactPersonDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeContactPersonsEntity?> GetByIdAsync(int employeeId, int id, CancellationToken ct)
    {
        return await _context.Set<EmployeeContactPersonsEntity>()
            .AsNoTracking()
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<EmployeeContactPersonsEntity>> GetAllAsync(int employeeId, CancellationToken ct)
    {
        return await _context.Set<EmployeeContactPersonsEntity>()
            .AsNoTracking()
            .Include(x => x.Employee)
            .Where(x => x.EmployeeId == employeeId && !x.IsDeleted).OrderBy(x => x.Name).ToListAsync(ct);
    }

    public async Task<ListPage<EmployeeContactPersonsEntity>> GetPagedAsync(int employeeId, PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<EmployeeContactPersonsEntity>()
            .AsNoTracking()
            .Include(x => x.Employee)
            .Where(x => x.EmployeeId == employeeId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => (x.Name != null && EF.Functions.Like(x.Name.ToLower(), $"%{search}%")) ||
                                     (x.Email != null && EF.Functions.Like(x.Email.ToLower(), $"%{search}%")) ||
                                     (x.Phone != null && EF.Functions.Like(x.Phone.ToLower(), $"%{search}%")) ||
                                     (x.Employee != null && x.Employee.Name != null && EF.Functions.Like(x.Employee.Name.ToLower(), $"%{search}%")));
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

        return new ListPage<EmployeeContactPersonsEntity>
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
        return await _context.Set<EmployeeContactPersonsEntity>().AsNoTracking().AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByEmailAsync(int tenantId, int EmployeeId, string? email, CancellationToken ct)
    {
        return await _context.Set<EmployeeContactPersonsEntity>().AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.EmployeeId == EmployeeId && x.Email == email && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByEmailForUpdateAsync(int tenantId, int EmployeeId, string? email, int excludeId, CancellationToken ct)
    {
        return await _context.Set<EmployeeContactPersonsEntity>().AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.EmployeeId == EmployeeId && x.Email == email && x.Id != excludeId && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(EmployeeContactPersonsEntity entity, CancellationToken ct)
    {
        await _context.Set<EmployeeContactPersonsEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(EmployeeContactPersonsEntity entity, CancellationToken ct)
    {
        _context.Set<EmployeeContactPersonsEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
