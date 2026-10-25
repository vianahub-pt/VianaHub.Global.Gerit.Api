using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientAddress;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientAddress;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

/// <summary>
/// Interface de servi�o de aplica��o para ClientAddress
/// </summary>
public interface IClientAddressAppService
{
    Task<IEnumerable<ClientAddressResponse>> GetAllAsync(int clientId, CancellationToken ct);
    Task<ClientAddressDetailResponse?> GetByIdAsync(int clientId, int id, CancellationToken ct);
    Task<ListPageResponse<ClientAddressResponse>> GetPagedAsync(int clientId, PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(int clientId, CreateClientAddressRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int clientId, int id, UpdateClientAddressRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int clientId, int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int clientId, int id, CancellationToken ct);
    Task<bool> DeleteAsync(int clientId, int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(int clientId, IFormFile? file, CancellationToken ct);
}
