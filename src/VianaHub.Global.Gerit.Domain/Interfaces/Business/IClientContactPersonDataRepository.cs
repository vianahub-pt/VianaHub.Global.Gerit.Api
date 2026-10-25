using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

/// <summary>
/// Interface de Data Repository para ClientContact
/// </summary>
public interface IClientContactPersonDataRepository
{
    Task<ClientContactPersonsEntity?> GetByIdAsync(int clientId, int id, CancellationToken ct);
    Task<IEnumerable<ClientContactPersonsEntity>> GetAllAsync(int clientId, CancellationToken ct);
    Task<ListPage<ClientContactPersonsEntity>> GetPagedAsync(int clientId, PagedFilter filter, CancellationToken ct);
    Task<bool> ExistsByClientAndEmailAsync(int clientId, string? name, string? email, CancellationToken ct);
    Task<bool> AddAsync(ClientContactPersonsEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(ClientContactPersonsEntity entity, CancellationToken ct);
}
