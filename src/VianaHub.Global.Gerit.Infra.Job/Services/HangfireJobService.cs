using Microsoft.Extensions.Logging;
using VianaHub.Global.Gerit.Domain.Interfaces.Job;
using VianaHub.Global.Gerit.Domain.Entities.Job;

namespace VianaHub.Global.Gerit.Infra.Job.Services;

public class HangfireJobService : VianaHub.Global.Gerit.Domain.Interfaces.Job.IJobSchedulerService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HangfireJobService> _logger;
    private readonly IJobExecutor _jobExecutor;

    public HangfireJobService(IServiceProvider serviceProvider, ILogger<HangfireJobService> logger, IJobExecutor jobExecutor)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _jobExecutor = jobExecutor;
    }

    public async Task RegisterRecurringAsync(JobDefinitionsEntity jobDef)
    {
        if (jobDef == null) throw new ArgumentNullException(nameof(jobDef));
        if (jobDef.ExecuteOnlyOnce) return;

        if (string.IsNullOrWhiteSpace(jobDef.CronExpression))
        {
            _logger.LogWarning("Skipping registration for recurring job '{JobName}' because CronExpression is empty", jobDef.JobName);
            return;
        }

        // Resolve timezone safely, fallback to UTC if not found
        TimeZoneInfo timeZone = TimeZoneInfo.Utc;
        if (!string.IsNullOrWhiteSpace(jobDef.TimeZoneId))
        {
            try
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(jobDef.TimeZoneId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TimeZone '{TimeZoneId}' not found for job '{JobName}', falling back to UTC", jobDef.TimeZoneId, jobDef.JobName);
                timeZone = TimeZoneInfo.Utc;
            }
        }

        // Delegate to the concrete job executor which registers a stable invocation target for Hangfire
        var registered = await _jobExecutor.RegisterRecurringJobAsync(jobDef);
        if (registered)
        {
            _logger.LogInformation("Registered recurring job {JobName} in Hangfire", jobDef.JobName);
        }
        else
        {
            _logger.LogWarning("Failed to register recurring job {JobName} via job executor", jobDef.JobName);
        }
    }

    public async Task RemoveRecurringAsync(string jobName)
    {
        await _jobExecutor.RemoveRecurringJobAsync(jobName);
        _logger.LogInformation("Removed recurring job {JobName} from Hangfire", jobName);
    }

    public async Task<string?> EnqueueJobAsync(JobDefinitionsEntity jobDef)
    {
        if (jobDef == null) throw new ArgumentNullException(nameof(jobDef));

        var jobId = await _jobExecutor.EnqueueJobAsync(jobDef);
        _logger.LogInformation("Enqueued job {JobName} with Hangfire id {JobId}", jobDef.JobName, jobId);
        return jobId;
    }
}
