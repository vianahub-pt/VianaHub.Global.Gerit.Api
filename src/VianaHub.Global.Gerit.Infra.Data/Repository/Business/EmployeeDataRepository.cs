using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class EmployeeDataRepository : IEmployeeDataRepository
{
    private readonly GeritDbContext _context;

    public EmployeeDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<EmployeeEntity>()
            .AsNoTracking()
            .Include($"{nameof(EmployeeEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(EmployeeEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<EmployeeEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<EmployeeEntity>()
            .AsNoTracking()
            .Include($"{nameof(EmployeeEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(EmployeeEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .Where(x => !x.IsDeleted).OrderBy(x => x.Name).ToListAsync(ct);
    }

    public async Task<ListPage<EmployeeEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<EmployeeEntity>()
            .AsNoTracking()
            .Include($"{nameof(EmployeeEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(EmployeeEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => (x.Name != null && EF.Functions.Like(x.Name.ToLower(), $"%{search}%")) ||
                                     (x.Email != null && (x.Email != null && EF.Functions.Like(x.Email.ToLower(), $"%{search}%"))));
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

        return new ListPage<EmployeeEntity>
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
        return await _context.Set<EmployeeEntity>().AsNoTracking().AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByEmailAsync(int tenantId, string? email, CancellationToken ct)
    {
        return await _context.Set<EmployeeEntity>().AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.Email == email && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(EmployeeEntity entity, CancellationToken ct)
    {
        await _context.Set<EmployeeEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(EmployeeEntity entity, CancellationToken ct)
    {
        _context.Set<EmployeeEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
