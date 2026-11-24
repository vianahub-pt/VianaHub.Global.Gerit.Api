namespace VianaHub.Global.Gerit.Application.Dtos.Request.Billing.Tenant;

public class CreateTenantRequest
{
    public byte PartyTypeId { get; set; }
    public int AcquisitionSourceTypeId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? UrlImage { get; set; }
    public string? Note { get; set; }
    public bool? IsActive { get; set; }
}
