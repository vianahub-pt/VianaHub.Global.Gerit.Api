using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Vehicle;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Vehicle;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IVehicleAppService
{
    Task<IEnumerable<VehicleResponse>> GetAllAsync(CancellationToken ct);
    Task<VehicleDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<VehicleResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateVehicleRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateVehicleRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct);
}
