using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IAddressTypeDomainService
{
    Task<AddressTypeEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<AddressTypeEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<AddressTypeEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);

    Task<bool> CreateAsync(AddressTypeEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(AddressTypeEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(AddressTypeEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(AddressTypeEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(AddressTypeEntity entity, CancellationToken ct);
}
