using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VianaHub.Global.Gerit.Domain.Entities.Job;
using VianaHub.Global.Gerit.Domain.Interfaces.Job;
using VianaHub.Global.Gerit.Infra.Job.Interfaces;

namespace VianaHub.Global.Gerit.Infra.Job.Services;

/// <summary>
/// Implementação do executor de background jobs usando Hangfire
/// </summary>
public class HangfireJobExecutor : IJobExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HangfireJobExecutor> _logger;

    public HangfireJobExecutor(
        IServiceProvider serviceProvider,
        ILogger<HangfireJobExecutor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task<bool> RegisterRecurringJobAsync(JobDefinitionsEntity jobDefinition)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(jobDefinition.TimeZoneId);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(jobDefinition.TimeZoneId);

            RecurringJob.AddOrUpdate(
                jobDefinition.JobName,
                jobDefinition.Queue,
                () => ExecuteJobInternal(jobDefinition.JobType, jobDefinition.JobMethod),
                jobDefinition.CronExpression,
                new RecurringJobOptions { TimeZone = timeZone }
            );

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register recurring job {JobName}", jobDefinition.JobName);
            return Task.FromResult(false);
        }
    }

    public Task<bool> RemoveRecurringJobAsync(string jobName)
    {
        try
        {
            RecurringJob.RemoveIfExists(jobName);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove recurring job {JobName}", jobName);
            return Task.FromResult(false);
        }
    }

    public Task<string?> EnqueueJobAsync(JobDefinitionsEntity jobDefinition)
    {
        try
        {
            var jobId = BackgroundJob.Enqueue(
                () => ExecuteJobInternal(jobDefinition.JobType, jobDefinition.JobMethod)
            );

            return Task.FromResult<string?>(jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue job {JobName}", jobDefinition.JobName);
            return Task.FromResult<string?>(null);
        }
    }

    /// <summary>
    /// Método auxiliar para execução do job - será chamado pelo Hangfire.
    /// Este método usa reflexão para instanciar e executar dinamicamente jobs registrados.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteJobInternal(string? jobType, string? jobMethod)
    {
        // Criar um escopo para resolver dependências do job
        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<HangfireJobExecutor>>();

        try
        {
            logger.LogInformation("Starting execution of job {JobType}.{JobMethod}", jobType, jobMethod);

            // Validar e carregar o tipo do job via reflexão
            ArgumentNullException.ThrowIfNull(jobType);
            var type = Type.GetType(jobType);
            if (type == null)
            {
                logger.LogError("Job type not found: {JobType}", jobType);
                throw new InvalidOperationException($"Job type not found: {jobType}");
            }

            // Validar se a classe implementa IBackgroundJob
            if (!typeof(IJob).IsAssignableFrom(type))
            {
                logger.LogError("Job type {JobType} does not implement IBackgroundJob", jobType);
                throw new InvalidOperationException($"Job type {jobType} must implement IBackgroundJob interface");
            }

            // Instanciar o job usando o container de DI
            var job = ActivatorUtilities.CreateInstance(scope.ServiceProvider, type) as IJob;
            if (job == null)
            {
                logger.LogError("Failed to instantiate job type: {JobType}", jobType);
                throw new InvalidOperationException($"Failed to instantiate job type: {jobType}");
            }

            // ? NOVO: Configurar contexto de background job explicitamente
            var jobName = type.Name;
            var jobExecutionId = Guid.NewGuid().ToString("N");

            using (JobContext.EnterBackgroundJobScope(jobName, jobExecutionId))
            {
                logger.LogInformation(
                    "?? [BackgroundJob] Executing {JobName} with execution ID {ExecutionId}",
                    jobName, jobExecutionId);

                // Executar o job através do método padrão Execute
                await job.Execute(CancellationToken.None);

                logger.LogInformation(
                    "? [BackgroundJob] Successfully completed {JobName} (execution ID: {ExecutionId})",
                    jobName, jobExecutionId);
            }

            logger.LogInformation("Successfully completed execution of job {JobType}.{JobMethod}", jobType, jobMethod);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing job {JobType}.{JobMethod}: {ErrorMessage}",
                jobType, jobMethod, ex.Message);
            throw;
        }
    }
}
