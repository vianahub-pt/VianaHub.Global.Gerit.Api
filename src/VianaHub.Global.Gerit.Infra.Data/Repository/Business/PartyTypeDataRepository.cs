using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class PartyTypeDataRepository : IPartyTypeDataRepository
{
    public Task<bool> ExistsByTranslationAsync(string? languageCode, string? name, CancellationToken ct) => _context.Set<PartyTypeTranslationsEntity>().AnyAsync(x => x.LanguageCode == languageCode && x.Name == name, ct);

    private readonly GeritDbContext _context;

    public PartyTypeDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PartyTypeEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<PartyTypeEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Code)
            .ToListAsync(ct);
    }

    public async Task<PartyTypeEntity?> GetByIdAsync(byte id, CancellationToken ct)
    {
        return await _context.Set<PartyTypeEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<ListPage<PartyTypeEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<PartyTypeEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                (x.Code != null && EF.Functions.Like(x.Code.ToLower(), $"%{search}%"))
            );
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        var count = await query.CountAsync(ct);

        // Garantir que o sort default é "Code" em vez de "Id"/"CreatedAt".
        // PartyTypeEntity usa 'new byte Id' que sombreia Entity.Id (int),
        // o que causaria AmbiguousMatchException ao ordenar por "Id".
        if (string.IsNullOrWhiteSpace(request.SortBy) || string.Equals(request.SortBy, "CreatedAt", StringComparison.OrdinalIgnoreCase))
        {
            request.SortBy = "Code";
        }
        var orderedQuery = CreateSort.ApplyOrdering(query, request);
        var pageNumber = request.PageNumber ?? 1;
        var pageSize = request.PageSize ?? Paging.MinPageSize();

        var result = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new ListPage<PartyTypeEntity>
        {
            Items = result,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = (int)Math.Ceiling((double)count / pageSize)
        };
    }

    public async Task<bool> ExistsByIdAsync(byte id, CancellationToken ct)
    {
        return await _context.Set<PartyTypeEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByCodeAsync(string? code, CancellationToken ct)
    {
        return await _context.Set<PartyTypeEntity>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(x => x.Code == code, ct);
    }

    public async Task<bool> AddAsync(PartyTypeEntity entity, CancellationToken ct)
    {
        await _context.Set<PartyTypeEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(PartyTypeEntity entity, CancellationToken ct)
    {
        _context.Set<PartyTypeEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
