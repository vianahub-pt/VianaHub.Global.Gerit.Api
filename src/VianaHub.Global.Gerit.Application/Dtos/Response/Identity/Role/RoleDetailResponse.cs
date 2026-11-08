namespace VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Role;

/// <summary>
/// DTO de resposta detalhada para Role — inclui campos de auditoria.
/// Classe independente — não herda de RoleResponse.
/// </summary>
public class RoleDetailResponse
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
