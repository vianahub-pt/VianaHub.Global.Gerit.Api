using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IPartyTypeTranslationDataRepository
{
    Task<PartyTypeTranslationsEntity?> GetByIdAsync(byte partyTypeId, string languageCode, CancellationToken ct);
    Task<IEnumerable<PartyTypeTranslationsEntity>> GetByPartyTypeIdAsync(byte partyTypeId, CancellationToken ct);
    Task<bool> ExistsByPartyTypeAndLanguageAsync(byte partyTypeId, string? languageCode, CancellationToken ct);
    Task<bool> ExistsByLanguageAndNameAsync(string? languageCode, string? name, CancellationToken ct);

    Task<bool> AddAsync(PartyTypeTranslationsEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(PartyTypeTranslationsEntity entity, CancellationToken ct);
}
