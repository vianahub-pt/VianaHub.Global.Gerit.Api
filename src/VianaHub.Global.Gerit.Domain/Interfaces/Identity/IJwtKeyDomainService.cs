using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Identity;

public interface IJwtKeyDomainService
{
    Task<JwtKeysEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<JwtKeysEntity?> GetByKeyIdAsync(Guid keyId, CancellationToken ct);
    Task<JwtKeysEntity?> GetActiveKeyAsync(int tenantId, CancellationToken ct);
    Task<IEnumerable<JwtKeysEntity>> GetAllAsync(CancellationToken ct);
    Task<IEnumerable<JwtKeysEntity>> GetByTenantAsync(int tenantId, CancellationToken ct);
    Task<JwtKeysEntity?> CreateAsync(JwtKeysEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(JwtKeysEntity key, CancellationToken ct);
    Task<bool> DeactivateAsync(JwtKeysEntity key, CancellationToken ct);
    Task<bool> RevokeAsync(int id, string reason, int modifiedBy, CancellationToken ct);
    Task<bool> DeleteAsync(JwtKeysEntity key, CancellationToken ct);
    Task<bool> UpdateRotationPolicyAsync(int id, int rotationPolicyDays, int overlapPeriodDays, int modifiedBy, CancellationToken ct);
    Task<(string PublicKey, string PrivateKeyEncrypted)> GenerateKeyPairAsync(string? algorithm, int keySize, CancellationToken ct);
    Task<string> DecryptPrivateKeyAsync(string encryptedPrivateKey, CancellationToken ct);
    Task<int> RotateKeysAsync(CancellationToken ct);
    Task<int> CleanupExpiredKeysAsync(int retentionDays, CancellationToken ct);
    Task<JwtKeysEntity?> EnsureKeyExistsAsync(int tenantId, int createdBy, CancellationToken ct);
}
