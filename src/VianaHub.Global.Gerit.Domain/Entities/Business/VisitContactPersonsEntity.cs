using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade que representa um contato da Interven��o
/// </summary>
public class VisitContactPersonsEntity : Entity
{
    public int TenantId { get; private set; }
    public int VisitId { get; private set; }
    public string? Name { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? JobTitle { get; private set; }
    public string? Department { get; private set; }
    public string? CellPhoneNumber { get; private set; }
    public bool IsCellPhoneWhatsapp { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public TenantEntity? Tenant { get; private set; }
    public VisitEntity? Visit { get; private set; }

    // Construtor protegido para o EF Core
    protected VisitContactPersonsEntity() { }

    /// <summary>
    /// Construtor para cria��o de um novo contato da Interven��o
    /// </summary>
    public VisitContactPersonsEntity(int tenantId, int interventionId, string? name, string? email, string? phone,
        string? jobTitle, string? department, string? cellPhoneNumber, bool isCellPhoneWhatsapp,
        bool isPrimary, int createdBy)
    {
        TenantId = tenantId;
        VisitId = interventionId;
        Name = name;
        Email = email;
        Phone = phone;
        JobTitle = jobTitle;
        Department = department;
        CellPhoneNumber = cellPhoneNumber;
        IsCellPhoneWhatsapp = isCellPhoneWhatsapp;
        IsPrimary = isPrimary;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string? name, string? email, string? phone,
        string? jobTitle, string? department, string? cellPhoneNumber, bool isCellPhoneWhatsapp,
        bool isPrimary, int modifiedBy)
    {
        Name = name;
        Email = email;
        Phone = phone;
        JobTitle = jobTitle;
        Department = department;
        CellPhoneNumber = cellPhoneNumber;
        IsCellPhoneWhatsapp = isCellPhoneWhatsapp;
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
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Delete(int modifiedBy)
    {
        IsDeleted = true;
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }
}
