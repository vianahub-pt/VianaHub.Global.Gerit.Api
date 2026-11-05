namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientDocument;

public sealed class BulkUploadClientDocumentItem
{
    public int DocumentTypeId { get; init; }
    public string DocumentNumber { get; init; } = string.Empty;
    public string IssuingCountryCode { get; init; } = string.Empty;
    public DateOnly? IssuedAt { get; init; }
    public DateOnly? ExpiresAt { get; init; }
    public bool IsPrimary { get; init; }
    public int LineNumber { get; init; }
}
