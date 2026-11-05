using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade que representa uma Definição de Status por tenant e domínio (ex: VisitStatus.Pending, EquipmentStatus.Active).
/// Entidade tenant-scoped com FK para StatusDomainEntity. Não é Aggregate Root.
/// </summary>
public class StatusDefinitionEntity : Entity
{
    public int TenantId { get; private set; }
    public int StatusDomainId { get; private set; }
    public string? Code { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public TenantEntity Tenant { get; private set; } = null!;
    public StatusDomainEntity StatusDomain { get; private set; } = null!;
    public ICollection<StatusDefinitionTranslationsEntity> Translations { get; private set; } = new List<StatusDefinitionTranslationsEntity>();

    // Construtor protegido para o EF Core
    protected StatusDefinitionEntity() { }

    /// <summary>
    /// Construtor para criação de uma nova Definição de Status
    /// </summary>
    public StatusDefinitionEntity(int tenantId, int statusDomainId, string? code, int displayOrder, bool isSystem, int createdBy)
    {
        TenantId = tenantId;
        StatusDomainId = statusDomainId;
        Code = code;
        DisplayOrder = displayOrder;
        IsSystem = isSystem;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        Translations = new List<StatusDefinitionTranslationsEntity>();
    }

    public void Update(int displayOrder, bool isSystem, int modifiedBy)
    {

        DisplayOrder = displayOrder;
        IsSystem = isSystem;
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
