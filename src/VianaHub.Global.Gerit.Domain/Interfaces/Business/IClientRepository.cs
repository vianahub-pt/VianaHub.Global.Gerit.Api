using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

/// <summary>
/// Contrato principal de persistência do agregado Client.
/// </summary>
public interface IClientRepository
{
    Task<ClientEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<ClientEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<ClientEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> AddAsync(ClientEntity entity, CancellationToken ct);
    Task<bool> AddManyAsync(IReadOnlyCollection<ClientEntity> entities, CancellationToken ct);
    Task<bool> UpdateAsync(ClientEntity entity, CancellationToken ct);
}
