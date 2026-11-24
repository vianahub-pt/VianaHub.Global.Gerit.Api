using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

public class EmployeeTeamEntity : Entity
{
    public int TenantId { get; private set; }
    public int TeamId { get; private set; }
    public int EmployeeId { get; private set; }
    public bool IsLeader { get; private set; }
    public DateTime StartDateTime { get; private set; }
    public DateTime? EndDateTime { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation
    public TenantEntity Tenant { get; private set; } = null!;
    public TeamEntity Team { get; private set; } = null!;
    public EmployeeEntity Employee { get; private set; } = null!;

    protected EmployeeTeamEntity() { }

    public EmployeeTeamEntity(int tenantId, int teamId, int employeeId, bool isLeader, DateTime startDateTime, int createdBy, bool? isActive = null)
    {
        TenantId = tenantId;
        TeamId = teamId;
        EmployeeId = employeeId;
        IsLeader = isLeader;
        StartDateTime = startDateTime;
        IsActive = isActive ?? true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(int teamId, int employeeId, bool isLeader, DateTime startDateTime, DateTime? endDateTime, int modifiedBy, bool? isActive = null)
    {
        TeamId = teamId;
        EmployeeId = employeeId;
        IsLeader = isLeader;
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
        if (isActive.HasValue) IsActive = isActive.Value;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void SetEndDateTime(DateTime endDateTime, int modifiedBy)
    {
        if (endDateTime < StartDateTime)
            throw new InvalidOperationException("A data de término não pode ser anterior à data de início.");

        EndDateTime = endDateTime;
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
