using VianaHub.Global.Gerit.Domain.Base;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade que representa um Domínio de Status (ex: VisitStatus, EquipmentStatus).
/// Lookup global sem TenantId. Não é Aggregate Root.
/// </summary>
public class StatusDomainEntity : Entity
{
    public string? Code { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public ICollection<StatusDomainTranslationsEntity> Translations { get; private set; } = new List<StatusDomainTranslationsEntity>();

    // Construtor protegido para o EF Core
    protected StatusDomainEntity() { }

    /// <summary>
    /// Construtor para criação de um novo Domínio de Status
    /// </summary>
    public StatusDomainEntity(string? code, int createdBy)
    {
        Code = code;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        Translations = new List<StatusDomainTranslationsEntity>();
    }

    public void MarkModified(int modifiedBy)
    {
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
