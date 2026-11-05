namespace VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientDocument;

public class ClientDocumentDetailResponse
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ClientId { get; set; }
    public string? Client { get; set; }
    public int DocumentTypeId { get; set; }
    public string? DocumentType { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string IssuingCountryCode { get; set; } = string.Empty;
    public DateOnly? IssuedAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
