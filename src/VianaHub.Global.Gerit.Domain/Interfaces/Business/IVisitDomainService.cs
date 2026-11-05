using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IVisitDomainService
{
    Task<VisitEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<VisitEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<VisitEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> CreateAsync(VisitEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(VisitEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(VisitEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(VisitEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(VisitEntity entity, CancellationToken ct);
}
