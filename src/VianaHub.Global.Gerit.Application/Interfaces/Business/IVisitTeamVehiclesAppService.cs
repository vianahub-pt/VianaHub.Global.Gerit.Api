using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeamVehicles;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamVehicle;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamVehicles;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IVisitTeamVehiclesAppService
{
    Task<IEnumerable<VisitTeamVehicleResponse>> GetAllAsync(int visitTeamId, CancellationToken ct);
    Task<VisitTeamVehicleDetailResponse?> GetByIdAsync(int visitTeamId, int id, CancellationToken ct);
    Task<ListPageResponse<VisitTeamVehicleResponse>> GetPagedAsync(int visitTeamId, PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(int visitTeamId, CreateVisitTeamVehicleRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int visitTeamId, int id, UpdateVisitTeamVehicleRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int visitTeamId, int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int visitTeamId, int id, CancellationToken ct);
    Task<bool> DeleteAsync(int visitTeamId, int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(int visitTeamId, IFormFile? file, CancellationToken ct);
}
