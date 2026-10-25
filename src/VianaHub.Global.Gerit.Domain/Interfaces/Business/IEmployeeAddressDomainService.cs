using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

/// <summary>
/// Interface de serviço de domínio para EmployeeAddress
/// </summary>
public interface IEmployeeAddressDomainService
{
    Task<EmployeeAddressesEntity?> GetByIdAsync(int employeeId, int id, CancellationToken ct);
    Task<IEnumerable<EmployeeAddressesEntity>> GetAllAsync(int employeeId, CancellationToken ct);
    Task<ListPage<EmployeeAddressesEntity>> GetPagedAsync(int employeeId, PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);

    Task<bool> CreateAsync(EmployeeAddressesEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(EmployeeAddressesEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(EmployeeAddressesEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(EmployeeAddressesEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(EmployeeAddressesEntity entity, CancellationToken ct);
}
