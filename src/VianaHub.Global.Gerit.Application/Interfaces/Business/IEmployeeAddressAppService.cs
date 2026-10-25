using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.EmployeeAddress;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeAddress;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

/// <summary>
/// Interface de serviço de aplicação para EmployeeAddress
/// </summary>
public interface IEmployeeAddressAppService
{
    Task<IEnumerable<EmployeeAddressResponse>> GetAllAsync(int employeeId, CancellationToken ct);
    Task<EmployeeAddressDetailResponse?> GetByIdAsync(int employeeId, int id, CancellationToken ct);
    Task<ListPageResponse<EmployeeAddressResponse>> GetPagedAsync(int employeeId, PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(int employeeId, CreateEmployeeAddressRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int employeeId, int id, UpdateEmployeeAddressRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int employeeId, int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int employeeId, int id, CancellationToken ct);
    Task<bool> DeleteAsync(int employeeId, int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(int employeeId, IFormFile? file, CancellationToken ct);
}
