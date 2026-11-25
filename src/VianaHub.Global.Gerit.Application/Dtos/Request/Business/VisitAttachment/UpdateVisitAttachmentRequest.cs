namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitAttachment;

public record UpdateVisitAttachmentRequest
{
    public string? FileName { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsPrimary { get; init; }
    public bool? IsActive { get; init; }
}
