using VianaHub.Global.Gerit.Domain.Base;

namespace VianaHub.Global.Gerit.Domain.Entities.Identity;

/// <summary>
/// Refresh token persistido para permitir rota��o e revoga��o
/// </summary>
public class RefreshTokensEntity : Entity
{
    public int TenantId { get; private set; }
    public int UserId { get; private set; }
    public string? TokenHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public int? RevokedBy { get; private set; }

    protected RefreshTokensEntity() { }

    public RefreshTokensEntity(int tenantId, int userId, string? token, DateTime expiresAt, int createdBy)
    {
        if (tenantId <= 0) throw new ArgumentException("TenantId inv�lido", nameof(tenantId));
        if (userId <= 0) throw new ArgumentException("UserId inv�lido", nameof(userId));
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token inv�lido", nameof(token));

        TenantId = tenantId;
        UserId = userId;
        TokenHash = token;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public bool IsActive() => RevokedAt == null && DateTime.UtcNow < ExpiresAt;

    public void Revoke(int revokedBy)
    {
        RevokedAt = DateTime.UtcNow;
        RevokedBy = revokedBy;
        ModifiedBy = revokedBy;
        ModifiedAt = DateTime.UtcNow;
    }
}
