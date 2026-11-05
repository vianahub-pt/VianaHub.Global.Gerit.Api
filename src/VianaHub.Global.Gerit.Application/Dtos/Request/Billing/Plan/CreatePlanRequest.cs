namespace VianaHub.Global.Gerit.Application.Dtos.Request.Billing.Plan;

public class CreatePlanRequest
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Application.Catalog.CanonicalEnglishNameRequired")]
    public List<CreatePlanTranslationRequest>? Translations { get; set; }
    public decimal? PricePerHour { get; set; }
    public decimal? PricePerDay { get; set; }
    public decimal? PricePerMonth { get; set; }
    public decimal? PricePerYear { get; set; }
    public string? Currency { get; set; }
    public int MaxUsers { get; set; }
    public int MaxPhotosPerVisit { get; set; }
}
