namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.Client;

/// <summary>
/// Request para atualizacao de Client
/// </summary>
public class UpdateClientRequest
{
    public byte PartyTypeId { get; set; }
    public int AcquisitionSourceTypeId { get; set; }
    public string? UrlImage { get; set; }
    public string? Note { get; set; }

    // Campos unificados
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? CellPhoneNumber { get; set; }
    public bool IsCellPhoneWhatsapp { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? CompanyRegistrationNumber { get; set; }
    public string? EconomicActivityCode { get; set; }
    public int? NumberOfEmployees { get; set; }
    public int StatusDefinitionId { get; set; }
    public int StatusDomainId { get; set; }
}
