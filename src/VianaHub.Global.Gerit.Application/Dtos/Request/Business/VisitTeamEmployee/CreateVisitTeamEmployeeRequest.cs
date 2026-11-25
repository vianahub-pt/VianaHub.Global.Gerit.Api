namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeamEmployee;

public record CreateVisitTeamEmployeeRequest
{
    public int EmployeeId { get; init; }
    public int VisitTeamFunctionId { get; init; }
    public bool IsLeader { get; init; }
    public DateTime StartDateTime { get; init; }
    public bool? IsActive { get; set; }
}
