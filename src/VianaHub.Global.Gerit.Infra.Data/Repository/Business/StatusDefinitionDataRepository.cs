using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class StatusDefinitionDataRepository : IStatusDefinitionDataRepository
{
    private readonly GeritDbContext _context;

    // Consulta a tradução diretamente: o índice único também reserva nomes de pais removidos.
    public Task<bool> ExistsByTranslationAndDomainAsync(int statusDomainId, string? languageCode, string? name, CancellationToken ct) =>
        _context.Set<StatusDefinitionTranslationsEntity>().AnyAsync(x => x.StatusDomainId == statusDomainId && x.LanguageCode == languageCode && x.Name == name, ct);

    public StatusDefinitionDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<StatusDefinitionEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<StatusDefinitionEntity>()
            .AsNoTracking()
            .Include(x => x.StatusDomain)
                .ThenInclude(sd => sd.Translations)
            .Include(x => x.Translations)
            .Include(x => x.Tenant)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(ct);
    }

    public async Task<StatusDefinitionEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<StatusDefinitionEntity>()
            .AsNoTracking()
            .Include(x => x.StatusDomain)
                .ThenInclude(sd => sd.Translations)
            .Include(x => x.Translations)
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<ListPage<StatusDefinitionEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<StatusDefinitionEntity>()
            .AsNoTracking()
            .Include(x => x.StatusDomain)
                .ThenInclude(sd => sd.Translations)
            .Include(x => x.Translations)
            .Include(x => x.Tenant)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                (x.Code != null && EF.Functions.Like(x.Code.ToLower(), $"%{search}%")));
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

        return new ListPage<StatusDefinitionEntity>
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
        return await _context.Set<StatusDefinitionEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByCodeAndDomainAsync(int statusDomainId, string? code, CancellationToken ct)
    {
        return await _context.Set<StatusDefinitionEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.StatusDomainId == statusDomainId && x.Code == code && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(StatusDefinitionEntity entity, CancellationToken ct)
    {
        await _context.Set<StatusDefinitionEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(StatusDefinitionEntity entity, CancellationToken ct)
    {
        _context.Set<StatusDefinitionEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
