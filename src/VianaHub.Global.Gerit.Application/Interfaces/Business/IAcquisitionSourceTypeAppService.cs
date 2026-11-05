using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.AcquisitionSourceType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.AcquisitionSourceType;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IAcquisitionSourceTypeAppService
{
    Task<IEnumerable<AcquisitionSourceTypeResponse>> GetAllAsync(CancellationToken ct);
    Task<AcquisitionSourceTypeDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<AcquisitionSourceTypeResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateAcquisitionSourceTypeRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateAcquisitionSourceTypeRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct);
}
