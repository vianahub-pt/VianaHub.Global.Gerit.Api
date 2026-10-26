using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Resource;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Resource;

namespace VianaHub.Global.Gerit.Application.Interfaces.Identity;

public interface IResourceAppService
{
    Task<IEnumerable<ResourceResponse>> GetAllAsync(CancellationToken ct);
    Task<ResourceDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<ResourceResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateResourceRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateResourceRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct);
}
