namespace VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Action;

/// <summary>
/// DTO de resposta detalhada para Action — inclui campos de auditoria.
/// Classe independente — não herda de ActionResponse.
/// </summary>
public class ActionDetailResponse
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
