using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Equipment;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Equipment;
using Microsoft.AspNetCore.Http;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IEquipmentAppService
{
    Task<IEnumerable<EquipmentResponse>> GetAllAsync(CancellationToken ct);
    Task<EquipmentDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<EquipmentResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateEquipmentRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateEquipmentRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct);
}
