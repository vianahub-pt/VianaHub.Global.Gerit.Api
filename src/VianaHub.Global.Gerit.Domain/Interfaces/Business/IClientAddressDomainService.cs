using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

/// <summary>
/// Interface de servi�o de dom�nio para ClientAddress
/// </summary>
public interface IClientAddressDomainService
{
    Task<ClientAddressesEntity?> GetByIdAsync(int clientId, int id, CancellationToken ct);
    Task<IEnumerable<ClientAddressesEntity>> GetAllAsync(int clientId, CancellationToken ct);
    Task<ListPage<ClientAddressesEntity>> GetPagedAsync(int clientId, PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);

    Task<bool> CreateAsync(ClientAddressesEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(ClientAddressesEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(ClientAddressesEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(ClientAddressesEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(ClientAddressesEntity entity, CancellationToken ct);
}
