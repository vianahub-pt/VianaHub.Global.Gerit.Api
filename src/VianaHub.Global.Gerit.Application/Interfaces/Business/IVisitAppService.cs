using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Visit;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Visit;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IVisitAppService
{
    Task<IEnumerable<VisitResponse>> GetAllAsync(CancellationToken ct);
    Task<VisitDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<VisitResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateVisitRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateVisitRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct);
}
