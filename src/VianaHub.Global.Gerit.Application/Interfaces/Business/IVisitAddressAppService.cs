using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitAddress;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitAddress;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IVisitAddressAppService
{
    Task<IEnumerable<VisitAddressResponse>> GetAllAsync(int visitId, CancellationToken ct);
    Task<VisitAddressDetailResponse?> GetByIdAsync(int visitId, int id, CancellationToken ct);
    Task<ListPageResponse<VisitAddressResponse>> GetPagedAsync(int visitId, PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(int visitId, CreateVisitAddressRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int visitId, int id, UpdateVisitAddressRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int visitId, int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int visitId, int id, CancellationToken ct);
    Task<bool> DeleteAsync(int visitId, int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(int visitId, IFormFile? file, CancellationToken ct);
}
