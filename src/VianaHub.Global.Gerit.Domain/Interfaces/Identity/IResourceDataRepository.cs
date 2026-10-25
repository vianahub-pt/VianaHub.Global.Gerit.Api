using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Identity;

public interface IResourceDataRepository
{
    Task<ResourceEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<ResourceEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<ResourceEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByNameAsync(string? name, CancellationToken ct);
    Task<bool> AddAsync(ResourceEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(ResourceEntity entity, CancellationToken ct);
}
