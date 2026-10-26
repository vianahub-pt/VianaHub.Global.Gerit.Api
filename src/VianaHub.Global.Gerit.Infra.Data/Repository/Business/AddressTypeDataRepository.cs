using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class AddressTypeDataRepository : IAddressTypeDataRepository
{
    public Task<bool> ExistsByTranslationAsync(string? languageCode, string? name, CancellationToken ct) => _context.Set<AddressTypeTranslationsEntity>().AnyAsync(x => x.LanguageCode == languageCode && x.Name == name, ct);

    public Task<bool> ExistsByCodeAsync(string? code, CancellationToken ct) => _context.Set<AddressTypeEntity>().AnyAsync(x => x.Code == code, ct);

    private readonly GeritDbContext _context;

    public AddressTypeDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AddressTypeEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<AddressTypeEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Code)
            .ToListAsync(ct);
    }
    public async Task<AddressTypeEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<AddressTypeEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }
    public async Task<ListPage<AddressTypeEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<AddressTypeEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Translations.Any(t =>
                    (t.Name != null && EF.Functions.Like(t.Name.ToLower(), $"%{search}%")) ||
                    (t.Description != null && (t.Description != null && EF.Functions.Like(t.Description.ToLower(), $"%{search}%")))
                )
            );
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        var count = await query.CountAsync(ct);
        var orderedQuery = CreateSort.ApplyOrdering(query, request);
        var pageNumber = request.PageNumber ?? 1;
        var pageSize = request.PageSize ?? Paging.MinPageSize();

        var result = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new ListPage<AddressTypeEntity>
        {
            Items = result,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = (int)Math.Ceiling((double)count / pageSize)
        };
    }
    public async Task<bool> ExistsByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<AddressTypeEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }
    public async Task<bool> ExistsByNameAsync(string? name, CancellationToken ct)
    {
        return await _context.Set<AddressTypeEntity>()
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.Translations.Any(t => t.Name == name), ct);
    }

    public async Task<bool> AddAsync(AddressTypeEntity entity, CancellationToken ct)
    {
        await _context.Set<AddressTypeEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }
    public async Task<bool> UpdateAsync(AddressTypeEntity entity, CancellationToken ct)
    {
        _context.Set<AddressTypeEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
