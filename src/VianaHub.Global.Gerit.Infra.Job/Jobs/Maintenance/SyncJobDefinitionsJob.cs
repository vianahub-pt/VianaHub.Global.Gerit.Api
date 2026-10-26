using VianaHub.Global.Gerit.Domain.Helpers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VianaHub.Global.Gerit.Infra.Job.Interfaces;
using VianaHub.Global.Gerit.Infra.Job.Jobs;

namespace VianaHub.Global.Gerit.Infra.Job.Jobs.Maintenance;

/// <summary>
/// Job responsável por disparar a sincronização de JobDefinitions com o Hangfire.
/// Agendado via JobDefinition (JobType = full name desta classe). CronExpression controla periodicidade;
/// IsActive controla ativação/desativação.
/// </summary>
public class SyncJobDefinitionsJob : VianaHub.Global.Gerit.Infra.Job.Interfaces.IJob
{
    private readonly IJobSyncService _jobSyncService;
    private readonly ILogger<SyncJobDefinitionsJob> _logger;

    public SyncJobDefinitionsJob(IJobSyncService jobSyncService, ILogger<SyncJobDefinitionsJob> logger)
    {
        _jobSyncService = jobSyncService;
        _logger = logger;
    }

    public async Task Execute(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SyncJobDefinitionsJob] Starting job to synchronize JobDefinitions with Hangfire");

        try
        {
            await _jobSyncService.SyncJobsWithHangfire(cancellationToken);
            _logger.LogInformation("[SyncJobDefinitionsJob] Synchronization completed successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[SyncJobDefinitionsJob] Synchronization was cancelled");
        }
        catch (System.Exception ex)
        {
            _logger.LogError("[SyncJobDefinitionsJob] Error while synchronizing JobDefinitions with Hangfire | Exception: {ExceptionDetails}", LogValueSanitizer.ExceptionDetails(ex));
            throw; // rethrow to let Hangfire handle retries if configured
        }
    }
}
