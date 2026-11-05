namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDomain;

/// <summary>
/// DTO para atualização de um Domínio de Status.
/// Name e Description atualizam a tradução no idioma padrão (pt-PT).
/// </summary>
public class UpdateStatusDomainRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
