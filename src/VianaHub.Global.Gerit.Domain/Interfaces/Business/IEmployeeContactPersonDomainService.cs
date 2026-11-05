using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

/// <summary>
/// Interface de serviço de domínio para EmployeeContact
/// </summary>
public interface IEmployeeContactPersonDomainService
{
    Task<EmployeeContactPersonsEntity?> GetByIdAsync(int employeeId, int id, CancellationToken ct);
    Task<IEnumerable<EmployeeContactPersonsEntity>> GetAllAsync(int employeeId, CancellationToken ct);
    Task<ListPage<EmployeeContactPersonsEntity>> GetPagedAsync(int employeeId, PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> CreateAsync(EmployeeContactPersonsEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(EmployeeContactPersonsEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(EmployeeContactPersonsEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(EmployeeContactPersonsEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(EmployeeContactPersonsEntity entity, CancellationToken ct);
}
