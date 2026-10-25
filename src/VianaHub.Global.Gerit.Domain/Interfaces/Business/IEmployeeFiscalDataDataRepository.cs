using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IEmployeeFiscalDataDataRepository
{
    Task<IEnumerable<EmployeeFiscalDataEntity>> GetAllAsync(int employeeId, CancellationToken ct);
    Task<EmployeeFiscalDataEntity?> GetByIdAsync(int employeeId, int id, CancellationToken ct = default);
    Task<ListPage<EmployeeFiscalDataEntity>> GetPagedAsync(int employeeId, PagedFilter filter, CancellationToken ct = default);
    Task<bool> ExistsByIdAsync(int employeeId, CancellationToken ct = default);
    Task<bool> ExistsByTaxNumberAsync(int employeeId, string taxNumber, CancellationToken ct = default);
    Task<bool> CreateAsync(EmployeeFiscalDataEntity entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(EmployeeFiscalDataEntity entity, CancellationToken ct = default);
}
