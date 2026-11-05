using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IClientFiscalDataDataRepository
{
    Task<IEnumerable<ClientFiscalDataEntity>> GetAllAsync(int clientId, CancellationToken ct);
    Task<ClientFiscalDataEntity?> GetByIdAsync(int clientId, int id, CancellationToken ct = default);
    Task<ListPage<ClientFiscalDataEntity>> GetPagedAsync(int clientId, PagedFilter filter, CancellationToken ct = default);
    Task<bool> ExistsByIdAsync(int clientId, CancellationToken ct = default);
    Task<bool> ExistsByTaxIdentityAsync(int tenantId, string fiscalCountry, string taxNumber, int? excludeId, CancellationToken ct = default);
    Task<bool> CreateAsync(ClientFiscalDataEntity entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(ClientFiscalDataEntity entity, CancellationToken ct = default);
}
