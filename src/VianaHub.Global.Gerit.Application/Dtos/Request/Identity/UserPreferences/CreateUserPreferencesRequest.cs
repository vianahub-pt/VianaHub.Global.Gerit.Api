namespace VianaHub.Global.Gerit.Application.Dtos.Request.Identity.UserPreferences;

public class CreateUserPreferencesRequest
{
    public string? Appearance { get; set; }
    public string? CurrencyCode { get; set; }
    public string? Locale { get; set; }
    public string? Timezone { get; set; }
    public string? DateFormat { get; set; }
    public string? TimeFormat { get; set; }
    public string? DayStart { get; set; }
    public string? DayEnd { get; set; }

    public bool EmailNewsletter { get; set; }
    public bool EmailWeeklyReport { get; set; }
    public bool EmailApproval { get; set; }
    public bool EmailAlerts { get; set; }
    public bool EmailReminders { get; set; }
    public bool EmailPlanner { get; set; }
    public bool? IsActive { get; set; }
}
