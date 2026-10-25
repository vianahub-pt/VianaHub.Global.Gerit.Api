using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientFiscalData;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientFiscalData;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IClientFiscalDataAppService
{
    Task<IEnumerable<ClientFiscalDataResponse>> GetAllAsync(int clientId, CancellationToken ct);
    Task<ClientFiscalDataDetailResponse?> GetByIdAsync(int clientId, int id, CancellationToken ct);
    Task<ListPageResponse<ClientFiscalDataResponse>> GetPagedAsync(int clientId, PagedFilterRequest request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int clientId, CancellationToken ct = default);
    Task<bool> ExistsByTaxNumberAsync(int clientId, string taxNumber, CancellationToken ct = default);
    Task<int> CreateAsync(int clientId, CreateClientFiscalDataRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int clientId, int id, UpdateClientFiscalDataRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int clientId, int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int clientId, int id, CancellationToken ct);
    Task<bool> DeleteAsync(int clientId, int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(int clientId, IFormFile? file, CancellationToken ct);
}
