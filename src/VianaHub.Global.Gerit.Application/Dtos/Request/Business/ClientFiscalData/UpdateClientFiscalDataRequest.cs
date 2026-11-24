namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientFiscalData;

public class UpdateClientFiscalDataRequest
{
    public string? TaxNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? FiscalCountry { get; set; }
    public bool IsVatRegistered { get; set; }
    public string? IBAN { get; set; }
    public string? FiscalEmail { get; set; }
    public bool? IsActive { get; set; }
}
