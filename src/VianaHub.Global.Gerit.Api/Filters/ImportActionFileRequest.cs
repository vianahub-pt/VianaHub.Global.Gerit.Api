namespace VianaHub.Global.Gerit.Api.Filters;

/// <summary>
/// Request para importação de arquivo de Actions (CSV ou JSON)
/// </summary>
public class ImportActionFileRequest
{
    /// <summary>
    /// Arquivo a ser importado (CSV ou JSON)
    /// </summary>
    public IFormFile? File { get; set; }

    /// <summary>
    /// Content Type do request
    /// </summary>
    public string? ContentType { get; set; }
}
