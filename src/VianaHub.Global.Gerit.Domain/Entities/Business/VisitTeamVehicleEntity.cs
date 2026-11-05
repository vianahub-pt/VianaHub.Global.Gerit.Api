using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

public class VisitTeamVehicleEntity : Entity
{
    public int TenantId { get; private set; }
    public int VisitTeamId { get; private set; }
    public int VehicleId { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation
    public TenantEntity? Tenant { get; private set; }
    public VisitTeamEntity? VisitTeam { get; private set; }
    public VehicleEntity? Vehicle { get; private set; }

    protected VisitTeamVehicleEntity() { }

    public VisitTeamVehicleEntity(int tenantId, int interventionTeamId, int vehicleId, int createdBy)
    {
        TenantId = tenantId;
        VisitTeamId = interventionTeamId;
        VehicleId = vehicleId;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(int interventionTeamId, int vehicleId, int modifiedBy)
    {
        VisitTeamId = interventionTeamId;
        VehicleId = vehicleId;
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
