using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class EmployeeAddressDataRepository : IEmployeeAddressDataRepository
{
    private readonly GeritDbContext _context;

    public EmployeeAddressDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeAddressesEntity?> GetByIdAsync(int employeeId, int id, CancellationToken ct)
    {
        return await _context.EmployeeAddresses
            .AsNoTracking()
            .Include($"{nameof(EmployeeAddressesEntity.AddressType)}.{nameof(AddressTypeEntity.Translations)}")
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<EmployeeAddressesEntity>> GetAllAsync(int employeeId, CancellationToken ct)
    {
        return await _context.EmployeeAddresses
            .AsNoTracking()
            .Include($"{nameof(EmployeeAddressesEntity.AddressType)}.{nameof(AddressTypeEntity.Translations)}")
            .Include(x => x.Employee)
            .Where(x => x.EmployeeId == employeeId && !x.IsDeleted)
            .OrderBy(x => x.EmployeeId)
            .ThenBy(x => x.City)
            .ToListAsync(ct);
    }

    public async Task<ListPage<EmployeeAddressesEntity>> GetPagedAsync(int employeeId, PagedFilter request, CancellationToken ct)
    {
        var query = _context.EmployeeAddresses
            .AsNoTracking()
            .Include($"{nameof(EmployeeAddressesEntity.AddressType)}.{nameof(AddressTypeEntity.Translations)}")
            .Include(x => x.Employee)
            .Where(x => x.EmployeeId == employeeId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(x =>
                (x.Street != null && EF.Functions.Like(x.Street.ToLower(), $"%{searchLower}%")) ||
                (x.City != null && EF.Functions.Like(x.City.ToLower(), $"%{searchLower}%")) ||
                (x.PostalCode != null && EF.Functions.Like(x.PostalCode.ToLower(), $"%{searchLower}%")) ||
                (x.District != null && EF.Functions.Like(x.District.ToLower(), $"%{searchLower}%")) ||
                x.AddressType != null && x.AddressType.Translations.Any(t => (t.Name != null && EF.Functions.Like(t.Name.ToLower(), $"%{searchLower}%"))) ||
                (x.Employee != null && x.Employee.Name != null && EF.Functions.Like(x.Employee.Name.ToLower(), $"%{searchLower}%")));
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

        return new ListPage<EmployeeAddressesEntity>
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
        return await _context.EmployeeAddresses
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByEmployeeAndAddressAsync(int tenantId, int EmployeeId, string? street, string? city, string? postalCode, CancellationToken ct)
    {
        return await _context.EmployeeAddresses
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId &&
                          x.EmployeeId == EmployeeId &&
                          x.Street == street &&
                          x.City == city &&
                          x.PostalCode == postalCode &&
                          !x.IsDeleted, ct);
    }

    public async Task<EmployeeAddressesEntity?> GetPrimaryAddressByEmployeeAsync(int EmployeeId, CancellationToken ct)
    {
        return await _context.EmployeeAddresses
            .AsNoTracking()
            .Include($"{nameof(EmployeeAddressesEntity.AddressType)}.{nameof(AddressTypeEntity.Translations)}")
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.EmployeeId == EmployeeId && x.IsPrimary && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(EmployeeAddressesEntity entity, CancellationToken ct)
    {
        await _context.EmployeeAddresses.AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(EmployeeAddressesEntity entity, CancellationToken ct)
    {
        _context.EmployeeAddresses.Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
