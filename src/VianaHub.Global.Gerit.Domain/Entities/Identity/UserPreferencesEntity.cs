using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Identity;

public class UserPreferencesEntity : Entity
{
    public int TenantId { get; private set; }
    public int UserId { get; private set; }

    public string? Appearance { get; private set; }
    public string? CurrencyCode { get; private set; }
    public string? Locale { get; private set; }
    public string? Timezone { get; private set; }
    public string? DateFormat { get; private set; }
    public string? TimeFormat { get; private set; }
    public TimeSpan DayStart { get; private set; }
    public TimeSpan DayEnd { get; private set; }

    public bool EmailNewsletter { get; private set; }
    public bool EmailWeeklyReport { get; private set; }
    public bool EmailApproval { get; private set; }
    public bool EmailAlerts { get; private set; }
    public bool EmailReminders { get; private set; }
    public bool EmailPlanner { get; private set; }

    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation
    public TenantEntity? Tenant { get; private set; }
    public UserEntity? User { get; private set; }

    protected UserPreferencesEntity() { }

    public UserPreferencesEntity(int tenantId, int userId, string? appearance, string? currencyCode, string? locale, string? timezone, string? dateFormat, string? timeFormat, TimeSpan dayStart, TimeSpan dayEnd, int createdBy)
    {
        TenantId = tenantId;
        UserId = userId;
        Appearance = appearance;
        CurrencyCode = currencyCode;
        Locale = locale;
        Timezone = timezone;
        DateFormat = dateFormat;
        TimeFormat = timeFormat;
        DayStart = dayStart;
        DayEnd = dayEnd;

        EmailNewsletter = false;
        EmailWeeklyReport = false;
        EmailApproval = false;
        EmailAlerts = true;
        EmailReminders = true;
        EmailPlanner = true;

        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string? appearance, string? locale, string? currencyCode, string? timezone, string? dateFormat, string? timeFormat, TimeSpan dayStart, TimeSpan dayEnd,
        bool emailNewsletter, bool emailWeeklyReport, bool emailApproval, bool emailAlerts, bool emailReminders, bool emailPlanner, int modifiedBy)
    {
        Appearance = appearance;
        CurrencyCode = currencyCode;
        Locale = locale;
        Timezone = timezone;
        DateFormat = dateFormat;
        TimeFormat = timeFormat;
        DayStart = dayStart;
        DayEnd = dayEnd;

        EmailNewsletter = emailNewsletter;
        EmailWeeklyReport = emailWeeklyReport;
        EmailApproval = emailApproval;
        EmailAlerts = emailAlerts;
        EmailReminders = emailReminders;
        EmailPlanner = emailPlanner;

        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Activate(int modifiedBy)
    {
        IsActive = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Deactivate(int modifiedBy)
    {
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Delete(int modifiedBy)
    {
        IsDeleted = true;
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }
}
