namespace VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Resource;

/// <summary>
/// DTO de resposta detalhada para Resource — inclui campos de auditoria.
/// Classe independente — não herda de ResourceResponse.
/// </summary>
public class ResourceDetailResponse
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
