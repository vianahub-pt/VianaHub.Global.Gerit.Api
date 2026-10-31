using VianaHub.Global.Gerit.Domain.ReadModels;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Job;

public interface IJwtSystemJobTenantDiscovery
{
    Task<IReadOnlyList<SystemJobTenant>> GetAllAsync(CancellationToken cancellationToken);
}
