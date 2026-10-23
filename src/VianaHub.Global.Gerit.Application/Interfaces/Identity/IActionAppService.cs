using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Action;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Action;

namespace VianaHub.Global.Gerit.Application.Interfaces.Identity;

public interface IActionAppService
{
    Task<IEnumerable<ActionResponse>> GetAllAsync(CancellationToken ct);
    Task<ActionDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<ActionResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateActionRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateActionRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile file, CancellationToken ct);

}
