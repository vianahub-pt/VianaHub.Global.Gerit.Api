using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class EquipmentDataRepository : IEquipmentDataRepository
{
    private readonly GeritDbContext _context;

    public EquipmentDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<EquipmentEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<EquipmentEntity>()
            .AsNoTracking()
            .Include(x => x.EquipmentType)
            .Include($"{nameof(EquipmentEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(EquipmentEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<EquipmentEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<EquipmentEntity>()
            .AsNoTracking()
            .Include(x => x.EquipmentType)
            .Include($"{nameof(EquipmentEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(EquipmentEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .Where(x => !x.IsDeleted).OrderBy(x => x.Name).ToListAsync(ct);
    }

    public async Task<ListPage<EquipmentEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<EquipmentEntity>()
            .AsNoTracking()
            .Include(x => x.EquipmentType)
            .Include($"{nameof(EquipmentEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Include($"{nameof(EquipmentEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.StatusDomain)}.{nameof(StatusDomainEntity.Translations)}")
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => (x.Name != null && EF.Functions.Like(x.Name.ToLower(), $"%{search}%")) ||
                                     (x.SerialNumber != null && EF.Functions.Like(x.SerialNumber.ToLower(), $"%{search}%")) ||
                                     (x.EquipmentType != null && x.EquipmentType.Name != null && EF.Functions.Like(x.EquipmentType.Name.ToLower(), $"%{search}%")) ||
                                     (x.StatusDefinition != null && x.StatusDefinition.Code != null && EF.Functions.Like(x.StatusDefinition.Code.ToLower(), $"%{search}%")));
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

        return new ListPage<EquipmentEntity>
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
        return await _context.Set<EquipmentEntity>().AsNoTracking().AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByNameAsync(int tenantId, string? name, CancellationToken ct)
    {
        return await _context.Set<EquipmentEntity>().AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.Name == name && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(EquipmentEntity entity, CancellationToken ct)
    {
        await _context.Set<EquipmentEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(EquipmentEntity entity, CancellationToken ct)
    {
        _context.Set<EquipmentEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
