namespace VianaHub.Global.Gerit.Application.Dtos.Request.Job;

public class CreateJobRequest
{
    public string? JobCategory { get; set; }
    public string? JobName { get; set; }
    public string? Description { get; set; }
    public string? JobPurpose { get; set; }
    public string? JobType { get; set; }
    public string? JobMethod { get; set; } = "Execute";
    public string? CronExpression { get; set; }
    public string? TimeZoneId { get; set; } = "GMT Standard Time";
    public bool ExecuteOnlyOnce { get; set; } = false;
    public int TimeoutMinutes { get; set; } = 5;
    public int Priority { get; set; } = 5;
    public string? Queue { get; set; } = "default";
    public int MaxRetries { get; set; } = 3;
    public string? JobConfiguration { get; set; }
    public bool IsSystemJob { get; set; } = false;
    public bool? IsActive { get; set; }
}
