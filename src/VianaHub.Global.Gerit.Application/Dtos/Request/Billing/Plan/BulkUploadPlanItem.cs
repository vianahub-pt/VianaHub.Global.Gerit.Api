namespace VianaHub.Global.Gerit.Application.Dtos.Request.Billing.Plan;

public class BulkUploadPlanItem
{
    public string? EnglishName { get; set; }
    public string? EnglishDescription { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? PricePerHour { get; set; }
    public decimal? PricePerDay { get; set; }
    public decimal? PricePerMonth { get; set; }
    public decimal? PricePerYear { get; set; }
    public string? Currency { get; set; }
    public int MaxUsers { get; set; }
    public int MaxPhotosPerVisit { get; set; }
}
