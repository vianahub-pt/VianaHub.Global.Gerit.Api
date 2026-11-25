namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeamEmployee;

public record UpdateVisitTeamEmployeeRequest
{
    public int VisitTeamFunctionId { get; init; }
    public bool IsLeader { get; init; }
    public DateTime StartDateTime { get; init; }
    public DateTime? EndDateTime { get; init; }
    public bool? IsActive { get; set; }
}
