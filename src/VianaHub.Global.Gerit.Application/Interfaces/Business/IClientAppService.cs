using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Client;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Client;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

/// <summary>
/// Interface de Application Service para Client
/// </summary>
public interface IClientAppService
{
    Task<IEnumerable<ClientResponse>> GetAllAsync(CancellationToken ct);
    Task<ClientDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<ClientResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateClientRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateClientRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct);
}
