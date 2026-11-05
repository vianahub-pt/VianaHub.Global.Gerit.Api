using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IEquipmentTypeDomainService
{
    Task<EquipmentTypeEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<EquipmentTypeEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<EquipmentTypeEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> CreateAsync(EquipmentTypeEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(EquipmentTypeEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(EquipmentTypeEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(EquipmentTypeEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(EquipmentTypeEntity entity, CancellationToken ct);
}
