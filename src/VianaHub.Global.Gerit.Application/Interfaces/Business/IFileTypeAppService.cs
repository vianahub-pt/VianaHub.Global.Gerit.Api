using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.FileType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.FileType;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IFileTypeAppService
{
    Task<IEnumerable<FileTypeResponse>> GetAllAsync(CancellationToken ct);
    Task<FileTypeDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<FileTypeResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateFileTypeRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateFileTypeRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct);
}
