namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.Client;

/// <summary>
/// Item de upload em massa para Client
/// </summary>
public class BulkUploadClientItem
{
    public byte PartyTypeId { get; set; }
    public int AcquisitionSourceTypeId { get; set; }
    public string? UrlImage { get; set; }
    public string? Note { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int StatusDefinitionId { get; set; }
    public int StatusDomainId { get; set; }
}
