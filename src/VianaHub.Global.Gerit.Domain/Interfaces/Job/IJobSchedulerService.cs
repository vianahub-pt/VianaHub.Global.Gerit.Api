using System.Threading.Tasks;
using VianaHub.Global.Gerit.Domain.Entities.Job;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Job;

/// <summary>
/// Abstra��o para agendamento e execu��o de jobs (adapter para Hangfire ou outros sistemas).
/// </summary>
public interface IJobSchedulerService
{
    Task RegisterRecurringAsync(JobDefinitionsEntity jobDef);

    Task RemoveRecurringAsync(string jobName);

    Task<string?> EnqueueJobAsync(JobDefinitionsEntity jobDef);
}
