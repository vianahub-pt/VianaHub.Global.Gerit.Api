namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.PartyType;

/// <summary>
/// DTO para atualização de um Tipo de Party.
/// Name e Description atualizam a tradução no idioma padrão (pt-PT).
/// </summary>
public class UpdatePartyTypeRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
