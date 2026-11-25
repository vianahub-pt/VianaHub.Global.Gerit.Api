namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeams;

public class CreateVisitTeamRequest
{
    public int VisitId { get; set; }
    public int TeamId { get; set; }
    public bool? IsActive { get; set; }
}
