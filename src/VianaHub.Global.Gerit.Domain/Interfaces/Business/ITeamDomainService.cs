using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface ITeamDomainService
{
    Task<TeamEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<TeamEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<TeamEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> CreateAsync(TeamEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(TeamEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(TeamEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(TeamEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(TeamEntity entity, CancellationToken ct);
}
