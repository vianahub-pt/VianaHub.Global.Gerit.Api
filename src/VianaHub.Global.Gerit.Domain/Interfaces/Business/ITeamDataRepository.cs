using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface ITeamDataRepository
{
    Task<TeamEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<TeamEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<TeamEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByNameAsync(int tenantId, string? name, CancellationToken ct);
    Task<bool> AddAsync(TeamEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(TeamEntity entity, CancellationToken ct);
}
