using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class EmployeeFiscalDataDataRepository : IEmployeeFiscalDataDataRepository
{
    private readonly GeritDbContext _context;

    public EmployeeFiscalDataDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EmployeeFiscalDataEntity>> GetAllAsync(int employeeId, CancellationToken ct)
    {
        return await _context.EmployeeFiscalData
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Employee)
            .Where(x => x.EmployeeId == employeeId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<EmployeeFiscalDataEntity?> GetByIdAsync(int employeeId, int id, CancellationToken ct = default)
    {
        return await _context.EmployeeFiscalData
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Employee)
            .Where(x => x.EmployeeId == employeeId && x.Id == id && !x.IsDeleted)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ListPage<EmployeeFiscalDataEntity>> GetPagedAsync(int employeeId, PagedFilter filter, CancellationToken ct = default)
    {
        var query = _context.EmployeeFiscalData
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Employee)
            .Where(x => x.EmployeeId == employeeId && !x.IsDeleted);

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

        return new ListPage<EmployeeFiscalDataEntity>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<bool> ExistsByIdAsync(int employeeId, CancellationToken ct = default)
    {
        return await _context.EmployeeFiscalData
            .AsNoTracking()
            .AnyAsync(x => x.EmployeeId == employeeId && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByTaxNumberAsync(int employeeId, string taxNumber, CancellationToken ct = default)
    {
        return await _context.EmployeeFiscalData
            .AsNoTracking()
            .AnyAsync(x => x.EmployeeId == employeeId && x.TaxNumber == taxNumber && !x.IsDeleted, ct);
    }

    public async Task<bool> CreateAsync(EmployeeFiscalDataEntity entity, CancellationToken ct = default)
    {
        await _context.EmployeeFiscalData
            .AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(EmployeeFiscalDataEntity entity, CancellationToken ct = default)
    {
        _context.EmployeeFiscalData
            .Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
