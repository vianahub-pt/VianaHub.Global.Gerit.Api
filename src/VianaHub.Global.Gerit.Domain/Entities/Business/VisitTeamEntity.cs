using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

public class VisitTeamEntity : Entity
{
    public int TenantId { get; private set; }
    public int VisitId { get; private set; }
    public int TeamId { get; private set; }
    public DateTime StartDateTime { get; private set; }
    public DateTime? EndDateTime { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation
    public TenantEntity Tenant { get; private set; } = null!;
    public VisitEntity Visit { get; private set; } = null!;
    public TeamEntity Team { get; private set; } = null!;

    protected VisitTeamEntity() { }

    public VisitTeamEntity(int tenantId, int visitId, int teamId, DateTime startDateTime, int createdBy, bool? isActive = null)
    {
        TenantId = tenantId;
        VisitId = visitId;
        TeamId = teamId;
        StartDateTime = startDateTime;
        IsActive = isActive ?? true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(int visitId, int teamId, DateTime startDateTime, DateTime? endDateTime, int modifiedBy, bool? isActive = null)
    {
        VisitId = visitId;
        TeamId = teamId;
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
        if (isActive.HasValue) IsActive = isActive.Value;
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
