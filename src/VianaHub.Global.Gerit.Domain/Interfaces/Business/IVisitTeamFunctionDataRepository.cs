using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IVisitTeamFunctionDataRepository
{
    Task<VisitTeamFunctionsEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<VisitTeamFunctionsEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<VisitTeamFunctionsEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByNameAsync(int tenantId, string? name, CancellationToken ct);
    Task<bool> AddAsync(VisitTeamFunctionsEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(VisitTeamFunctionsEntity entity, CancellationToken ct);
}
