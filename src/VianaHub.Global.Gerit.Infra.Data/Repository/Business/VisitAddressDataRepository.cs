using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

/// <summary>
/// Repositório de dados para VisitAddress
/// </summary>
public class VisitAddressDataRepository : IVisitAddressDataRepository
{
    private readonly GeritDbContext _context;

    public VisitAddressDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<VisitAddressesEntity?> GetByIdAsync(int visitId, int id, CancellationToken ct)
    {
        return await _context.VisitAddresses
            .AsNoTracking()
            .Include(x => x.Visit)
            .Include($"{nameof(VisitAddressesEntity.AddressType)}.{nameof(AddressTypeEntity.Translations)}")
            .FirstOrDefaultAsync(x => x.VisitId == visitId && x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<VisitAddressesEntity>> GetAllAsync(int visitId, CancellationToken ct)
    {
        return await _context.VisitAddresses
            .AsNoTracking()
            .Include(x => x.Visit)
            .Include($"{nameof(VisitAddressesEntity.AddressType)}.{nameof(AddressTypeEntity.Translations)}")
            .Where(x => x.VisitId == visitId && !x.IsDeleted)
            .OrderBy(x => x.VisitId)
            .ThenBy(x => x.City)
            .ToListAsync(ct);
    }

    public async Task<ListPage<VisitAddressesEntity>> GetPagedAsync(int visitId, PagedFilter request, CancellationToken ct)
    {
        var query = _context.VisitAddresses
            .AsNoTracking()
            .Include(x => x.Visit)
            .Include($"{nameof(VisitAddressesEntity.AddressType)}.{nameof(AddressTypeEntity.Translations)}")
            .Where(x => x.VisitId == visitId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(x =>
                (x.Street != null && EF.Functions.Like(x.Street.ToLower(), $"%{searchLower}%")) ||
                (x.City != null && EF.Functions.Like(x.City.ToLower(), $"%{searchLower}%")) ||
                (x.PostalCode != null && EF.Functions.Like(x.PostalCode.ToLower(), $"%{searchLower}%")) ||
                (x.District != null && EF.Functions.Like(x.District.ToLower(), $"%{searchLower}%")) ||
                (x.Neighborhood != null && EF.Functions.Like(x.Neighborhood.ToLower(), $"%{searchLower}%")) ||
                (x.Visit != null && x.Visit.Title != null && EF.Functions.Like(x.Visit.Title.ToLower(), $"%{searchLower}%")) ||
                x.AddressType != null && x.AddressType.Translations.Any(t => (t.Name != null && EF.Functions.Like(t.Name.ToLower(), $"%{searchLower}%"))));
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

        return new ListPage<VisitAddressesEntity>
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
        return await _context.VisitAddresses
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByVisitAndAddressAsync(int tenantId, int interventionId, string? street, string? city, string? postalCode, CancellationToken ct)
    {
        return await _context.VisitAddresses
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId &&
                          x.VisitId == interventionId &&
                          x.Street == street &&
                          x.City == city &&
                          x.PostalCode == postalCode &&
                          !x.IsDeleted, ct);
    }

    public async Task<VisitAddressesEntity?> GetPrimaryAddressByVisitAsync(int interventionId, CancellationToken ct)
    {
        return await _context.VisitAddresses
            .AsNoTracking()
            .Include($"{nameof(VisitAddressesEntity.AddressType)}.{nameof(AddressTypeEntity.Translations)}")
            .Include(x => x.Visit)
            .FirstOrDefaultAsync(x => x.VisitId == interventionId && x.IsPrimary && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(VisitAddressesEntity entity, CancellationToken ct)
    {
        await _context.VisitAddresses.AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(VisitAddressesEntity entity, CancellationToken ct)
    {
        _context.VisitAddresses.Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
