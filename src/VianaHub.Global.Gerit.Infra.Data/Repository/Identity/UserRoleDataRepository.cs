using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
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
            .Include(x => x.Role)
            .Where(x => x.TenantId == tenantId && x.UserId == userId)
            .ToListAsync(ct);
    }
    public async Task<IList<UserRolesEntity>> GetByRoleAsync(int tenantId, int roleId, CancellationToken ct)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.User)
            .Where(x => x.TenantId == tenantId && x.RoleId == roleId)
            .ToListAsync(ct);
    }
    public async Task<bool> ExistsAsync(int tenantId, int userId, int roleId, CancellationToken ct)
    {
        return await _context.UserRoles.AnyAsync(x => x.TenantId == tenantId && x.UserId == userId && x.RoleId == roleId, ct);
    }

    public async Task<bool> CreateAsync(UserRolesEntity entity, CancellationToken ct)
    {
        await _context.UserRoles.AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }
    public async Task<bool> DeleteAsync(int tenantId, int userId, int roleId, CancellationToken ct)
    {
        var entity = await _context.UserRoles.FirstOrDefaultAsync(x => x.TenantId == tenantId &&
                                                                       x.UserId == userId &&
                                                                       x.RoleId == roleId, ct);
        if (entity is null) return false;
        _context.UserRoles.Remove(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
