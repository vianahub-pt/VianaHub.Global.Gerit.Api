namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientDocument;

public class CreateClientDocumentRequest
{
    public int DocumentTypeId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string IssuingCountryCode { get; set; } = string.Empty;
    public DateOnly? IssuedAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }
    public bool IsPrimary { get; set; }
}
