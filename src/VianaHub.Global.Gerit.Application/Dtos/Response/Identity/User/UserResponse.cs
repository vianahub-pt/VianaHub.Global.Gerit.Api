using System.Text.Json.Serialization;

namespace VianaHub.Global.Gerit.Application.Dtos.Response.Identity.User;

public class UserResponse
{
    public int Id { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? Name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? Email { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? PhoneNumber { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public DateTime? LastAccessAt { get; set; }
    public bool IsActive { get; set; }
}
