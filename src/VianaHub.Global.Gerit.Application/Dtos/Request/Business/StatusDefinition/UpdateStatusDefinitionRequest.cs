namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDefinition;

/// <summary>
/// DTO para atualização de uma Definição de Status.
/// </summary>
public class UpdateStatusDefinitionRequest
{
    public int DisplayOrder { get; set; }
    public bool IsSystem { get; set; }
}
