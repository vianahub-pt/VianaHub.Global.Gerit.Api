namespace VianaHub.Global.Gerit.Application.Dtos.Response.Identity.RolePermission;

/// <summary>
/// DTO de resposta detalhada para RolePermission.
/// Classe independente — não herda de RolePermissionResponse.
/// </summary>
public class RolePermissionDetailResponse
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int ResourceId { get; set; }
    public int ActionId { get; set; }
    public string? RoleName { get; set; }
    public string? ResourceName { get; set; }
    public string? ActionName { get; set; }
}
