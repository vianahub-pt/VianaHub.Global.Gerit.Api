using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Identity;

public interface IUserRoleDataRepository
{
    Task<IList<UserRolesEntity>> GetAllAsync(int tenantId, CancellationToken ct);
    Task<UserRolesEntity?> GetByIdAsync(int tenantId, int userId, int roleId, CancellationToken ct);
    Task<IList<UserRolesEntity>> GetByUserAsync(int tenantId, int userId, CancellationToken ct);
    Task<IList<UserRolesEntity>> GetByRoleAsync(int tenantId, int roleId, CancellationToken ct);
    Task<bool> ExistsAsync(int tenantId, int userId, int roleId, CancellationToken ct);

    Task<bool> CreateAsync(UserRolesEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(int tenantId, int userId, int roleId, CancellationToken ct);
}
