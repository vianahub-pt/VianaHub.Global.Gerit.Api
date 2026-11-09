using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Exceptions;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Identity;

public class UserRoleDataRepository : IUserRoleDataRepository
{
    private readonly GeritDbContext _context;

    public UserRoleDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<IList<UserRolesEntity>> GetAllAsync(int tenantId, CancellationToken ct)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.User)
            .Include(x => x.Role)
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.UserId)
            .ThenBy(x => x.RoleId)
            .ToListAsync(ct);
    }
    public async Task<UserRolesEntity?> GetByIdAsync(int tenantId, int userId, int roleId, CancellationToken ct)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.User)
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId &&
                                      x.UserId == userId &&
                                      x.RoleId == roleId, ct);
    }
    public async Task<IList<UserRolesEntity>> GetByUserAsync(int tenantId, int userId, CancellationToken ct)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.User)
            .Include(x => x.Role)
            .Where(x => x.TenantId == tenantId && x.UserId == userId)
            .OrderBy(x => x.RoleId)
            .ToListAsync(ct);
    }
    public async Task<IList<UserRolesEntity>> GetByRoleAsync(int tenantId, int roleId, CancellationToken ct)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.User)
            .Include(x => x.Role)
            .Where(x => x.TenantId == tenantId && x.RoleId == roleId)
            .OrderBy(x => x.UserId)
            .ToListAsync(ct);
    }
    public async Task<bool> ExistsAsync(int tenantId, int userId, int roleId, CancellationToken ct)
    {
        return await _context.UserRoles.AnyAsync(x => x.TenantId == tenantId && x.UserId == userId && x.RoleId == roleId, ct);
    }

    public async Task<bool> CreateAsync(UserRolesEntity entity, CancellationToken ct)
    {
        return await CreateManyAsync([entity], ct);
    }

    public async Task<bool> CreateManyAsync(IReadOnlyCollection<UserRolesEntity> entities, CancellationToken ct)
    {
        if (entities.Count == 0)
            return false;

        await using var transaction = _context.Database.IsRelational() &&
            _context.Database.CurrentTransaction is null
            ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct)
            : null;
        var tenantId = entities.First().TenantId;
        var users = entities.Select(entity => entity.UserId).Distinct().ToArray();
        var roles = entities.Select(entity => entity.RoleId).Distinct().ToArray();
        if (entities.Any(entity => entity.TenantId != tenantId) ||
            await _context.Set<UserEntity>().CountAsync(
                user => user.TenantId == tenantId && !user.IsDeleted && users.Contains(user.Id), ct) != users.Length ||
            await _context.Set<RoleEntity>().CountAsync(
                role => role.TenantId == tenantId && role.IsActive && !role.IsDeleted && roles.Contains(role.Id), ct) != roles.Length)
        {
            return false;
        }

        _context.UserRoles.AddRange(entities);
        try
        {
            var saved = await _context.SaveChangesAsync(ct);
            if (transaction is not null)
                await transaction.CommitAsync(ct);
            return saved > 0;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 } sqlException &&
            sqlException.Message.Contains("'UQ_UserRoles'", StringComparison.Ordinal))
        {
            Detach(entities);
            throw new UserRoleConflictException(exception);
        }
        catch
        {
            Detach(entities);
            throw;
        }
    }

    private void Detach(IEnumerable<UserRolesEntity> entities)
    {
        foreach (var entity in entities)
            _context.Entry(entity).State = EntityState.Detached;
    }
    public async Task<bool> DeleteAsync(int tenantId, int userId, int roleId, CancellationToken ct)
    {
        var entity = await _context.UserRoles.FirstOrDefaultAsync(x => x.TenantId == tenantId &&
                                                                       x.UserId == userId &&
                                                                       x.RoleId == roleId, ct);
        if (entity is null) return false;
        _context.UserRoles.Remove(entity);
        try
        {
            return await _context.SaveChangesAsync(ct) > 0;
        }
        catch (DbUpdateConcurrencyException exception) when (
            exception.Entries.Count == 1 && ReferenceEquals(exception.Entries[0].Entity, entity))
        {
            _context.Entry(entity).State = EntityState.Detached;
            return false;
        }
    }
}
