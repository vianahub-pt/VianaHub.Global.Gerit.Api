using VianaHub.Global.Gerit.Domain.Base;

namespace VianaHub.Global.Gerit.Domain.Entities.Billing;

/// <summary>
/// Entidade que representa um contato do Tenant
/// </summary>
public class TenantContactPersonsEntity : Entity
{
    public int TenantId { get; private set; }
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

    // Navigation Property
    public TenantEntity? Tenant { get; private set; }

    // Construtor protegido para o EF Core
    protected TenantContactPersonsEntity() { }

    /// <summary>
    /// Construtor para criação de um novo contato do Tenant
    /// </summary>
    public TenantContactPersonsEntity(
        int tenantId,
        string? name,
        string? email,
        string? phone,
        string? jobTitle,
        string? department,
        string? cellPhoneNumber,
        bool isCellPhoneWhatsapp,
        bool isPrimary,
        int createdBy)
    {
        TenantId = tenantId;
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

    public void Update(
        string? name,
        string? email,
        string? phone,
        string? jobTitle,
        string? department,
        string? cellPhoneNumber,
        bool isCellPhoneWhatsapp,
        int modifiedBy)
    {
        Name = name;
        Email = email;
        Phone = phone;
        JobTitle = jobTitle;
        Department = department;
        CellPhoneNumber = cellPhoneNumber;
        IsCellPhoneWhatsapp = isCellPhoneWhatsapp;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    public void RemoveAsPrimary()
    {
        IsPrimary = false;
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
