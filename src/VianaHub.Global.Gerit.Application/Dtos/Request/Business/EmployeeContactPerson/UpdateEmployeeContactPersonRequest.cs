namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.EmployeeContact;

/// <summary>
/// DTO para atualiza��o de EmployeeContact
/// </summary>
public class UpdateEmployeeContactPersonRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? CellPhoneNumber { get; set; }
    public bool IsCellPhoneWhatsapp { get; set; }
    public bool IsPrimary { get; set; }
    public bool? IsActive { get; set; }
}
