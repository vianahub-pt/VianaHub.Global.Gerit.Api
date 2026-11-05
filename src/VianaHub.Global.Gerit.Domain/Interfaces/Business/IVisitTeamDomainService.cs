using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IVisitTeamDomainService
{
    Task<VisitTeamEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<VisitTeamEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<VisitTeamEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int tenantId, int interventionId, int teamId, CancellationToken ct);

    Task<bool> CreateAsync(VisitTeamEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(VisitTeamEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(VisitTeamEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(VisitTeamEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(VisitTeamEntity entity, CancellationToken ct);
}
