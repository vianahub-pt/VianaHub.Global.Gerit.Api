using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class VisitAttachmentDataRepository : IVisitAttachmentDataRepository
{
    private readonly GeritDbContext _context;

    public VisitAttachmentDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<VisitAttachmentsEntity?> GetByIdAsync(int visitId, int id, CancellationToken ct)
    {
        return await _context.Set<VisitAttachmentsEntity>()
            .AsNoTracking()
            .Include(x => x.FileType)
            .Include(x => x.Visit)
            .FirstOrDefaultAsync(x => x.VisitId == visitId && x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<VisitAttachmentsEntity?> GetByPublicIdAsync(Guid publicId, CancellationToken ct)
    {
        return await _context.Set<VisitAttachmentsEntity>()
            .AsNoTracking()
            .Include(x => x.FileType)
            .FirstOrDefaultAsync(x => x.PublicId == publicId && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<VisitAttachmentsEntity>> GetAllAsync(int visitId, CancellationToken ct)
    {
        return await _context.Set<VisitAttachmentsEntity>()
            .AsNoTracking()
            .Include(x => x.FileType)
            .Where(x => x.VisitId == visitId && !x.IsDeleted)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DisplayOrder)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<VisitAttachmentsEntity>> GetByVisitIdAsync(int visitId, CancellationToken ct)
    {
        return await _context.Set<VisitAttachmentsEntity>()
            .AsNoTracking()
            .Include(x => x.FileType)
            .Where(x => x.VisitId == visitId && !x.IsDeleted)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.FileName)
            .ToListAsync(ct);
    }

    public async Task<VisitAttachmentsEntity?> GetPrimaryByVisitIdAsync(int visitId, CancellationToken ct)
    {
        return await _context.Set<VisitAttachmentsEntity>()
            .AsNoTracking()
            .Include(x => x.FileType)
            .FirstOrDefaultAsync(x => x.VisitId == visitId && x.IsPrimary && x.IsActive && !x.IsDeleted, ct);
    }

    public async Task<ListPage<VisitAttachmentsEntity>> GetPagedAsync(int visitId, PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<VisitAttachmentsEntity>()
            .AsNoTracking()
            .Include(x => x.FileType)
            .Include(x => x.Visit)
            .Where(x => x.VisitId == visitId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => (x.FileName != null && EF.Functions.Like(x.FileName.ToLower(), $"%{search}%")) ||
                                     (x.S3Key != null && EF.Functions.Like(x.S3Key.ToLower(), $"%{search}%")));
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

        return new ListPage<VisitAttachmentsEntity>
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
        return await _context.Set<VisitAttachmentsEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByS3KeyAsync(int tenantId, string? s3Key, CancellationToken ct)
    {
        return await _context.Set<VisitAttachmentsEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.S3Key == s3Key && !x.IsDeleted, ct);
    }

    public async Task<bool> HasPrimaryAttachmentAsync(int visitId, CancellationToken ct)
    {
        return await _context.Set<VisitAttachmentsEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.VisitId == visitId && x.IsPrimary && x.IsActive && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(VisitAttachmentsEntity entity, CancellationToken ct)
    {
        await _context.Set<VisitAttachmentsEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(VisitAttachmentsEntity entity, CancellationToken ct)
    {
        _context.Set<VisitAttachmentsEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<long> GetTotalSizeByVisitIdAsync(int visitId, CancellationToken ct)
    {
        return await _context.Set<VisitAttachmentsEntity>()
            .AsNoTracking()
            .Where(x => x.VisitId == visitId && !x.IsDeleted)
            .SumAsync(x => x.FileSizeBytes, ct);
    }

    public async Task<int> GetCountByVisitIdAsync(int visitId, CancellationToken ct)
    {
        return await _context.Set<VisitAttachmentsEntity>()
            .AsNoTracking()
            .CountAsync(x => x.VisitId == visitId && !x.IsDeleted, ct);
    }
}
