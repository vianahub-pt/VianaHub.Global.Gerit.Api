using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IEmployeeTeamDataRepository
{
    Task<EmployeeTeamEntity?> GetByIdAsync(int employeeId, int id, CancellationToken ct);
    Task<IEnumerable<EmployeeTeamEntity>> GetAllAsync(int employeeId, CancellationToken ct);
    Task<ListPage<EmployeeTeamEntity>> GetPagedAsync(int employeeId, PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByTeamAndMemberAsync(int tenantId, int teamId, int EmployeeId, CancellationToken ct);
    Task<bool> AddAsync(EmployeeTeamEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(EmployeeTeamEntity entity, CancellationToken ct);
}
