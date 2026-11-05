using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IClientDocumentDataRepository
{
    Task<ClientDocumentsEntity?> GetByIdAsync(int tenantId, int clientId, int id, CancellationToken ct);
    Task<IEnumerable<ClientDocumentsEntity>> GetAllAsync(int tenantId, int clientId, CancellationToken ct);
    Task<ListPage<ClientDocumentsEntity>> GetPagedAsync(int tenantId, int clientId, PagedFilter filter, CancellationToken ct);
    Task<bool> ExistsDuplicateAsync(int tenantId, int documentTypeId, string issuingCountryCode, string documentNumber, int? excludeId, CancellationToken ct);
    Task<ClientDocumentsEntity?> GetPrimaryAsync(int tenantId, int clientId, int? excludeId, CancellationToken ct);
    Task<bool> CreateAsync(ClientDocumentsEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(ClientDocumentsEntity entity, CancellationToken ct);
}
