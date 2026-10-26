using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class PartyTypeTranslationDataRepository : IPartyTypeTranslationDataRepository
{
    private readonly GeritDbContext _context;

    public PartyTypeTranslationDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<PartyTypeTranslationsEntity?> GetByIdAsync(byte partyTypeId, string languageCode, CancellationToken ct)
    {
        return await _context.Set<PartyTypeTranslationsEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PartyTypeId == partyTypeId && x.LanguageCode == languageCode, ct);
    }

    public async Task<IEnumerable<PartyTypeTranslationsEntity>> GetByPartyTypeIdAsync(byte partyTypeId, CancellationToken ct)
    {
        return await _context.Set<PartyTypeTranslationsEntity>()
            .AsNoTracking()
            .Where(x => x.PartyTypeId == partyTypeId)
            .OrderBy(x => x.LanguageCode)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsByPartyTypeAndLanguageAsync(byte partyTypeId, string? languageCode, CancellationToken ct)
    {
        return await _context.Set<PartyTypeTranslationsEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.PartyTypeId == partyTypeId && x.LanguageCode == languageCode, ct);
    }

    public async Task<bool> ExistsByLanguageAndNameAsync(string? languageCode, string? name, CancellationToken ct)
    {
        return await _context.Set<PartyTypeTranslationsEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.LanguageCode == languageCode && x.Name == name, ct);
    }

    public async Task<bool> AddAsync(PartyTypeTranslationsEntity entity, CancellationToken ct)
    {
        await _context.Set<PartyTypeTranslationsEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(PartyTypeTranslationsEntity entity, CancellationToken ct)
    {
        _context.Set<PartyTypeTranslationsEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
