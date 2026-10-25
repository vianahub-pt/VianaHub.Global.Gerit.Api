using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade que representa um Documento do Client (ex: BI, Passaporte, NIF do cliente, etc.).
/// Tabela: dbo.ClientDocuments
/// </summary>
public class ClientDocumentsEntity : Entity
{
    public int TenantId { get; private set; }
    public int ClientId { get; private set; }
    public int DocumentTypeId { get; private set; }

    public string? DocumentNumber { get; private set; } = null!;
    public string? IssuingCountryCode { get; private set; } = "PT";
    public DateTime? IssuedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsPrimary { get; private set; }

    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public ClientEntity Client { get; private set; } = null!;
    public TenantEntity Tenant { get; private set; } = null!;
    public DocumentTypeEntity DocumentType { get; private set; } = null!;

    // Construtor protegido para o EF Core
    protected ClientDocumentsEntity() { }

    /// <summary>
    /// Construtor para criação de um novo Documento do Client
    /// </summary>
    public ClientDocumentsEntity(int tenantId, int clientId, int documentTypeId, string? documentNumber,
        string? issuingCountryCode, DateTime? issuedAt, DateTime? expiresAt, bool isPrimary, int createdBy)
    {
        TenantId = tenantId;
        ClientId = clientId;
        DocumentTypeId = documentTypeId;

        DocumentNumber = documentNumber;
        IssuingCountryCode = issuingCountryCode ?? "PT";
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        IsPrimary = isPrimary;

        IsActive = true;
        IsDeleted = false;

        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string? documentNumber, string? issuingCountryCode,
        DateTime? issuedAt, DateTime? expiresAt, bool isPrimary, int modifiedBy)
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
