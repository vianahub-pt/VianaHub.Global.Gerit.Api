using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using System;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class VisitTeamEquipmentDataRepository : IVisitTeamEquipmentDataRepository
{
    private readonly GeritDbContext _context;

    public VisitTeamEquipmentDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<VisitTeamEquipmentEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<VisitTeamEquipmentEntity>()
            .AsNoTracking()
            .Include(x => x.Equipment)
            .Include(x => x.VisitTeam)
            .Include($"{nameof(VisitTeamEquipmentEntity.VisitTeam)}.{nameof(VisitTeamEntity.Visit)}")
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<VisitTeamEquipmentEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<VisitTeamEquipmentEntity>()
            .AsNoTracking()
            .Include(x => x.Equipment)
            .Include(x => x.VisitTeam)
            .Include($"{nameof(VisitTeamEquipmentEntity.VisitTeam)}.{nameof(VisitTeamEntity.Visit)}")
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<ListPage<VisitTeamEquipmentEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<VisitTeamEquipmentEntity>()
            .AsNoTracking()
            .Include(x => x.Equipment)
            .Include(x => x.VisitTeam)
            .Include($"{nameof(VisitTeamEquipmentEntity.VisitTeam)}.{nameof(VisitTeamEntity.Visit)}")
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                (x.Equipment != null && x.Equipment.Name != null && EF.Functions.Like(x.Equipment.Name.ToLower(), $"%{search}%")) ||
                (x.VisitTeam != null && x.VisitTeam.Visit != null && x.VisitTeam.Visit.Title != null && EF.Functions.Like(x.VisitTeam.Visit.Title.ToLower(), $"%{search}%")));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        var count = await query.CountAsync(ct);

        var pageNumber = request.PageNumber ?? 1;
        var pageSize = request.PageSize ?? Paging.MinPageSize();

        var result = await query.OrderByDescending(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new ListPage<VisitTeamEquipmentEntity>
        {
            Items = result,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = (int)Math.Ceiling((double)count / pageSize)
        };
    }

    public async Task<bool> ExistsByIdAsync(int tenantId, int interventionTeamId, int equipmentId, CancellationToken ct)
    {
        return await _context.Set<VisitTeamEquipmentEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.VisitTeamId == interventionTeamId && x.EquipmentId == equipmentId && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(VisitTeamEquipmentEntity entity, CancellationToken ct)
    {
        await _context.Set<VisitTeamEquipmentEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(VisitTeamEquipmentEntity entity, CancellationToken ct)
    {
        _context.Set<VisitTeamEquipmentEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
