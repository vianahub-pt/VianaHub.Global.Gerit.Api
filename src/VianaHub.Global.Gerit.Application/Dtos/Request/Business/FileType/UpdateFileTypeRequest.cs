namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.FileType;

public class UpdateFileTypeRequest
{
    public string? MimeType { get; set; }
    public string? Extension { get; set; }
    public bool? IsActive { get; set; }
}
