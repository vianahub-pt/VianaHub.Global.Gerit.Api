using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

/// <summary>
/// Interface de serviço de domínio para VisitAddress
/// </summary>
public interface IVisitAddressDomainService
{
    Task<VisitAddressesEntity?> GetByIdAsync(int visitId, int id, CancellationToken ct);
    Task<IEnumerable<VisitAddressesEntity>> GetAllAsync(int visitId, CancellationToken ct);
    Task<ListPage<VisitAddressesEntity>> GetPagedAsync(int visitId, PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);

    Task<bool> CreateAsync(VisitAddressesEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(VisitAddressesEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(VisitAddressesEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(VisitAddressesEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(VisitAddressesEntity entity, CancellationToken ct);
}
