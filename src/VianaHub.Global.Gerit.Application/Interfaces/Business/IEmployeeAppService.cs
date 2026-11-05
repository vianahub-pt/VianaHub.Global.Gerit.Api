using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Employee;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Employee;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IEmployeeAppService
{
    Task<IEnumerable<EmployeeResponse>> GetAllAsync(CancellationToken ct);
    Task<EmployeeDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<EmployeeResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateEmployeeRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct);
}
