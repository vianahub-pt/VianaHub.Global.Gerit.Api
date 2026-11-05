using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IEquipmentDomainService
{
    Task<EquipmentEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<EquipmentEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<EquipmentEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> CreateAsync(EquipmentEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(EquipmentEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(EquipmentEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(EquipmentEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(EquipmentEntity entity, CancellationToken ct);
}
