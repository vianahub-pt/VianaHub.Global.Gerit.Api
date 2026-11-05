using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IEmployeeDomainService
{
    Task<EmployeeEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<EmployeeEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<EmployeeEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> CreateAsync(EmployeeEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(EmployeeEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(EmployeeEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(EmployeeEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(EmployeeEntity entity, CancellationToken ct);
}
