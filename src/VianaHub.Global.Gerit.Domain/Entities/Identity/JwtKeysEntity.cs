using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Enums;

namespace VianaHub.Global.Gerit.Domain.Entities.Identity;

public class JwtKeysEntity : Entity
{
    public int TenantId { get; private set; }
    public Guid KeyId { get; private set; }
    public string? PublicKey { get; private set; }
    public string? PrivateKeyEncrypted { get; private set; }
    public string? Algorithm { get; private set; }
    public int KeySize { get; private set; }
    public string? KeyType { get; private set; }
    public string? RevokedReason { get; private set; }
    public long UsageCount { get; private set; }
    public DateTime? ActivatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public DateTime NextRotationAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime? LastValidatedAt { get; private set; }
    public long ValidationCount { get; private set; }
    public int RotationPolicyDays { get; private set; }
    public int OverlapPeriodDays { get; private set; }
    public int MaxTokenLifetimeMinutes { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsDeleted { get; private set; }

    // Propriedade temporária para retornar a chave privada descriptografada (nunca persiste)
    public string? PlainPrivateKey { get; private set; }

    private JwtKeysEntity()
    {
        KeyId = Guid.Empty;
        PublicKey = string.Empty;
        PrivateKeyEncrypted = string.Empty;
        Algorithm = "RS256";
        KeyType = "RSA";
        KeySize = 2048;
        RevokedReason = string.Empty;
        IsActive = true;
        PlainPrivateKey = string.Empty;
    }

    public JwtKeysEntity(
        int tenantId,
        string? publicKey,
        string? privateKeyEncrypted,
        int createdBy,
        string? algorithm = "RS256",
        int keySize = 2048,
        string? keyType = "RSA",
        int rotationPolicyDays = 90,
        int overlapPeriodDays = 7,
        int maxTokenLifetimeMinutes = 60) : base()
    {
        TenantId = tenantId;
        KeyId = Guid.NewGuid();
        PublicKey = publicKey;
        PrivateKeyEncrypted = privateKeyEncrypted;
        Algorithm = algorithm;
        KeySize = keySize;
        KeyType = keyType;
        RevokedReason = string.Empty;
        UsageCount = 0;

        // Set activation and schedule based on activation time
        ActivatedAt = DateTime.UtcNow;
        ExpiresAt = ActivatedAt.Value.AddDays(rotationPolicyDays);
        NextRotationAt = ActivatedAt.Value.AddDays(rotationPolicyDays - overlapPeriodDays);

        LastUsedAt = null;
        RevokedAt = null;
        LastValidatedAt = null;
        ValidationCount = 0;
        RotationPolicyDays = rotationPolicyDays;
        OverlapPeriodDays = overlapPeriodDays;
        MaxTokenLifetimeMinutes = maxTokenLifetimeMinutes;
        IsActive = true;
        IsDeleted = false;

        // Auditing fields
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        ModifiedBy = createdBy;
        ModifiedAt = CreatedAt;

        PlainPrivateKey = string.Empty;
    }

    public void Activate(int modifiedBy)
    {
        IsActive = true;
        ActivatedAt = DateTime.UtcNow;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Deactivate(int modifiedBy)
    {
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Revoke(string? reason, int modifiedBy)
    {
        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason;
        IsActive = false;
        ExpiresAt = DateTime.UtcNow;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Delete(int modifiedBy)
    {
        IsDeleted = true;
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void IncrementUsageCount()
    {
        UsageCount++;
        LastUsedAt = DateTime.UtcNow;
    }

    public void IncrementValidationCount()
    {
        ValidationCount++;
        LastValidatedAt = DateTime.UtcNow;
    }

    public void UpdateRotationSchedule(int rotationPolicyDays, int overlapPeriodDays, int modifiedBy)
    {
        RotationPolicyDays = rotationPolicyDays;
        OverlapPeriodDays = overlapPeriodDays;
        NextRotationAt = ActivatedAt?.AddDays(rotationPolicyDays - overlapPeriodDays) ?? DateTime.UtcNow.AddDays(rotationPolicyDays - overlapPeriodDays);
        ExpiresAt = ActivatedAt?.AddDays(rotationPolicyDays) ?? DateTime.UtcNow.AddDays(rotationPolicyDays);
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void SetPlainPrivateKey(string? plainPrivateKey)
    {
        PlainPrivateKey = plainPrivateKey;
    }

    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAt;
    }

    public bool IsRevoked()
    {
        return RevokedAt.HasValue;
    }

    public bool IsEligibleForRotation()
    {
        return IsActive && !IsRevoked() && !IsDeleted && DateTime.UtcNow >= NextRotationAt;
    }

    public bool IsValidForSigning()
    {
        return IsActive && !IsRevoked() && !IsDeleted && !IsExpired();
    }

    public bool IsValidForValidation()
    {
        return !IsDeleted && !IsExpired() && (!IsRevoked() || RevokedAt > DateTime.UtcNow);
    }
}
