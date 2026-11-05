using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IVisitTeamEquipmentDomainService
{
    Task<IEnumerable<VisitTeamEquipmentEntity>> GetAllAsync(CancellationToken ct);
    Task<VisitTeamEquipmentEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPage<VisitTeamEquipmentEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int tenantId, int interventionTeamId, int equipmentId, CancellationToken ct);
    Task<bool> CreateAsync(VisitTeamEquipmentEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(VisitTeamEquipmentEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(VisitTeamEquipmentEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(VisitTeamEquipmentEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(VisitTeamEquipmentEntity entity, CancellationToken ct);
}
