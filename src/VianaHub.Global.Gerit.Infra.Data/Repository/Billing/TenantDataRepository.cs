using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Security;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Billing;

public class TenantDataRepository : ITenantDataRepository
{
    private readonly GeritDbContext _context;

    public TenantDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<TenantEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<TenantEntity>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<TenantEntity?> GetByUserEmailAsync(string? email, CancellationToken ct)
    {
        await _context.Database.OpenConnectionAsync(ct);
        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_set_session_context @key=N'IsSuperAdmin', @value=1", ct);

            return await _context.Set<TenantEntity>()
                .AsNoTracking()
                .Include(x => x.Users)
                .Where(x => !x.IsDeleted && x.Users.Any(u => u.Email == email))
                .FirstOrDefaultAsync(ct);
        }
        finally
        {
            try
            {
                await SqlRlsSessionContext.ResetAsync(_context.Database.GetDbConnection(), CancellationToken.None);
            }
            catch
            {
                if (_context.Database.GetDbConnection() is SqlConnection sql) SqlConnection.ClearPool(sql);
                throw;
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }

    public async Task<IEnumerable<TenantEntity>> GetLoginAsync(CancellationToken ct)
    {
        return await _context.Set<TenantEntity>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<TenantEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<TenantEntity>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            // order by Id instead of Name to avoid SQL errors when 'Name' column is missing in some DB schemas
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<ListPage<TenantEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<TenantEntity>().AsNoTracking().Where(x => !x.IsDeleted);

        // ?? Filtro de busca
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();

            query = query.Where(x =>
                (x.Name != null && EF.Functions.Like(x.Name.ToLower(), $"%{search}%"))
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

        return new ListPage<TenantEntity>
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
        return await _context.Set<TenantEntity>().AsNoTracking().AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByNameAsync(string? name, CancellationToken ct)
    {
        return await _context.Set<TenantEntity>().AsNoTracking().AnyAsync(x => x.Name == name && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(TenantEntity entity, CancellationToken ct)
    {
        await _context.Set<TenantEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(TenantEntity entity, CancellationToken ct)
    {
        _context.Set<TenantEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
