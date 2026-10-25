using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class VehicleDataRepository : IVehicleDataRepository
{
    private readonly GeritDbContext _context;

    public VehicleDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<VehicleEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<VehicleEntity>()
            .AsNoTracking()
            .Include($"{nameof(VehicleEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(VehicleEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<VehicleEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<VehicleEntity>()
            .AsNoTracking()
            .Include($"{nameof(VehicleEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(VehicleEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .Where(x => !x.IsDeleted).OrderBy(x => x.Plate).ToListAsync(ct);
    }

    public async Task<ListPage<VehicleEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<VehicleEntity>()
            .AsNoTracking()
            .Include($"{nameof(VehicleEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(VehicleEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var rawSearch = request.Search.Trim();
            var searchLower = rawSearch.ToLower();

            // Se for um número válido, permite busca exata por ano
            if (int.TryParse(rawSearch, out var searchYear))
            {
                query = query.Where(x => (x.Plate != null && EF.Functions.Like(x.Plate.ToLower(), $"%{searchLower}%")) ||
                                         (x.Brand != null && EF.Functions.Like(x.Brand.ToLower(), $"%{searchLower}%")) ||
                                         x.Year == searchYear ||
                                         (x.Model != null && EF.Functions.Like(x.Model.ToLower(), $"%{searchLower}%")) ||
                                         (x.Color != null && EF.Functions.Like(x.Color.ToLower(), $"%{searchLower}%")) ||
                                         (x.FuelType != null && EF.Functions.Like(x.FuelType.ToLower(), $"%{searchLower}%")));
            }
            else
            {
                query = query.Where(x => (x.Plate != null && EF.Functions.Like(x.Plate.ToLower(), $"%{searchLower}%")) ||
                                         (x.Brand != null && EF.Functions.Like(x.Brand.ToLower(), $"%{searchLower}%")) ||
                                         (x.Model != null && EF.Functions.Like(x.Model.ToLower(), $"%{searchLower}%")) ||
                                         (x.Color != null && EF.Functions.Like(x.Color.ToLower(), $"%{searchLower}%")) ||
                                         (x.FuelType != null && EF.Functions.Like(x.FuelType.ToLower(), $"%{searchLower}%")));
            }
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        var count = await query.CountAsync(ct);
        var orderedQuery = CreateSort.ApplyOrdering(query, request);
        var pageNumber = request.PageNumber ?? 1;
        var pageSize = request.PageSize ?? Paging.MinPageSize();

        var result = await orderedQuery.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new ListPage<VehicleEntity>
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
        return await _context.Set<VehicleEntity>().AsNoTracking().AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByPlateAsync(int tenantId, string? plate, CancellationToken ct)
    {
        return await _context.Set<VehicleEntity>().AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.Plate == plate && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(VehicleEntity entity, CancellationToken ct)
    {
        await _context.Set<VehicleEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(VehicleEntity entity, CancellationToken ct)
    {
        _context.Set<VehicleEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
