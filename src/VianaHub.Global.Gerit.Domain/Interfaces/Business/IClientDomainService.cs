using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

/// <summary>
/// Interface de serviço de domínio para Client
/// </summary>
public interface IClientDomainService
{
    Task<ClientEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<ClientEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<ClientEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);

    Task<bool> CreateAsync(ClientEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(ClientEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(ClientEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(ClientEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(ClientEntity entity, CancellationToken ct);
}
