using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientDocument;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientDocument;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IClientDocumentAppService
{
    Task<IEnumerable<ClientDocumentResponse>> GetAllAsync(int clientId, CancellationToken ct);
    Task<ClientDocumentDetailResponse?> GetByIdAsync(int clientId, int id, CancellationToken ct);
    Task<ListPageResponse<ClientDocumentResponse>> GetPagedAsync(int clientId, PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(int clientId, CreateClientDocumentRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int clientId, int id, UpdateClientDocumentRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int clientId, int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int clientId, int id, CancellationToken ct);
    Task<bool> DeleteAsync(int clientId, int id, CancellationToken ct);
}
