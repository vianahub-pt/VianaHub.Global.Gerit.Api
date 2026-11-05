using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Identity;

public interface IUserDomainService
{
    Task<UserEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<UserEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<UserEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);

    Task<bool> CreateAsync(UserEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(UserEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(UserEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(UserEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(UserEntity entity, CancellationToken ct);
}
