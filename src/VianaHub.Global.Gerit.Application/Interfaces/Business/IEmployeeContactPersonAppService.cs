using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.EmployeeContact;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeContact;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

/// <summary>
/// Interface de serviço de aplicação para EmployeeContact
/// </summary>
public interface IEmployeeContactPersonAppService
{
    Task<IEnumerable<EmployeeContactPersonResponse>> GetAllAsync(int employeeId, CancellationToken ct);
    Task<EmployeeContactPersonDetailResponse> GetByIdAsync(int employeeId, int id, CancellationToken ct);
    Task<ListPageResponse<EmployeeContactPersonResponse>> GetPagedAsync(int employeeId, PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(int employeeId, CreateEmployeeContactPersonRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int employeeId, int id, UpdateEmployeeContactPersonRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int employeeId, int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int employeeId, int id, CancellationToken ct);
    Task<bool> DeleteAsync(int employeeId, int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(int employeeId, IFormFile file, CancellationToken ct);
}
