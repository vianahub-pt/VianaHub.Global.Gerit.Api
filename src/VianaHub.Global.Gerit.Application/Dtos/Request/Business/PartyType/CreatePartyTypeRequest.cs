namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.PartyType;

/// <summary>
/// DTO para criação de um Tipo de Party (Pessoa Física / Jurídica).
/// As traduções incluem obrigatoriamente um Name canónico em en-US.
/// </summary>
public class CreatePartyTypeRequest
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Application.Catalog.CanonicalEnglishNameRequired")]
    public List<CreatePartyTypeTranslationRequest>? Translations { get; set; }
}
