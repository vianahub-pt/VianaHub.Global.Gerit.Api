using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Business;

public class EmployeeDocumentDataRepository(GeritDbContext context) : IEmployeeDocumentDataRepository
{
    public Task<EmployeeDocumentsEntity?> GetByIdAsync(int employeeId, int id, CancellationToken ct = default) =>
        context.EmployeeDocuments.AsNoTracking().Include(x => x.Employee).Include(x => x.DocumentType)
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.Id == id && !x.IsDeleted, ct);

    public async Task<IEnumerable<EmployeeDocumentsEntity>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default) =>
        await context.EmployeeDocuments.AsNoTracking().Include(x => x.DocumentType)
            .Where(x => x.EmployeeId == employeeId && !x.IsDeleted).OrderBy(x => x.Id).ToListAsync(ct);

    public Task<bool> ExistsIdentityAsync(int tenantId, int documentTypeId, string issuingCountryCode, string documentNumber, int? excludeId, CancellationToken ct = default) =>
        context.EmployeeDocuments.AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.DocumentTypeId == documentTypeId &&
            x.IssuingCountryCode == issuingCountryCode && x.DocumentNumber == documentNumber && !x.IsDeleted &&
            (!excludeId.HasValue || x.Id != excludeId.Value), ct);

    public Task<bool> ExistsPrimaryAsync(int tenantId, int employeeId, int? excludeId, CancellationToken ct = default) =>
        context.EmployeeDocuments.AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.EmployeeId == employeeId &&
            x.IsPrimary && x.IsActive && !x.IsDeleted && (!excludeId.HasValue || x.Id != excludeId.Value), ct);

    public async Task<bool> AddAsync(EmployeeDocumentsEntity entity, CancellationToken ct = default)
    {
        if (entity.IsPrimary)
        {
            var current = await context.EmployeeDocuments.FirstOrDefaultAsync(x => x.TenantId == entity.TenantId && x.EmployeeId == entity.EmployeeId && x.IsPrimary && x.IsActive && !x.IsDeleted, ct);
            current?.SetPrimary(false, entity.CreatedBy);
        }
        await context.EmployeeDocuments.AddAsync(entity, ct);
        return await context.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> UpdateAsync(EmployeeDocumentsEntity entity, CancellationToken ct = default)
    {
        if (entity.IsPrimary)
        {
            var current = await context.EmployeeDocuments.FirstOrDefaultAsync(x => x.TenantId == entity.TenantId && x.EmployeeId == entity.EmployeeId && x.IsPrimary && x.IsActive && !x.IsDeleted && x.Id != entity.Id, ct);
            current?.SetPrimary(false, entity.ModifiedBy ?? 0);
        }
        context.EmployeeDocuments.Update(entity);
        return await context.SaveChangesAsync(ct) > 0;
    }
}
