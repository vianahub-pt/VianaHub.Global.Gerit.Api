using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IVisitTeamEmployeeDataRepository
{
    Task<VisitTeamEmployeeEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<VisitTeamEmployeeEntity>> GetAllAsync(CancellationToken ct);
    Task<IEnumerable<VisitTeamEmployeeEntity>> GetByVisitTeamIdAsync(int visitTeamId, CancellationToken ct);
    Task<IEnumerable<VisitTeamEmployeeEntity>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct);
    Task<IEnumerable<VisitTeamEmployeeEntity>> GetActiveByVisitTeamIdAsync(int visitTeamId, CancellationToken ct);
    Task<ListPage<VisitTeamEmployeeEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsActiveAssignmentAsync(int visitTeamId, int employeeId, CancellationToken ct);
    Task<bool> AddAsync(VisitTeamEmployeeEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(VisitTeamEmployeeEntity entity, CancellationToken ct);
}
