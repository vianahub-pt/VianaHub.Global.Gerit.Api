namespace VianaHub.Global.Gerit.Application.Dtos.Response.Identity.UserRole;

/// <summary>
/// DTO com os identificadores e nomes da associação.
/// </summary>
public class UserRoleDetailResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string? UserName { get; set; }
    public string? RoleName { get; set; }
}
