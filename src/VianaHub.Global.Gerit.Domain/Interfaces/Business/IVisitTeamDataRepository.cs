using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Domain.ReadModels;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IVisitTeamDataRepository
{
    Task<IEnumerable<VisitTeamEntity>> GetAllAsync(CancellationToken ct);
    Task<VisitTeamEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPage<VisitTeamEntity>> GetPagedAsync(PagedFilter filter, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int tenantId, int interventionId, int teamId, CancellationToken ct);
    Task<bool> AddAsync(VisitTeamEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(VisitTeamEntity entity, CancellationToken ct);
}
