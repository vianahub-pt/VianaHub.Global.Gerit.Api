namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDefinition;

/// <summary>
/// DTO para criação de uma Definição de Status.
/// </summary>
public class CreateStatusDefinitionRequest
{
    public int StatusDomainId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsSystem { get; set; }
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Application.Catalog.CanonicalEnglishNameRequired")]
    public List<CreateStatusDefinitionTranslationRequest>? Translations { get; set; }
}
