namespace VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientContact;

public class ClientContactPersonDetailResponse
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ClientId { get; set; }
    public string? Client { get; set; }

    public string? Name { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? CellPhoneNumber { get; set; }
    public bool IsWhatsapp { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public bool IsCellPhoneWhatsapp { get; set; }
    public string? Email { get; set; } = null!;

    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
