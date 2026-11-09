using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VianaHub.Global.Gerit.Domain.Exceptions;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Identity;

public class RolePermissionDataRepository : IRolePermissionDataRepository
{
    private readonly GeritDbContext _context;

    public RolePermissionDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<IList<RolePermissionsEntity>> GetAllAsync(int tenantId, CancellationToken ct)
    {
        return await _context.RolePermissions
            .AsNoTracking()
            .Include(x => x.Role)
            .Include(x => x.Resource)
            .Include(x => x.Action)
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.RoleId)
            .ThenBy(x => x.ResourceId)
            .ThenBy(x => x.ActionId)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }
    public async Task<RolePermissionsEntity?> GetByIdAsync(int tenantId, int roleId, int resourceId, int actionId, CancellationToken ct)
    {
        return await _context.RolePermissions
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.Role)
            .Include(x => x.Resource)
            .Include(x => x.Action)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId &&
                                      x.RoleId == roleId &&
                                      x.ResourceId == resourceId &&
                                      x.ActionId == actionId, ct);
    }
    public async Task<IList<RolePermissionsEntity>> GetByRoleAsync(int roleId, int tenantId, CancellationToken ct)
    {
        return await _context.RolePermissions
            .AsNoTracking()
            .Include(x => x.Role)
            .Include(x => x.Resource)
            .Include(x => x.Action)
            .Where(x => x.RoleId == roleId && x.TenantId == tenantId)
            .OrderBy(x => x.ResourceId)
            .ThenBy(x => x.ActionId)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }
    public async Task<IList<RolePermissionsEntity>> GetByResourceAsync(int resourceId, int tenantId, CancellationToken ct)
    {
        return await _context.RolePermissions
            .AsNoTracking()
            .Include(x => x.Role)
            .Include(x => x.Action)
            .Include(x => x.Resource)
            .Where(x => x.ResourceId == resourceId && x.TenantId == tenantId)
            .OrderBy(x => x.RoleId)
            .ThenBy(x => x.ActionId)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }
    public async Task<bool> ExistsAsync(int tenantId, int roleId, int resourceId, int actionId, CancellationToken ct)
    {
        return await _context.RolePermissions
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.RoleId == roleId && x.ResourceId == resourceId && x.ActionId == actionId, ct);
    }

    public async Task<bool> CreateAsync(RolePermissionsEntity entity, CancellationToken ct)
    {
        await _context.RolePermissions.AddAsync(entity, ct);
        try
        {
            return await _context.SaveChangesAsync(ct) > 0;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new RolePermissionConflictException(ex);
        }
    }

    public async Task<bool> CreateManyAsync(IReadOnlyCollection<RolePermissionsEntity> entities, CancellationToken ct)
    {
        if (entities.Count == 0)
            return false;

        IDbContextTransaction? transaction = null;
        try
        {
            if (_context.Database.IsRelational())
                transaction = await _context.Database.BeginTransactionAsync(ct);

            await _context.RolePermissions.AddRangeAsync(entities, ct);
            var saved = await _context.SaveChangesAsync(ct);
            if (transaction is not null)
                await transaction.CommitAsync(ct);

            return saved == entities.Count;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            if (transaction is not null)
                await transaction.RollbackAsync(CancellationToken.None);

            throw new RolePermissionConflictException(ex);
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }
    public async Task<bool> DeleteAsync(int tenantId, int roleId, int resourceId, int actionId, CancellationToken ct)
    {
        var entity = await _context.RolePermissions
            .FirstOrDefaultAsync(x => x.TenantId == tenantId &&
                                      x.RoleId == roleId &&
                                      x.ResourceId == resourceId &&
                                      x.ActionId == actionId, ct);

        if (entity is null) return false;
        _context.RolePermissions.Remove(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException?.Message.Contains("UQ_RolePermissions", StringComparison.OrdinalIgnoreCase) == true;
}
