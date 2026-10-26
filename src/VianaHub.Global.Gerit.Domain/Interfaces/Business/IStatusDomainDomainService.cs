using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IStatusDomainDomainService
{
    Task<StatusDomainEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<StatusDomainEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<StatusDomainEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);

    Task<bool> CreateAsync(StatusDomainEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(StatusDomainEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(StatusDomainEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(StatusDomainEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(StatusDomainEntity entity, CancellationToken ct);
}
