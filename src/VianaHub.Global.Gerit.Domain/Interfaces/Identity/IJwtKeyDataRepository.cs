using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Identity;

public interface IJwtKeyDataRepository
{
    Task<JwtKeysEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<JwtKeysEntity?> GetByKeyIdAsync(Guid keyId, CancellationToken ct);
    Task<JwtKeysEntity?> GetActiveKeyAsync(int tenantId, CancellationToken ct);
    Task<IEnumerable<JwtKeysEntity>> GetAllAsync(CancellationToken ct);
    Task<IEnumerable<JwtKeysEntity>> GetByTenantAsync(int tenantId, CancellationToken ct);
    Task<IEnumerable<JwtKeysEntity>> GetByApplicationAsync(int tenantId, CancellationToken ct);
    Task<IEnumerable<JwtKeysEntity>> GetKeysEligibleForRotationAsync(CancellationToken ct);
    Task<IEnumerable<JwtKeysEntity>> GetExpiredKeysAsync(int retentionDays, CancellationToken ct);
    Task<ListPage<JwtKeysEntity>> GetPagedAsync(PagedFilter request, int tenantId, CancellationToken ct);
    Task<bool> HasActiveKeyAsync(int tenantId, CancellationToken ct);
    Task<bool> AddAsync(JwtKeysEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(JwtKeysEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(JwtKeysEntity entity, CancellationToken ct);
    Task<int> BulkUpdateTelemetryAsync(List<(int Id, long UsageCount, DateTime? LastUsedAt, long ValidationCount, DateTime? LastValidatedAt)> updates, CancellationToken ct);
}
