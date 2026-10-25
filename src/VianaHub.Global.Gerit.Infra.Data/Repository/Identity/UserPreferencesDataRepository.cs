using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Identity;

public class UserPreferencesDataRepository : IUserPreferencesDataRepository
{
    private readonly GeritDbContext _context;

    public UserPreferencesDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<UserPreferencesEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<UserPreferencesEntity>()
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<UserPreferencesEntity?> GetByUserAsync(int tenantId, int userId, CancellationToken ct)
    {
        return await _context.Set<UserPreferencesEntity>()
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<UserPreferencesEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<UserPreferencesEntity>()
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.User)
            .Where(x => !x.IsDeleted).ToListAsync(ct);
    }

    public async Task<ListPage<UserPreferencesEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<UserPreferencesEntity>()
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.User)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => (x.Appearance != null && EF.Functions.Like(x.Appearance.ToLower(), $"%{search}%")) ||
                                     (x.Locale != null && EF.Functions.Like(x.Locale.ToLower(), $"%{search}%")) ||
                                     (x.Timezone != null && EF.Functions.Like(x.Timezone.ToLower(), $"%{search}%")) ||
                                     (x.TimeFormat != null && EF.Functions.Like(x.TimeFormat.ToLower(), $"%{search}%")));
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

        return new ListPage<UserPreferencesEntity>
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
        return await _context.Set<UserPreferencesEntity>().AsNoTracking().AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByUserAsync(int tenantId, int userId, CancellationToken ct)
    {
        return await _context.Set<UserPreferencesEntity>().AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.UserId == userId && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(UserPreferencesEntity entity, CancellationToken ct)
    {
        await _context.Set<UserPreferencesEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(UserPreferencesEntity entity, CancellationToken ct)
    {
        _context.Set<UserPreferencesEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
