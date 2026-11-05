using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientContact;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientContact;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

/// <summary>
/// Interface de Application Service para ClientContact
/// </summary>
public interface IClientContactPersonAppService
{
    Task<IEnumerable<ClientContactPersonResponse>> GetAllAsync(int clientId, CancellationToken ct);
    Task<ClientContactPersonDetailResponse?> GetByIdAsync(int clientId, int id, CancellationToken ct);
    Task<ListPageResponse<ClientContactPersonResponse>> GetPagedAsync(int clientId, PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(int clientId, CreateClientContactRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int clientId, int id, UpdateClientContactPersonRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int clientId, int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int clientId, int id, CancellationToken ct);
    Task<bool> DeleteAsync(int clientId, int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(int clientId, IFormFile? file, CancellationToken ct);
}
