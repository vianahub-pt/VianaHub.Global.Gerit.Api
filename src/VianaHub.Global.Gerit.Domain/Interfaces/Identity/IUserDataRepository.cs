using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Identity;

public interface IUserDataRepository
{
    Task<IEnumerable<UserEntity>> GetAllAsync(CancellationToken ct);
    Task<UserEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<UserEntity?> GetByEmailAsync(string? email, CancellationToken ct);
    Task<ListPage<UserEntity>> GetPagedAsync(PagedFilter filter, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByEmailAsync(string? email, CancellationToken ct);
    Task<bool> ExistsByEmailAsync(string? email, int excludeId, CancellationToken ct);
    Task<bool> CreateAsync(UserEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(UserEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(UserEntity entity, CancellationToken ct);
}
