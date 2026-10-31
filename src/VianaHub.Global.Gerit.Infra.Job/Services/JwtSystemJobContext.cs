using VianaHub.Global.Gerit.Domain.Interfaces.Job;
using VianaHub.Global.Gerit.Infra.Job.Jobs.Cleanup;
using VianaHub.Global.Gerit.Infra.Job.Jobs.Security;

namespace VianaHub.Global.Gerit.Infra.Job.Services;

public sealed class JwtSystemJobContext : IJwtSystemJobContext
{
    public bool IsAuthorized => JobContext.IsInBackgroundJob && JobContext.CurrentJobName is
        nameof(JwtKeyRotationJob) or nameof(ReconcileJwtKeysJob) or nameof(CleanupExpiredJwtKeysJob);
}
