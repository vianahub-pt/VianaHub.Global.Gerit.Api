namespace VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientDocument;

public class ClientDocumentResponse
{
    public int Id { get; set; }
    public int DocumentTypeId { get; set; }
    public string? DocumentType { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string IssuingCountryCode { get; set; } = string.Empty;
    public DateOnly? IssuedAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}
