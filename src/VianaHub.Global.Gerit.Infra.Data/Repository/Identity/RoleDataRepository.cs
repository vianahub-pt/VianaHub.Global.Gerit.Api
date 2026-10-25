using VianaHub.Global.Gerit.Domain.Helpers;
using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Identity;

public class RoleDataRepository : IRoleDataRepository
{
    private readonly GeritDbContext _context;
    private readonly ILogger<RoleDataRepository> _logger;

    public RoleDataRepository(GeritDbContext context, ILogger<RoleDataRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RoleEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<RoleEntity>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<RoleEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<RoleEntity>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    public async Task<ListPage<RoleEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<RoleEntity>().AsNoTracking().Where(x => !x.IsDeleted);

        // ?? Filtro de busca
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

        return new ListPage<RoleEntity>
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
        return await _context.Set<RoleEntity>().AsNoTracking().AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByNameAsync(int tenantId, string? name, CancellationToken ct)
    {
        return await _context.Set<RoleEntity>().AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.Name == name && !x.IsDeleted, ct);
    }

    public async Task<bool> AddAsync(RoleEntity entity, CancellationToken ct)
    {
        await _context.Set<RoleEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(RoleEntity entity, CancellationToken ct)
    {
        _context.Set<RoleEntity>().Update(entity);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    // Diagnostic helper: lê o SESSION_CONTEXT na conexão atual e registra no logger.
    public async Task LogSessionContextAsync(CancellationToken ct)
    {
        try
        {
            var conn = _context.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT SESSION_CONTEXT(N'TenantId') AS TenantId, SESSION_CONTEXT(N'IsSuperAdmin') AS IsSuperAdmin";

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                var tenant = reader.IsDBNull(0) ? null : reader.GetValue(0);
                var isSuper = reader.IsDBNull(1) ? null : reader.GetValue(1);
                _logger.LogDebug("[RLS][Diagnostic] Current SESSION_CONTEXT - TenantId={Tenant}, IsSuperAdmin={IsSuper}", tenant, isSuper);
            }
            else
            {
                _logger.LogDebug("[RLS][Diagnostic] SESSION_CONTEXT query returned no rows");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[RLS][Diagnostic] Failed to read SESSION_CONTEXT | Exception: {ExceptionDetails}", LogValueSanitizer.ExceptionDetails(ex));
        }
    }
}
