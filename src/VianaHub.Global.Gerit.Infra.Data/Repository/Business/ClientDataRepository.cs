using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public partial class ClientDataRepository : IClientDataRepository
{
    private readonly GeritDbContext _context;

    public ClientDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ClientEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Set<ClientEntity>()
            .AsNoTracking()
            .Include($"{nameof(ClientEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Where(x => !x.IsDeleted)
            .ToListAsync(ct);
    }

    public async Task<ClientEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<ClientEntity>()
            .AsNoTracking()
            .Include($"{nameof(ClientEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<IEnumerable<ClientEntity>> GetAllAsync(int tenantId, CancellationToken ct)
    {
        return await _context.Set<ClientEntity>()
            .AsNoTracking()
            .Include($"{nameof(ClientEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ListPage<ClientEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<ClientEntity>()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Contacts)
            .Include($"{nameof(ClientEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Contacts.Any(c =>
                    (c.Name != null && EF.Functions.Like(c.Name.ToLower(), $"%{search}%")) ||
                    (c.Email != null && EF.Functions.Like(c.Email.ToLower(), $"%{search}%"))));
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

        return new ListPage<ClientEntity>
        {
            Items = result,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = (int)Math.Ceiling((double)count / pageSize)
        };
    }

    public async Task<ListPage<ClientEntity>> GetPagedAsync(int tenantId, PagedFilter request, CancellationToken ct)
    {
        var query = _context.Set<ClientEntity>()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Contacts)
            .Include($"{nameof(ClientEntity.StatusDefinition)}.{nameof(StatusDefinitionEntity.Translations)}")
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Contacts.Any(c =>
                    (c.Name != null && EF.Functions.Like(c.Name.ToLower(), $"%{search}%")) ||
                    (c.Email != null && EF.Functions.Like(c.Email.ToLower(), $"%{search}%"))));
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

        return new ListPage<ClientEntity>
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
        return await _context.Set<ClientEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByIdAsync(int tenantId, int clientId, CancellationToken ct)
    {
        return await _context.Set<ClientEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == clientId && !x.IsDeleted, ct);
    }

    public async Task<bool> ExistsByCompanyRegistrationAsync(int tenantId, string? companyRegistrationNumber, int? excludeClientId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(companyRegistrationNumber))
        {
            return false;
        }

        var query = _context.Set<ClientEntity>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.CompanyRegistrationNumber == companyRegistrationNumber && !x.IsDeleted);

        if (excludeClientId.HasValue)
        {
            query = query.Where(x => x.Id != excludeClientId.Value);
        }

        return await query.AnyAsync(ct);
    }

    public async Task<bool> AddAsync(ClientEntity entity, CancellationToken ct)
    {
        await _context.Set<ClientEntity>().AddAsync(entity, ct);
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(ClientEntity entity, CancellationToken ct)
    {
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
