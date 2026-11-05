using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IEquipmentDataRepository
{
    Task<EquipmentEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<EquipmentEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<EquipmentEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByNameAsync(int tenantId, string? name, CancellationToken ct);
    Task<bool> AddAsync(EquipmentEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(EquipmentEntity entity, CancellationToken ct);
}
