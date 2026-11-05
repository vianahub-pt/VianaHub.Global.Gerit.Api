using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade que representa os dados fiscais do Funcionário
/// Tabela: dbo.EmployeeFiscalData
/// </summary>
public class EmployeeFiscalDataEntity : Entity
{
    public int TenantId { get; private set; }
    public int EmployeeId { get; private set; }

    public string? TaxNumber { get; private set; } = null!;
    public string? VatNumber { get; private set; }
    public string? FiscalCountry { get; private set; } = "PT";
    public bool IsVatRegistered { get; private set; }
    public string? IBAN { get; private set; }
    public string? FiscalEmail { get; private set; }

    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public EmployeeEntity Employee { get; private set; } = null!;
    public TenantEntity Tenant { get; private set; } = null!;

    // Construtor protegido para o EF Core
    protected EmployeeFiscalDataEntity() { }

    /// <summary>
    /// Construtor para criação de novos dados fiscais do Funcionário
    /// </summary>
    public EmployeeFiscalDataEntity(int tenantId, int employeeId, string? taxNumber, string? vatNumber,
        string? fiscalCountry, bool isVatRegistered, string? iban, string? fiscalEmail, int createdBy)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;

        TaxNumber = taxNumber;
        VatNumber = vatNumber;
        FiscalCountry = fiscalCountry;
        IsVatRegistered = isVatRegistered;
        IBAN = iban;
        FiscalEmail = fiscalEmail;

        IsActive = true;
        IsDeleted = false;

        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string? taxNumber, string? vatNumber, string? fiscalCountry, bool isVatRegistered,
        string? iban, string? fiscalEmail, int modifiedBy)
    {
        TaxNumber = taxNumber;
        VatNumber = vatNumber;
        FiscalCountry = fiscalCountry;
        IsVatRegistered = isVatRegistered;
        IBAN = iban;
        FiscalEmail = fiscalEmail;

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
        IsActive = false;
        IsDeleted = true;

        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }
}
