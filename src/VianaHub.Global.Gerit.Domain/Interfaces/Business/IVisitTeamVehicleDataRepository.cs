using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IVisitTeamVehicleDataRepository
{
    Task<IEnumerable<VisitTeamVehicleEntity>> GetAllAsync(CancellationToken ct);
    Task<VisitTeamVehicleEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPage<VisitTeamVehicleEntity>> GetPagedAsync(PagedFilter filter, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int tenantId, int interventionTeamId, int vehicleId, CancellationToken ct);
    Task<bool> AddAsync(VisitTeamVehicleEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(VisitTeamVehicleEntity entity, CancellationToken ct);
}
