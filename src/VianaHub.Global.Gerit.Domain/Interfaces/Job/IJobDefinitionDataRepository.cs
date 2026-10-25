using VianaHub.Global.Gerit.Domain.Entities.Job;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Job;

public interface IJobDefinitionDataRepository
{
    Task<JobDefinitionsEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<JobDefinitionsEntity?> GetByNameAsync(string jobName, CancellationToken ct);
    Task<IEnumerable<JobDefinitionsEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<JobDefinitionsEntity>> GetPagedAsync(PagedFilter filter, CancellationToken ct);
    Task<bool> ExistsByNameAsync(string? jobName, CancellationToken ct);
    Task<bool> CreateAsync(JobDefinitionsEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(JobDefinitionsEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(JobDefinitionsEntity entity, CancellationToken ct);
}
