using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.EmployeeFiscalData;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeFiscalData;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IEmployeeFiscalDataAppService
{
    Task<IEnumerable<EmployeeFiscalDataResponse>> GetAllAsync(int employeeId, CancellationToken ct);
    Task<EmployeeFiscalDataDetailResponse?> GetByIdAsync(int employeeId, int id, CancellationToken ct);
    Task<ListPageResponse<EmployeeFiscalDataResponse>> GetPagedAsync(int employeeId, PagedFilterRequest request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int employeeId, CancellationToken ct = default);
    Task<bool> ExistsByTaxNumberAsync(int employeeId, string taxNumber, CancellationToken ct = default);
    Task<int> CreateAsync(int employeeId, CreateEmployeeFiscalDataRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int employeeId, int id, UpdateEmployeeFiscalDataRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int employeeId, int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int employeeId, int id, CancellationToken ct);
    Task<bool> DeleteAsync(int employeeId, int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(int employeeId, IFormFile? file, CancellationToken ct);
}
