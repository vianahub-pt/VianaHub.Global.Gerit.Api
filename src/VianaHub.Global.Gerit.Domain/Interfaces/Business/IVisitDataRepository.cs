using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

/// <summary>
/// Interface de Data Repository para Visit
/// </summary>
public interface IVisitDataRepository
{
    Task<VisitEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<VisitEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<VisitEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByTenantAndTitleAsync(int tenantId, string? title, int? excludeId, CancellationToken ct);
    Task<bool> AddAsync(VisitEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(VisitEntity entity, CancellationToken ct);
}
