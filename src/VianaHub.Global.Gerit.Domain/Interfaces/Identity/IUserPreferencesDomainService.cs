using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Identity;

public interface IUserPreferencesDomainService
{
    Task<UserPreferencesEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<UserPreferencesEntity?> GetByUserAsync(int tenantId, int userId, CancellationToken ct);
    Task<IEnumerable<UserPreferencesEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<UserPreferencesEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByUserAsync(int tenantId, int userId, CancellationToken ct);

    Task<bool> CreateAsync(UserPreferencesEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(UserPreferencesEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(UserPreferencesEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(UserPreferencesEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(UserPreferencesEntity entity, CancellationToken ct);
}
