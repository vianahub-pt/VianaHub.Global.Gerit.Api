using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

/// <summary>
/// Interface de repositório para EmployeeContact
/// </summary>
public interface IEmployeeContactPersonDataRepository
{
    Task<EmployeeContactPersonsEntity?> GetByIdAsync(int employeeId, int id, CancellationToken ct);
    Task<IEnumerable<EmployeeContactPersonsEntity>> GetAllAsync(int employeeId, CancellationToken ct);
    Task<ListPage<EmployeeContactPersonsEntity>> GetPagedAsync(int employeeId, PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByEmailAsync(int tenantId, int EmployeeId, string? email, CancellationToken ct);
    Task<bool> ExistsByEmailForUpdateAsync(int tenantId, int EmployeeId, string? email, int excludeId, CancellationToken ct);
    Task<bool> AddAsync(EmployeeContactPersonsEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(EmployeeContactPersonsEntity entity, CancellationToken ct);
}
