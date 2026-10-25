using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class ClientFiscalDataDataRepository : IClientFiscalDataDataRepository
{
    private readonly GeritDbContext _context;

    public ClientFiscalDataDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ClientFiscalDataEntity>> GetAllAsync(int clientId, CancellationToken ct)
    {
        return await _context.ClientFiscalData
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Client)
            .Where(x => x.ClientId == clientId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ClientFiscalDataEntity?> GetByIdAsync(int clientId, int id, CancellationToken ct = default)
    {
        return await _context.ClientFiscalData
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Client)
            .Where(x => x.ClientId == clientId && x.Id == id && !x.IsDeleted)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ListPage<ClientFiscalDataEntity>> GetPagedAsync(int clientId, PagedFilter filter, CancellationToken ct = default)
    {
        var query = _context.ClientFiscalData
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Client)
            .Where(x => x.ClientId == clientId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();

            query = query.Where(x =>
                (x.TaxNumber != null && EF.Functions.Like(x.TaxNumber, $"%{search}%")) ||
                (x.VatNumber != null && EF.Functions.Like(x.VatNumber, $"%{search}%")) ||
                (x.FiscalCountry != null && EF.Functions.Like(x.FiscalCountry, $"%{search}%")) ||
                (x.IBAN != null && EF.Functions.Like(x.IBAN, $"%{search}%")) ||
                (x.FiscalEmail != null && EF.Functions.Like(x.FiscalEmail, $"%{search}%")));
        }

        var totalCount = await query.CountAsync(ct);

        var pageNumber = filter.PageNumber ?? 1;
        var pageSize = filter.PageSize ?? 10;

        var items = await query
            .OrderBy(x => x.TaxNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new ListPage<ClientFiscalDataEntity>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<bool> ExistsByIdAsync(int clientId, CancellationToken ct = default)
    {
        return await _context.ClientFiscalData
            .AsNoTracking()
            .AnyAsync(x => x.ClientId == clientId && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByTaxNumberAsync(int clientId, string taxNumber, CancellationToken ct = default)
    {
        return await _context.ClientFiscalData
            .AsNoTracking()
            .AnyAsync(x => x.ClientId == clientId && x.TaxNumber == taxNumber && !x.IsDeleted, ct);
    }

    public async Task<bool> CreateAsync(ClientFiscalDataEntity entity, CancellationToken ct = default)
    {
        await _context.ClientFiscalData
            .AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(ClientFiscalDataEntity entity, CancellationToken ct = default)
    {
        _context.ClientFiscalData
            .Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
