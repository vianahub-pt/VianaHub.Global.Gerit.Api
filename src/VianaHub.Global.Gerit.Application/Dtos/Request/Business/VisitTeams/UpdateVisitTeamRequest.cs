namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeams;

public class UpdateVisitTeamRequest
{
    public int VisitId { get; set; }
    public int TeamId { get; set; }
    public bool? IsActive { get; set; }
}
