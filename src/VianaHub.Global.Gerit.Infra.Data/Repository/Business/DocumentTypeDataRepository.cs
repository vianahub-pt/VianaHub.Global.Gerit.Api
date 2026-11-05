using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class DocumentTypeDataRepository : IDocumentTypeDataRepository
{
    private readonly GeritDbContext _context;

    // Consulta a tradução diretamente: o índice único também reserva nomes de pais removidos.
    public Task<bool> ExistsByTranslationAsync(string? languageCode, string? name, CancellationToken ct) =>
        _context.Set<DocumentTypeTranslationsEntity>().AnyAsync(x => x.LanguageCode == languageCode && x.Name == name, ct);

    public DocumentTypeDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DocumentTypeEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<DocumentTypeEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Code)
            .ToListAsync(ct);
    }

    public async Task<DocumentTypeEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<DocumentTypeEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<ListPage<DocumentTypeEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<DocumentTypeEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
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

        return new ListPage<DocumentTypeEntity>
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
        return await _context.Set<DocumentTypeEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByCodeAsync(string? code, CancellationToken ct)
    {
        return await _context.Set<DocumentTypeEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Code == code && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(DocumentTypeEntity entity, CancellationToken ct)
    {
        await _context.Set<DocumentTypeEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(DocumentTypeEntity entity, CancellationToken ct)
    {
        _context.Set<DocumentTypeEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
