using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.EquipmentType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EquipmentType;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IEquipmentTypeAppService
{
    Task<IEnumerable<EquipmentTypeResponse>> GetAllAsync(CancellationToken ct);
    Task<EquipmentTypeDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<EquipmentTypeResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateEquipmentTypeRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateEquipmentTypeRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct);
}
