using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeamEquipments;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamEquipments;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IVisitTeamEquipmentsAppService
{
    Task<IEnumerable<VisitTeamEquipmentResponse>> GetAllAsync(int visitTeamId, CancellationToken ct);
    Task<VisitTeamEquipmentDetailResponse?> GetByIdAsync(int visitTeamId, int id, CancellationToken ct);
    Task<ListPageResponse<VisitTeamEquipmentResponse>> GetPagedAsync(int visitTeamId, PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(int visitTeamId, CreateVisitTeamEquipmentRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int visitTeamId, int id, UpdateVisitTeamEquipmentRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int visitTeamId, int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int visitTeamId, int id, CancellationToken ct);
    Task<bool> DeleteAsync(int visitTeamId, int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(int visitTeamId, IFormFile? file, CancellationToken ct);
}
