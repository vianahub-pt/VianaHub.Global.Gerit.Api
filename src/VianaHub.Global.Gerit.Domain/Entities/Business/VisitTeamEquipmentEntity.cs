using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

public class VisitTeamEquipmentEntity : Entity
{
    public int TenantId { get; private set; }
    public int VisitTeamId { get; private set; }
    public int EquipmentId { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation
    public TenantEntity? Tenant { get; private set; }
    public VisitTeamEntity? VisitTeam { get; private set; }
    public EquipmentEntity? Equipment { get; private set; }

    protected VisitTeamEquipmentEntity() { }

    public VisitTeamEquipmentEntity(int tenantId, int interventionTeamId, int equipmentId, int createdBy)
    {
        TenantId = tenantId;
        VisitTeamId = interventionTeamId;
        EquipmentId = equipmentId;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(int interventionTeamId, int equipmentId, int modifiedBy)
    {
        VisitTeamId = interventionTeamId;
        EquipmentId = equipmentId;
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
