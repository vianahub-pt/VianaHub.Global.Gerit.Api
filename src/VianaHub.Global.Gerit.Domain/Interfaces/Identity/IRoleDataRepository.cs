using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Identity;

public interface IRoleDataRepository
{
    Task<RoleEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<RoleEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<RoleEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByNameAsync(int tenantId, string? name, CancellationToken ct);
    Task<bool> AddAsync(RoleEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(RoleEntity entity, CancellationToken ct);
}
