namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.Employee;

public class UpdateEmployeeRequest
{
    public int StatusDefinitionId { get; set; }
    public int StatusDomainId { get; set; }
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }
    public string? CellPhoneNumber { get; set; }
    public bool IsCellPhoneWhatsapp { get; set; }
    public string? Email { get; set; }
    public string? ImageUrl { get; set; }
    public bool? IsActive { get; set; }
}
