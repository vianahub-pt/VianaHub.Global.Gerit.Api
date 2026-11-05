using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.AddressType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.AddressType;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IAddressTypeAppService
{
    Task<IEnumerable<AddressTypeResponse>> GetAllAsync(CancellationToken ct);
    Task<AddressTypeDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<AddressTypeResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateAddressTypeRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateAddressTypeRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct);
}
