using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Job;
using VianaHub.Global.Gerit.Domain.Interfaces.Job;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Job;

public class JobDefinitionDataRepository : IJobDefinitionDataRepository
{
    private readonly GeritDbContext _context;

    public JobDefinitionDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<JobDefinitionsEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<JobDefinitionsEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<JobDefinitionsEntity?> GetByNameAsync(string jobName, CancellationToken ct)
    {
        var normalized = jobName?.Trim();
        return await _context.Set<JobDefinitionsEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.JobName == normalized && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<JobDefinitionsEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<JobDefinitionsEntity>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.JobCategory)
            .ThenBy(x => x.Priority)
            .ThenBy(x => x.JobName)
            .ToListAsync(ct);
    }

    public async Task<ListPage<JobDefinitionsEntity>> GetPagedAsync(PagedFilter filter, CancellationToken ct)
    {
        // Accept JobPagedFilter or fallback to PagedFilter
        var jobFilter = filter as JobPagedFilter ?? new JobPagedFilter(filter.Search, filter.IsActive, filter?.PageNumber, filter?.PageSize, filter?.SortBy, filter?.SortDirection);

        var query = _context.Set<JobDefinitionsEntity>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(jobFilter.Search))
        {
            var search = jobFilter.Search.Trim().ToLower();
            query = query.Where(x => (x.JobName != null && EF.Functions.Like(x.JobName.ToLower(), $"%{search}%")));
        }

        if (!string.IsNullOrWhiteSpace(jobFilter.Category))
        {
            query = query.Where(x => x.JobCategory == jobFilter.Category);
        }

        if (jobFilter.IsActive.HasValue)
            query = query.Where(x => x.IsActive == jobFilter.IsActive.Value);

        if (jobFilter.IsSystemJob.HasValue)
            query = query.Where(x => x.IsSystemJob == jobFilter.IsSystemJob.Value);

        if (!string.IsNullOrWhiteSpace(jobFilter.Queue))
            query = query.Where(x => x.Queue == jobFilter.Queue);

        var count = await query.CountAsync(ct);

        var ordered = query.OrderBy(x => x.JobCategory).ThenBy(x => x.Priority).ThenBy(x => x.JobName);

        var pageNumber = jobFilter.PageNumber ?? 1;
        var pageSize = jobFilter.PageSize ?? Paging.MinPageSize();

        var data = await ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new ListPage<JobDefinitionsEntity>
        {
            Items = data,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = (int)Math.Ceiling((double)count / pageSize)
        };
    }

    public async Task<bool> ExistsByNameAsync(string? jobName, CancellationToken ct)
    {
        var normalized = jobName?.Trim();
        return await _context.Set<JobDefinitionsEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.JobName == normalized && !x.IsDeleted, ct);
    }

    public async Task<bool> CreateAsync(JobDefinitionsEntity entity, CancellationToken ct)
    {
        await _context.Set<JobDefinitionsEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(JobDefinitionsEntity entity, CancellationToken ct)
    {
        _context.Set<JobDefinitionsEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(JobDefinitionsEntity entity, CancellationToken ct)
    {
        _context.Set<JobDefinitionsEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
