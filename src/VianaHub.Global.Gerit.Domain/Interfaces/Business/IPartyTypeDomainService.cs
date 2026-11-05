using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IPartyTypeDomainService
{
    Task<PartyTypeEntity?> GetByIdAsync(byte id, CancellationToken ct);
    Task<IEnumerable<PartyTypeEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<PartyTypeEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(byte id, CancellationToken ct);

    Task<bool> CreateAsync(PartyTypeEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(PartyTypeEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(PartyTypeEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(PartyTypeEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(PartyTypeEntity entity, CancellationToken ct);
}
