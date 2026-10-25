using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IPartyTypeDataRepository
{
    Task<bool> ExistsByTranslationAsync(string? languageCode, string? name, CancellationToken ct);

    Task<IEnumerable<PartyTypeEntity>> GetAllAsync(CancellationToken ct);
    Task<PartyTypeEntity?> GetByIdAsync(byte id, CancellationToken ct);
    Task<ListPage<PartyTypeEntity>> GetPagedAsync(PagedFilter filter, CancellationToken ct);
    Task<bool> ExistsByIdAsync(byte id, CancellationToken ct);
    Task<bool> ExistsByCodeAsync(string? code, CancellationToken ct);

    Task<bool> AddAsync(PartyTypeEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(PartyTypeEntity entity, CancellationToken ct);
}
