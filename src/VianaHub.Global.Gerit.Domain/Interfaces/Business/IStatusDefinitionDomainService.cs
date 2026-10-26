using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IStatusDefinitionDomainService
{
    Task<StatusDefinitionEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<StatusDefinitionEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<StatusDefinitionEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);

    Task<bool> CreateAsync(StatusDefinitionEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(StatusDefinitionEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(StatusDefinitionEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(StatusDefinitionEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(StatusDefinitionEntity entity, CancellationToken ct);
}
