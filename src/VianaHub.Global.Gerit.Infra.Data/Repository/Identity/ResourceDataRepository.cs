using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Identity;

public class ResourceDataRepository : IResourceDataRepository
{
    private readonly GeritDbContext _context;

    public ResourceDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<ResourceEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<ResourceEntity>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<ResourceEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<ResourceEntity>()
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    public async Task<ListPage<ResourceEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<ResourceEntity>().AsNoTracking().Where(x => !x.IsDeleted);

        // 🔹 Filtro de busca
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();

            query = query.Where(x =>
                (x.Name != null && EF.Functions.Like(x.Name.ToLower(), $"%{search}%"))
                || (x.Description != null && EF.Functions.Like(x.Description.ToLower(), $"%{search}%"))
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

        return new ListPage<ResourceEntity>
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
        return await _context.Set<ResourceEntity>().AsNoTracking().AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByNameAsync(string? name, CancellationToken ct)
    {
        return await _context.Set<ResourceEntity>().AsNoTracking().AnyAsync(x => x.Name == name && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(ResourceEntity entity, CancellationToken ct)
    {
        await _context.Set<ResourceEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(ResourceEntity entity, CancellationToken ct)
    {
        _context.Set<ResourceEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
