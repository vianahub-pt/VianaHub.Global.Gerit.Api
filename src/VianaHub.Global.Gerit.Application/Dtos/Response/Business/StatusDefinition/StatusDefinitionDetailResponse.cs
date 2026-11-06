namespace VianaHub.Global.Gerit.Application.Dtos.Response.Business.StatusDefinition;

/// <summary>
/// Resposta detalhada de uma Definição de Status (inclui traduções).
/// </summary>
public class StatusDefinitionDetailResponse
{
    public int Id { get; set; }
    public int StatusDomainId { get; set; }
    public int TenantId { get; set; }
    public string? TenantName { get; set; }
    public string? Code { get; set; }
    public string? LanguageCode { get; set; }
    public string? Name { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
