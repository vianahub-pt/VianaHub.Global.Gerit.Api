namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.DocumentType;

/// <summary>
/// DTO para criação de um Tipo de Documento (BI, Passaporte, NIF, etc.).
/// </summary>
public class CreateDocumentTypeRequest
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Application.Catalog.CanonicalEnglishNameRequired")]
    public List<CreateDocumentTypeTranslationRequest>? Translations { get; set; }
}
