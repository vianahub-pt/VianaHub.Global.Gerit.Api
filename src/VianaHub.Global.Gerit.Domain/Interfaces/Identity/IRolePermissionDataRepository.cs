using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Identity;

public interface IRolePermissionDataRepository
{
    Task<RolePermissionsEntity?> GetByIdAsync(int tenantId, int roleId, int resourceId, int actionId, CancellationToken ct);
    Task<IList<RolePermissionsEntity>> GetByRoleAsync(int roleId, int tenantId, CancellationToken ct);
    Task<IList<RolePermissionsEntity>> GetByResourceAsync(int resourceId, int tenantId, CancellationToken ct);
    Task<IList<RolePermissionsEntity>> GetAllAsync(int tenantId, CancellationToken ct);
    Task<bool> ExistsAsync(int tenantId, int roleId, int resourceId, int actionId, CancellationToken ct);
    Task<bool> CreateAsync(RolePermissionsEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(int tenantId, int roleId, int resourceId, int actionId, CancellationToken ct);
}
