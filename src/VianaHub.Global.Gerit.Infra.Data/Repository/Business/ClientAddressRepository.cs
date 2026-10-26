using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

/// <summary>
/// Reposit�rio de dados para ClientAddress
/// </summary>
public class ClientAddressRepository : IClientAddressDataRepository
{
    private readonly GeritDbContext _context;

    public ClientAddressRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<ClientAddressesEntity?> GetByIdAsync(int clientId, int id, CancellationToken ct)
    {
        return await _context.ClientAddresses
            .AsNoTracking()
            .Include(x => x.Client)
            .Include($"{nameof(ClientAddressesEntity.AddressType)}.{nameof(AddressTypeEntity.Translations)}")
            .Where(x => x.ClientId == clientId && x.Id == id && !x.IsDeleted)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<ClientAddressesEntity>> GetAllAsync(int clientId, CancellationToken ct)
    {
        return await _context.ClientAddresses
            .AsNoTracking()
            .Include(x => x.Client)
            .Include($"{nameof(ClientAddressesEntity.AddressType)}.{nameof(AddressTypeEntity.Translations)}")
            .Where(x => x.Client.Id == clientId && !x.IsDeleted)
            .OrderBy(x => x.ClientId)
            .ThenBy(x => x.IsPrimary ? 0 : 1)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<ListPage<ClientAddressesEntity>> GetPagedAsync(int clientId, PagedFilter request, CancellationToken ct)
    {
        var query = _context.ClientAddresses
            .AsNoTracking()
            .Include(x => x.Client)
            .Include($"{nameof(ClientAddressesEntity.AddressType)}.{nameof(AddressTypeEntity.Translations)}")
            .Where(x => x.Client.Id == clientId && !x.IsDeleted);

        // Aplicar busca
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                (x.Street != null && EF.Functions.Like(x.Street.ToLower(), $"%{search}%")) ||
                (x.City != null && EF.Functions.Like(x.City.ToLower(), $"%{search}%")) ||
                (x.PostalCode != null && EF.Functions.Like(x.PostalCode.ToLower(), $"%{search}%")) ||
                (x.District != null && EF.Functions.Like(x.District.ToLower(), $"%{search}%")) ||
                (x.Neighborhood != null && EF.Functions.Like(x.Neighborhood.ToLower(), $"%{search}%")) ||
                (x.Client != null && x.Client.Name != null && EF.Functions.Like(x.Client.Name.ToLower(), $"%{search}%")) ||
                x.AddressType != null && x.AddressType.Translations.Any(t => (t.Name != null && EF.Functions.Like(t.Name.ToLower(), $"%{search}%"))));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        // Total de registros
        var count = await query.CountAsync(ct);

        // Aplicar ordena��o
        var orderedQuery = CreateSort.ApplyOrdering(query, request);

        var pageNumber = request.PageNumber ?? 1;
        var pageSize = request.PageSize ?? Paging.MinPageSize();

        // Aplicar pagina��o
        var result = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new ListPage<ClientAddressesEntity>
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
        return await _context.ClientAddresses
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByClientIdAsync(int clientId, string? countryCode, string? street, string? streetNumber, string? neighborhood, string? city, string? district, string? postalCode, CancellationToken ct)
    {
        return await _context.ClientAddresses
            .AsNoTracking()
            .AnyAsync(x => x.Client.Id == clientId &&
                           x.CountryCode == countryCode &&
                           x.Street == street &&
                           x.StreetNumber == streetNumber &&
                           x.Neighborhood == neighborhood &&
                           x.City == city &&
                           x.District == district &&
                           x.PostalCode == postalCode &&
                           !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByClientAndAddressTypeAsync(int clientId, int addressTypeId, CancellationToken ct)
    {
        return await _context.ClientAddresses
            .AsNoTracking()
            .AnyAsync(x => x.ClientId == clientId && x.AddressTypeId == addressTypeId && !x.IsDeleted, ct);
    }

    public async Task<bool> CreateAsync(ClientAddressesEntity entity, CancellationToken ct)
    {
        await _context.ClientAddresses.AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(ClientAddressesEntity entity, CancellationToken ct)
    {
        _context.ClientAddresses.Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(ClientAddressesEntity entity, CancellationToken ct)
    {
        _context.ClientAddresses.Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
