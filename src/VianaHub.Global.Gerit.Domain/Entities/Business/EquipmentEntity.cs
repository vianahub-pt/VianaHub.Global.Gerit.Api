using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade que representa um Equipamento
/// </summary>
public class EquipmentEntity : Entity
{
    public int TenantId { get; private set; }
    public int EquipmentTypeId { get; set; }
    public int StatusDefinitionId { get; set; }
    public int StatusDomainId { get; set; }
    public string? Name { get; private set; }
    public string? SerialNumber { get; private set; }
    public string? UrlImage { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public TenantEntity? Tenant { get; private set; }
    public EquipmentTypeEntity? EquipmentType { get; private set; }
    public StatusDefinitionEntity? StatusDefinition { get; private set; }

    // Construtor protegido para o EF Core
    protected EquipmentEntity() { }

    /// <summary>
    /// Construtor para criação de um novo Equipamento
    /// </summary>
    public EquipmentEntity(int tenantId, int equipmentTypeId, int statusDefinitionId, int statusDomainId, string? name, string? serialNumber, string? urlImage, int createdBy)
    {
        TenantId = tenantId;
        EquipmentTypeId = equipmentTypeId;
        StatusDefinitionId = statusDefinitionId;
        StatusDomainId = statusDomainId;
        Name = name;
        SerialNumber = serialNumber;
        UrlImage = urlImage;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(int equipmentTypeId, int statusDefinitionId, int statusDomainId, string? name, string? serialNumber, string? urlImage, int modifiedBy)
    {
        EquipmentTypeId = equipmentTypeId;
        StatusDefinitionId = statusDefinitionId;
        StatusDomainId = statusDomainId;
        Name = name;
        SerialNumber = serialNumber;
        UrlImage = urlImage;
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
