namespace VianaHub.Global.Gerit.Application.Dtos.Response.Identity.UserRole;

public class UserRoleResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string? UserName { get; set; }
    public string? RoleName { get; set; }
}
