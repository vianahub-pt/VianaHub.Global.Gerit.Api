namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.AddressType;

public class CreateAddressTypeRequest
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Application.Catalog.CanonicalEnglishNameRequired")]
    public List<CreateAddressTypeTranslationRequest>? Translations { get; set; }
}
