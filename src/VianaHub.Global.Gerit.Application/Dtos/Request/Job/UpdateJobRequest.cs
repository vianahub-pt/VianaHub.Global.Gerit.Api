namespace VianaHub.Global.Gerit.Application.Dtos.Request.Job;

public class UpdateJobRequest
{
    public string? Description { get; set; }
    public string? JobPurpose { get; set; }
    public string? CronExpression { get; set; }
    public string? TimeZoneId { get; set; }
    public int TimeoutMinutes { get; set; }
    public int Priority { get; set; }
    public string? Queue { get; set; }
    public int MaxRetries { get; set; }
    public string? JobConfiguration { get; set; }
    public bool? IsActive { get; set; }
}
