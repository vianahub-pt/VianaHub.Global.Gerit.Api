namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.AcquisitionSourceType;

public class CreateAcquisitionSourceTypeRequest
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Application.Catalog.CanonicalEnglishNameRequired")]
    public List<CreateAcquisitionSourceTypeTranslationRequest>? Translations { get; set; }
}
