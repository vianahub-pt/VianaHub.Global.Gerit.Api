using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IVehicleDomainService
{
    Task<VehicleEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<VehicleEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<VehicleEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> CreateAsync(VehicleEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(VehicleEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(VehicleEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(VehicleEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(VehicleEntity entity, CancellationToken ct);
}
