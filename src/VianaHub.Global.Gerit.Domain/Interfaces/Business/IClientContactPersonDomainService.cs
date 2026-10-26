using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

/// <summary>
/// Interface de Domain Service para ClientContact
/// </summary>
public interface IClientContactPersonDomainService
{
    Task<ClientContactPersonsEntity?> GetByIdAsync(int clientId, int id, CancellationToken ct);
    Task<IEnumerable<ClientContactPersonsEntity>> GetAllAsync(int clientId, CancellationToken ct);
    Task<ListPage<ClientContactPersonsEntity>> GetPagedAsync(int clientId, PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByClientAndEmailAsync(int clientId, string name, string email, CancellationToken ct);

    Task<bool> CreateAsync(ClientContactPersonsEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(ClientContactPersonsEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(ClientContactPersonsEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(ClientContactPersonsEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(ClientContactPersonsEntity entity, CancellationToken ct);
}
