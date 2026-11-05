using VianaHub.Global.Gerit.Domain.Base;

namespace VianaHub.Global.Gerit.Domain.Entities.Billing;

/// <summary>
/// Entidade que representa os dados fiscais do Tenant
/// </summary>
public class TenantFiscalDataEntity : Entity
{
    public int TenantId { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? VatNumber { get; private set; }
    public string? IBAN { get; private set; }
    public string? FiscalEmail { get; private set; }
    public string? FiscalCountry { get; private set; }
    public bool IsVatRegistered { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Property
    public TenantEntity? Tenant { get; private set; }

    // Construtor protegido para o EF Core
    protected TenantFiscalDataEntity() { }

    /// <summary>
    /// Construtor para cria��o de novos dados fiscais do Tenant
    /// </summary>
    public TenantFiscalDataEntity(int tenantId, string? taxNumber, string? vatNumber, string? iban, string? fiscalEmail, string? fiscalCountry, bool isVatRegistered, int createdBy)
    {
        TenantId = tenantId;
        TaxNumber = taxNumber;
        VatNumber = vatNumber;
        IBAN = iban;
        FiscalEmail = fiscalEmail;
        FiscalCountry = fiscalCountry;
        IsVatRegistered = isVatRegistered;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateFiscalData(string? taxNumber, string? vatNumber, string? iban, string? fiscalEmail, string? fiscalCountry, bool isVatRegistered, int modifiedBy)
    {
        TaxNumber = taxNumber;
        VatNumber = vatNumber;
        IBAN = iban;
        FiscalEmail = fiscalEmail;
        FiscalCountry = fiscalCountry;
        IsVatRegistered = isVatRegistered;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void RegisterForVAT(int modifiedBy)
    {
        IsVatRegistered = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void UnregisterFromVAT(int modifiedBy)
    {
        IsVatRegistered = false;
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
