using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class FileTypeDataRepository : IFileTypeDataRepository
{
    public Task<bool> ExistsByCodeAsync(string? code, CancellationToken ct) => _context.Set<FileTypeEntity>().AnyAsync(x => x.Code == code, ct);

    private readonly GeritDbContext _context;

    public FileTypeDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FileTypeEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<FileTypeEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.MimeType)
            .ToListAsync(ct);
    }

    public async Task<FileTypeEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<FileTypeEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<ListPage<FileTypeEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<FileTypeEntity>()
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                (x.MimeType != null && EF.Functions.Like(x.MimeType.ToLower(), $"%{search}%")) ||
                (x.Extension != null && EF.Functions.Like(x.Extension.ToLower(), $"%{search}%")));
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

        return new ListPage<FileTypeEntity>
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
        return await _context.Set<FileTypeEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByMimeTypeAsync(string? mimeType, CancellationToken ct)
    {
        return await _context.Set<FileTypeEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.MimeType == mimeType && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(FileTypeEntity entity, CancellationToken ct)
    {
        await _context.Set<FileTypeEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(FileTypeEntity entity, CancellationToken ct)
    {
        _context.Set<FileTypeEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
