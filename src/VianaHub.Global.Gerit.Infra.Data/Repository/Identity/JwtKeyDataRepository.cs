using VianaHub.Global.Gerit.Domain.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Identity;

public class JwtKeyDataRepository : IJwtKeyDataRepository
{
    private readonly GeritDbContext _context;
    private readonly ILogger<JwtKeyDataRepository> _logger;

    public JwtKeyDataRepository(GeritDbContext context, ILogger<JwtKeyDataRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<JwtKeysEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<JwtKeysEntity>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<JwtKeysEntity?> GetByKeyIdAsync(Guid keyId, CancellationToken ct)
    {
        return await _context.Set<JwtKeysEntity>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.KeyId == keyId && !x.IsDeleted, ct);
    }

    public async Task<JwtKeysEntity?> GetActiveKeyAsync(int tenantId, CancellationToken ct)
    {
        return await _context.Set<JwtKeysEntity>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted && x.RevokedAt == null, ct);
    }

    public async Task<IEnumerable<JwtKeysEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<JwtKeysEntity>().AsNoTracking()
            .Where(x => !x.IsDeleted).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    }

    public async Task<IEnumerable<JwtKeysEntity>> GetByTenantAsync(int tenantId, CancellationToken ct)
    {
        return await _context.Set<JwtKeysEntity>().AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    }

    public async Task<IEnumerable<JwtKeysEntity>> GetByApplicationAsync(int tenantId, CancellationToken ct)
    {
        return await _context.Set<JwtKeysEntity>().AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    }

    public async Task<IEnumerable<JwtKeysEntity>> GetKeysEligibleForRotationAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<JwtKeysEntity>().AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted && x.RevokedAt == null && x.NextRotationAt <= now).ToListAsync(ct);
    }

    public async Task<IEnumerable<JwtKeysEntity>> GetExpiredKeysAsync(int retentionDays, CancellationToken ct)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        return await _context.Set<JwtKeysEntity>().AsNoTracking()
            .Where(x => !x.IsDeleted && !x.IsActive && x.ExpiresAt < cutoffDate).ToListAsync(ct);
    }

    public async Task<ListPage<JwtKeysEntity>> GetPagedAsync(PagedFilter request, int tenantId, CancellationToken ct)
    {
        var query = _context.Set<JwtKeysEntity>().AsNoTracking().Where(x => !x.IsDeleted);

        // 🔹 Filtro de busca
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();

            query = query.Where(x =>
                (x.PlainPrivateKey != null && EF.Functions.Like(x.PlainPrivateKey.ToLower(), $"%{search}%"))
                || (x.PublicKey != null && EF.Functions.Like(x.PublicKey.ToLower(), $"%{search}%"))
            );
        }

        var count = await query.CountAsync(ct);

        var orderedQuery = CreateSort.ApplyOrdering(query, request);

        var pageNumber = request.PageNumber ?? 1;
        var pageSize = request.PageSize ?? Paging.MinPageSize();

        var result = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new ListPage<JwtKeysEntity>
        {
            Items = result,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = (int)Math.Ceiling((double)count / pageSize)
        };
    }

    public async Task<bool> HasActiveKeyAsync(int tenantId, CancellationToken ct)
    {
        return await _context.Set<JwtKeysEntity>().AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted && x.RevokedAt == null, ct);
    }

    public async Task<bool> AddAsync(JwtKeysEntity entity, CancellationToken ct)
    {
        try
        {
            await _context.Set<JwtKeysEntity>().AddAsync(entity, ct);
            var result = await _context.SaveChangesAsync(ct);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError("?? [CreateAsync] Add JwtKeysEntity | falha ao criar chave JwtKey | Exception: {ExceptionDetails}", LogValueSanitizer.ExceptionDetails(ex));
            throw new Exception("Falha ao criar chave JwtKey", ex);
        }
    }

    public async Task<bool> UpdateAsync(JwtKeysEntity entity, CancellationToken ct)
    {
        _context.Set<JwtKeysEntity>().Update(entity);
        var result = await _context.SaveChangesAsync(ct);
        return result > 0;
    }

    public async Task<bool> DeleteAsync(JwtKeysEntity entity, CancellationToken ct)
    {
        _context.Set<JwtKeysEntity>().Update(entity);
        var result = await _context.SaveChangesAsync(ct);
        return result > 0;
    }

    public async Task<int> BulkUpdateTelemetryAsync(List<(int Id, long UsageCount, DateTime? LastUsedAt, long ValidationCount, DateTime? LastValidatedAt)> updates, CancellationToken ct)
    {
        var ids = updates.Select(x => x.Id).ToList();
        var entities = await _context.Set<JwtKeysEntity>().Where(x => ids.Contains(x.Id)).ToListAsync(ct);

        foreach (var entity in entities)
        {
            var update = updates.First(x => x.Id == entity.Id);

            // Atualizar telemetria (usando reflection ou método específico)
            entity.GetType().GetProperty(nameof(entity.UsageCount))?.SetValue(entity, update.UsageCount);
            entity.GetType().GetProperty(nameof(entity.LastUsedAt))?.SetValue(entity, update.LastUsedAt);
            entity.GetType().GetProperty(nameof(entity.ValidationCount))?.SetValue(entity, update.ValidationCount);
            entity.GetType().GetProperty(nameof(entity.LastValidatedAt))?.SetValue(entity, update.LastValidatedAt);
        }

        return await _context.SaveChangesAsync(ct);

    }
}
