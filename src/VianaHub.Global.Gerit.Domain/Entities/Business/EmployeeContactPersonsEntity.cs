using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade que representa um contato do Funcionário
/// </summary>
public class EmployeeContactPersonsEntity : Entity
{
    public int TenantId { get; private set; }
    public int EmployeeId { get; private set; }
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
    public EmployeeEntity? Employee { get; private set; }

    // Construtor protegido para o EF Core
    protected EmployeeContactPersonsEntity() { }

    /// <summary>
    /// Construtor para criação de um novo contato do Funcionário
    /// </summary>
    public EmployeeContactPersonsEntity(int tenantId, int employeeId, string? name, string? email,
        string? phone, string? jobTitle, string? department, string? cellPhoneNumber, bool isCellPhoneWhatsapp, bool isPrimary, int createdBy)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
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

    public void UpdateContactInfo(string? name, string? email, string? phone, string? jobTitle, string? department, string? cellPhoneNumber, bool isCellPhoneWhatsapp, int modifiedBy)
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

    public void SetAsPrimary(int modifiedBy)
    {
        IsPrimary = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void RemoveAsPrimary(int modifiedBy)
    {
        IsPrimary = false;
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
