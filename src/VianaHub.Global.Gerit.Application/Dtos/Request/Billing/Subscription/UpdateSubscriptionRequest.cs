namespace VianaHub.Global.Gerit.Application.Dtos.Request.Billing.Subscription;

public class UpdateSubscriptionRequest
{
    public int SubscriptionPlanId { get; set; }
    public int StatusDefinitionId { get; set; }
    public int StatusDomainId { get; set; }
    public decimal AgreedAmount { get; set; }
    public string? BillingInterval { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? TrialStart { get; set; }
    public DateTime? TrialEnd { get; set; }
    public bool? IsActive { get; set; }
}
