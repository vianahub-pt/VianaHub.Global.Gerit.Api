using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

public class EmployeeDocumentsEntity : Entity
{
    public int TenantId { get; private set; }
    public int EmployeeId { get; private set; }
    public int DocumentTypeId { get; private set; }
    public string DocumentNumber { get; private set; } = string.Empty;
    public string IssuingCountryCode { get; private set; } = string.Empty;
    public DateOnly? IssuedAt { get; private set; }
    public DateOnly? ExpiresAt { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    public TenantEntity Tenant { get; private set; } = null!;
    public EmployeeEntity Employee { get; private set; } = null!;
    public DocumentTypeEntity DocumentType { get; private set; } = null!;

    protected EmployeeDocumentsEntity() { }

    public EmployeeDocumentsEntity(int tenantId, int employeeId, int documentTypeId, string documentNumber,
        string issuingCountryCode, DateOnly? issuedAt, DateOnly? expiresAt, bool isPrimary, int createdBy)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        DocumentTypeId = documentTypeId;
        DocumentNumber = documentNumber;
        IssuingCountryCode = issuingCountryCode;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        IsPrimary = isPrimary;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string documentNumber, string issuingCountryCode, DateOnly? issuedAt, DateOnly? expiresAt, bool isPrimary, int modifiedBy)
    {
        DocumentNumber = documentNumber;
        IssuingCountryCode = issuingCountryCode;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        IsPrimary = isPrimary;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void SetPrimary(bool isPrimary, int modifiedBy)
    {
        IsPrimary = isPrimary;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Activate(int modifiedBy)
    {
        IsActive = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Deactivate(int modifiedBy)
    {
        IsPrimary = false;
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Delete(int modifiedBy)
    {
        IsPrimary = false;
        IsActive = false;
        IsDeleted = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }
}
