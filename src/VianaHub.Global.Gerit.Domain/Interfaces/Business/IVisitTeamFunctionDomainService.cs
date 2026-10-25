using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IVisitTeamFunctionDomainService
{
    Task<VisitTeamFunctionsEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<VisitTeamFunctionsEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<VisitTeamFunctionsEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> CreateAsync(VisitTeamFunctionsEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(VisitTeamFunctionsEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(VisitTeamFunctionsEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(VisitTeamFunctionsEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(VisitTeamFunctionsEntity entity, CancellationToken ct);
}
