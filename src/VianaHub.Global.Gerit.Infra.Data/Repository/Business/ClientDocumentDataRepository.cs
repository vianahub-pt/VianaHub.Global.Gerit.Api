using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Domain.Exceptions;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class ClientDocumentDataRepository : IClientDocumentDataRepository
{
    private readonly GeritDbContext _context;

    public ClientDocumentDataRepository(GeritDbContext context) => _context = context;

    public Task<ClientDocumentsEntity?> GetByIdAsync(int tenantId, int clientId, int id, CancellationToken ct) =>
        Query(tenantId, clientId).Include(x => x.DocumentType).ThenInclude(x => x.Translations)
            .Include(x => x.Client).FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IEnumerable<ClientDocumentsEntity>> GetAllAsync(int tenantId, int clientId, CancellationToken ct) =>
        await Query(tenantId, clientId).Include(x => x.DocumentType).ThenInclude(x => x.Translations)
            .OrderBy(x => x.Id).ToListAsync(ct);

    public async Task<ListPage<ClientDocumentsEntity>> GetPagedAsync(int tenantId, int clientId, PagedFilter filter, CancellationToken ct)
    {
        var query = Query(tenantId, clientId).Include(x => x.DocumentType).ThenInclude(x => x.Translations).AsQueryable();
        if (filter.IsActive.HasValue) query = query.Where(x => x.IsActive == filter.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => x.DocumentNumber.Contains(search) || x.IssuingCountryCode.Contains(search));
        }
        var total = await query.CountAsync(ct);
        var page = filter.PageNumber ?? 1;
        var size = filter.PageSize ?? 10;
        var items = await query.OrderBy(x => x.Id).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new ListPage<ClientDocumentsEntity>(items, page, size, total, (int)Math.Ceiling(total / (double)size));
    }

    public Task<bool> ExistsDuplicateAsync(int tenantId, int documentTypeId, string issuingCountryCode, string documentNumber, int? excludeId, CancellationToken ct) =>
        _context.ClientDocuments.AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.DocumentTypeId == documentTypeId &&
            x.IssuingCountryCode == issuingCountryCode && x.DocumentNumber == documentNumber && !x.IsDeleted &&
            (!excludeId.HasValue || x.Id != excludeId.Value), ct);

    public Task<ClientDocumentsEntity?> GetPrimaryAsync(int tenantId, int clientId, int? excludeId, CancellationToken ct) =>
        _context.ClientDocuments.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ClientId == clientId &&
            x.IsPrimary && x.IsActive && !x.IsDeleted && (!excludeId.HasValue || x.Id != excludeId.Value), ct);

    public async Task<bool> CreateAsync(ClientDocumentsEntity entity, CancellationToken ct)
    {
        if (entity.IsPrimary)
        {
            var current = await GetPrimaryAsync(entity.TenantId, entity.ClientId, null, ct);
            current?.SetPrimary(false, entity.CreatedBy);
        }
        _context.ClientDocuments.Add(entity);
        try { return await _context.SaveChangesAsync(ct) > 0; }
        catch (DbUpdateException ex) { throw new ClientDocumentConflictException(ex); }
    }

    public async Task<bool> UpdateAsync(ClientDocumentsEntity entity, CancellationToken ct)
    {
        if (entity.IsPrimary)
        {
            var current = await GetPrimaryAsync(entity.TenantId, entity.ClientId, entity.Id, ct);
            current?.SetPrimary(false, entity.ModifiedBy ?? 0);
        }
        _context.ClientDocuments.Update(entity);
        try { return await _context.SaveChangesAsync(ct) > 0; }
        catch (DbUpdateException ex) { throw new ClientDocumentConflictException(ex); }
    }

    private IQueryable<ClientDocumentsEntity> Query(int tenantId, int clientId) =>
        _context.ClientDocuments.AsNoTracking().AsSplitQuery().Where(x => x.TenantId == tenantId && x.ClientId == clientId && !x.IsDeleted);
}
