using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using System;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class VisitTeamVehicleDataRepository : IVisitTeamVehicleDataRepository
{
    private readonly GeritDbContext _context;

    public VisitTeamVehicleDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<VisitTeamVehicleEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<VisitTeamVehicleEntity>()
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.VisitTeam)
            .Include($"{nameof(VisitTeamVehicleEntity.VisitTeam)}.{nameof(VisitTeamEntity.Visit)}")
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<VisitTeamVehicleEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<VisitTeamVehicleEntity>()
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.VisitTeam)
            .Include($"{nameof(VisitTeamVehicleEntity.VisitTeam)}.{nameof(VisitTeamEntity.Visit)}")
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<ListPage<VisitTeamVehicleEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<VisitTeamVehicleEntity>()
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.VisitTeam)
            .Include($"{nameof(VisitTeamVehicleEntity.VisitTeam)}.{nameof(VisitTeamEntity.Visit)}")
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                (x.Vehicle != null && x.Vehicle.Plate != null && EF.Functions.Like(x.Vehicle.Plate.ToLower(), $"%{search}%")) ||
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

        return new ListPage<VisitTeamVehicleEntity>
        {
            Items = result,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = (int)Math.Ceiling((double)count / pageSize)
        };
    }

    public async Task<bool> ExistsByIdAsync(int tenantId, int interventionTeamId, int vehicleId, CancellationToken ct)
    {
        return await _context.Set<VisitTeamVehicleEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.VisitTeamId == interventionTeamId && x.VehicleId == vehicleId && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(VisitTeamVehicleEntity entity, CancellationToken ct)
    {
        await _context.Set<VisitTeamVehicleEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(VisitTeamVehicleEntity entity, CancellationToken ct)
    {
        _context.Set<VisitTeamVehicleEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
