namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientContact;

/// <summary>
/// Request para atualiza��o de ClientContact
/// </summary>
public class UpdateClientContactPersonRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? CellPhoneNumber { get; set; }
    public bool IsWhatsapp { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public bool IsCellPhoneWhatsapp { get; set; }
    public bool IsPrimary { get; set; }
    public bool? IsActive { get; set; }
}
