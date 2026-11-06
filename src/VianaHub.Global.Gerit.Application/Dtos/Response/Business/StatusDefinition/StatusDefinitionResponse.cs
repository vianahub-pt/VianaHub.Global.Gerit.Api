namespace VianaHub.Global.Gerit.Application.Dtos.Response.Business.StatusDefinition;

/// <summary>
/// Resposta resumida de uma Definição de Status (usada em listagens).
/// </summary>
public class StatusDefinitionResponse
{
    public int Id { get; set; }
    public int StatusDomainId { get; set; }
    public string? LanguageCode { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
}
