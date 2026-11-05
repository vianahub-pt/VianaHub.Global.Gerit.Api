namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDomain;

/// <summary>
/// DTO para criação de um Domínio de Status.
/// As traduções incluem obrigatoriamente um Name canónico em en-US.
/// </summary>
public class CreateStatusDomainRequest
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Application.Catalog.CanonicalEnglishNameRequired")]
    public List<CreateStatusDomainTranslationRequest>? Translations { get; set; }
}
