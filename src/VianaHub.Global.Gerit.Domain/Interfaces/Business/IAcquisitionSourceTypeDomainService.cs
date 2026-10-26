using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IAcquisitionSourceTypeDomainService
{
    Task<AcquisitionSourceTypeEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<AcquisitionSourceTypeEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<AcquisitionSourceTypeEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);

    Task<bool> CreateAsync(AcquisitionSourceTypeEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(AcquisitionSourceTypeEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(AcquisitionSourceTypeEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(AcquisitionSourceTypeEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(AcquisitionSourceTypeEntity entity, CancellationToken ct);
}
