using System.Text.Json.Serialization;

namespace VianaHub.Global.Gerit.Application.Dtos.Response.Identity.User;

/// <summary>
/// DTO de resposta detalhada para User — inclui campos de auditoria.
/// Classe independente — não herda de UserResponse.
/// </summary>
public class UserDetailResponse
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
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? UrlImage { get; set; }
    public bool IsDeleted { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public int? ModifiedBy { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public DateTime? ModifiedAt { get; set; }
}
