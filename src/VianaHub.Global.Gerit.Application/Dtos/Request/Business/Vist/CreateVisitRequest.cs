namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.Visit;

public class CreateVisitRequest
{
    public int ClientId { get; set; }
    public int StatusDefinitionId { get; set; }
    public int StatusDomainId { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartDateTime { get; set; }
    public decimal EstimatedValue { get; set; }
    public bool? IsActive { get; set; }
}
