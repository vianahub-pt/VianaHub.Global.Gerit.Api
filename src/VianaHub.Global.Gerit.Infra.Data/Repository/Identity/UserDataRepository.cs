using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Exceptions;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Identity;

public class UserDataRepository : IUserDataRepository
{
    private readonly GeritDbContext _context;

    public UserDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<UserEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<UserEntity>()
            .Include(x => x.Tenant)
            .Include(x => x.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<UserEntity?> GetByEmailAsync(string? email, CancellationToken ct)
    {
        var normalizedEmail = email?.ToUpperInvariant();
        return await _context.Set<UserEntity>()
            .Include(x => x.Tenant)
            .Include(x => x.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<UserEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<UserEntity>()
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<ListPage<UserEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<UserEntity>()
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(x => !x.IsDeleted);

        // ?? Filtro de busca
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();

            query = query.Where(x =>
                (x.Name != null && EF.Functions.Like(x.Name.ToLower(), $"%{search}%"))
                || (x.Email != null && EF.Functions.Like(x.Email.ToLower(), $"%{search}%"))
            );
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        var count = await query.CountAsync(ct);

        var orderedQuery = CreateSort.ApplyOrdering(query, request).ThenBy(x => x.Id);

        var pageNumber = request.PageNumber ?? 1;
        var pageSize = request.PageSize ?? Paging.MinPageSize();

        var result = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new ListPage<UserEntity>
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
        return await _context.Set<UserEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByEmailAsync(string? email, CancellationToken ct)
    {
        var normalizedEmail = email?.ToUpperInvariant();
        return await _context.Set<UserEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.NormalizedEmail == normalizedEmail && !x.IsDeleted, ct);
    }

    public Task<bool> IsDeletedAsync(int id, int tenantId, CancellationToken ct) =>
        _context.Set<UserEntity>().IgnoreQueryFilters()
            .AnyAsync(user => user.Id == id && user.TenantId == tenantId && user.IsDeleted, ct);

    public async Task<bool> ExistsByEmailAsync(string? email, int excludeId, CancellationToken ct)
    {
        var normalizedEmail = email?.ToUpperInvariant();
        return await _context.Set<UserEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.NormalizedEmail == normalizedEmail && x.Id != excludeId && !x.IsDeleted, ct);
    }

    public async Task<bool> CreateAsync(UserEntity entity, CancellationToken ct)
    {
        await _context.Set<UserEntity>().AddAsync(entity, ct);
        try
        {
            return await _context.SaveChangesAsync(ct) > 0;
        }
        catch (DbUpdateException exception) when (IsEmailConflict(exception))
        {
            _context.Entry(entity).State = EntityState.Detached;
            throw new UserEmailConflictException(exception);
        }
    }

    public async Task<bool> CreateManyAsync(IReadOnlyCollection<UserEntity> entities, CancellationToken ct)
    {
        await _context.Set<UserEntity>().AddRangeAsync(entities, ct);
        try
        {
            return await _context.SaveChangesAsync(ct) > 0;
        }
        catch (DbUpdateException exception) when (IsEmailConflict(exception))
        {
            foreach (var entity in entities)
                _context.Entry(entity).State = EntityState.Detached;

            throw new UserEmailConflictException(exception);
        }
    }

    private static bool IsEmailConflict(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 } sqlException &&
        sqlException.Message.Contains("'UX_Users_Tenant_NormalizedEmail'", StringComparison.Ordinal);

    public async Task<bool> UpdateAsync(UserEntity entity, CancellationToken ct)
    {
        var entry = _context.Entry(entity);
        if (entry.State == EntityState.Detached)
            throw new InvalidOperationException("User updates require a tracked entity.");

        try
        {
            return await _context.SaveChangesAsync(ct) > 0;
        }
        catch (DbUpdateException exception) when (IsEmailConflict(exception))
        {
            entry.State = EntityState.Detached;
            throw new UserEmailConflictException(exception);
        }
        catch (DbUpdateConcurrencyException exception) when (
            exception.Entries.Count == 1 && ReferenceEquals(exception.Entries[0].Entity, entity))
        {
            entry.State = EntityState.Detached;
            return false;
        }
    }

    public Task<bool> DeleteAsync(UserEntity entity, CancellationToken ct) => UpdateAsync(entity, ct);
}
