namespace VianaHub.Global.Gerit.Application.Dtos.Response.Business.Visit;

/// <summary>
/// Resposta detalhada de um Visit (Intervention) — inclui campos de auditoria.
/// Classe independente — não herda de VisitResponse.
/// </summary>
public class VisitDetailResponse
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ClientId { get; set; }
    public string? Client { get; set; }
    public int StatusDefinitionId { get; set; }
    public string? StatusDefinitionName { get; set; }
    public int StatusDomainId { get; set; }
    public string? StatusDomainName { get; set; }
    public string? StatusDefinition { get; set; }
    public string? CurrencyCode { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public decimal EstimatedValue { get; set; }
    public decimal? RealValue { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
