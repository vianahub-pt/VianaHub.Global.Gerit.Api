using System.Security.Claims;
using System.Security.Cryptography;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moq;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Services.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Cryptography;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Tests;

public class JwtKeyDomainLifecycleTests
{
    [Theory]
    [InlineData("expired")]
    [InlineData("revoked")]
    [InlineData("deleted")]
    public void InvalidKey_CannotSignOrValidate(string state)
    {
        var key = NewKey();
        if (state == "expired") SetProperty(key, nameof(key.ExpiresAt), DateTime.UtcNow.AddMinutes(-1));
        if (state == "revoked") key.Revoke("test", 1);
        if (state == "deleted") key.Delete(1);

        Assert.False(key.IsValidForSigning());
        Assert.False(key.IsValidForValidation());
    }

    [Fact]
    public void RotationSchedule_UsesActivationAndBecomesEligibleBeforeExpiry()
    {
        var key = NewKey();
        Assert.Equal(key.ActivatedAt!.Value.AddDays(83), key.NextRotationAt);
        Assert.Equal(key.ActivatedAt.Value.AddDays(90), key.ExpiresAt);
        Assert.False(key.IsEligibleForRotation());

        SetProperty(key, nameof(key.NextRotationAt), DateTime.UtcNow);

        Assert.True(key.IsEligibleForRotation());
        Assert.True(key.IsValidForSigning());
        Assert.True(key.IsValidForValidation());
        key.Deactivate(1);
        Assert.False(key.IsEligibleForRotation());
    }

    [Fact]
    public void ExpirationBoundary_CannotSignOrValidate()
    {
        var key = NewKey();
        SetProperty(key, nameof(key.ExpiresAt), DateTime.UtcNow);

        Assert.True(key.IsExpired());
        Assert.False(key.IsValidForSigning());
        Assert.False(key.IsValidForValidation());
    }

    [Fact]
    public async Task Ensure_ExpiredActiveKeyDoesNotBlockNewValidKeyOrEraseHistory()
    {
        var expired = NewKey();
        var expiry = DateTime.UtcNow.AddDays(-1);
        SetProperty(expired, nameof(expired.ExpiresAt), expiry);
        var fixture = new Fixture(expired);

        var result = await fixture.Service.EnsureKeyExistsAsync(1, 1, default);

        Assert.NotNull(result);
        Assert.True(result.IsValidForSigning());
        Assert.Equal(1, result.TenantId);
        Assert.NotEqual(expired.KeyId, result.KeyId);
        Assert.Equal(2, fixture.Keys.Count);
        Assert.Contains(expired, fixture.Keys);
        Assert.Equal(expiry, expired.ExpiresAt);
        Assert.False(expired.IsActive);
        Assert.False(expired.IsDeleted);
        Assert.False(fixture.Notify.HasNotify());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ensure_DefensivelyRejectsInvalidOrForeignRepositoryResult(bool foreignTenant)
    {
        var invalid = NewKey(foreignTenant ? 2 : 1);
        if (!foreignTenant) SetProperty(invalid, nameof(invalid.ExpiresAt), DateTime.UtcNow.AddDays(-1));
        var fixture = new Fixture(invalid);
        fixture.Repository.Setup(r => r.GetActiveKeyAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(invalid);

        var result = await fixture.Service.EnsureKeyExistsAsync(1, 1, default);

        Assert.NotNull(result);
        Assert.Equal(1, result.TenantId);
        Assert.True(result.IsValidForSigning());
        Assert.NotEqual(invalid.KeyId, result.KeyId);
    }

    [Fact]
    public async Task Ensure_ValidSigningKeyIsReused()
    {
        var key = NewKey();
        var fixture = new Fixture(key);

        Assert.Same(key, await fixture.Service.EnsureKeyExistsAsync(1, 1, default));

        fixture.Repository.Verify(r => r.ReplaceSigningKeyAsync(It.IsAny<JwtKeysEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Rotation_NewKeySignsWhileOldKeyKeepsValidatingExistingTokenUntilOriginalExpiry()
    {
        var pair = CryptoRSA.GenerateKeyPair();
        var oldKey = new JwtKeysEntity(1, pair.PublicKeyPem, "unused", 1);
        SetProperty(oldKey, nameof(oldKey.ActivatedAt), DateTime.UtcNow.AddDays(-84));
        SetProperty(oldKey, nameof(oldKey.NextRotationAt), DateTime.UtcNow.AddDays(-1));
        var oldExpiry = DateTime.UtcNow.AddDays(6);
        SetProperty(oldKey, nameof(oldKey.ExpiresAt), oldExpiry);
        var oldToken = CreateToken(oldKey, pair.PrivateKeyPem);
        var fixture = new Fixture(oldKey);

        Assert.Equal(1, await fixture.Service.RotateKeysAsync(default));

        var newKey = await fixture.Repository.Object.GetActiveKeyAsync(1, default);
        Assert.NotNull(newKey);
        Assert.NotEqual(oldKey.KeyId, newKey.KeyId);
        Assert.True(newKey.IsValidForSigning());
        Assert.Equal(newKey.ActivatedAt!.Value.AddDays(90), newKey.ExpiresAt);
        Assert.Equal(newKey.ActivatedAt.Value.AddDays(83), newKey.NextRotationAt);
        Assert.False(oldKey.IsValidForSigning());
        Assert.True(oldKey.IsValidForValidation());
        Assert.Equal(oldExpiry, oldKey.ExpiresAt);
        Assert.Equal(90, oldKey.RotationPolicyDays);
        Assert.Equal(7, oldKey.OverlapPeriodDays);
        Assert.False(oldKey.IsDeleted);
        Assert.False(oldKey.IsRevoked());
        fixture.Repository.Verify(r => r.UpdateAsync(oldKey, It.IsAny<CancellationToken>()), Times.Never);
        Assert.True(await ValidateToken(oldToken, oldKey));
        var newPrivateKey = CryptoRSA.DecryptPrivateKey(newKey.PrivateKeyEncrypted, fixture.MasterKey);
        var newToken = CreateToken(newKey, newPrivateKey);
        Assert.True(await ValidateToken(newToken, newKey));

        SetProperty(oldKey, nameof(oldKey.ExpiresAt), DateTime.UtcNow.AddTicks(-1));
        Assert.False(oldKey.IsValidForValidation());
    }

    [Fact]
    public async Task Rotation_PersistenceFailureLeavesOldSigningKeyAndExpiryUntouched()
    {
        var oldKey = NewKey();
        SetProperty(oldKey, nameof(oldKey.NextRotationAt), DateTime.UtcNow.AddMinutes(-1));
        var oldExpiry = oldKey.ExpiresAt;
        var fixture = new Fixture(oldKey);
        fixture.Repository.Setup(r => r.ReplaceSigningKeyAsync(It.IsAny<JwtKeysEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Assert.Equal(0, await fixture.Service.RotateKeysAsync(default));

        Assert.True(oldKey.IsValidForSigning());
        Assert.Equal(oldExpiry, oldKey.ExpiresAt);
        Assert.True(fixture.Notify.HasNotify());
        fixture.Repository.Verify(r => r.UpdateAsync(oldKey, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Rotation_ExistingSuccessorPreventsDuplicateReplacementOfExpiredActiveKey()
    {
        var expired = NewKey();
        SetProperty(expired, nameof(expired.NextRotationAt), DateTime.UtcNow.AddDays(-2));
        SetProperty(expired, nameof(expired.ExpiresAt), DateTime.UtcNow.AddDays(-1));
        var expiry = expired.ExpiresAt;
        var successor = NewKey();
        var fixture = new Fixture(expired, successor);

        Assert.Equal(0, await fixture.Service.RotateKeysAsync(default));

        Assert.Same(successor, await fixture.Repository.Object.GetActiveKeyAsync(1, default));
        Assert.False(expired.IsActive);
        Assert.Equal(expiry, expired.ExpiresAt);
        Assert.False(expired.IsValidForValidation());
        Assert.False(expired.IsDeleted);
        fixture.Repository.Verify(r => r.ReplaceSigningKeyAsync(It.IsAny<JwtKeysEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cleanup_SoftDeletesOnlyInactiveExpiredKeysBeyondRetention()
    {
        var eligible = NewKey();
        eligible.Deactivate(1);
        SetProperty(eligible, nameof(eligible.ExpiresAt), DateTime.UtcNow.AddDays(-60));
        var activeExpired = NewKey();
        SetProperty(activeExpired, nameof(activeExpired.ExpiresAt), DateTime.UtcNow.AddDays(-60));
        var recentExpired = NewKey();
        recentExpired.Deactivate(1);
        SetProperty(recentExpired, nameof(recentExpired.ExpiresAt), DateTime.UtcNow.AddDays(-1));
        var overlap = NewKey();
        overlap.Deactivate(1);
        var deleted = NewKey();
        deleted.Delete(1);
        SetProperty(deleted, nameof(deleted.ExpiresAt), DateTime.UtcNow.AddDays(-60));
        var candidates = new[] { eligible, activeExpired, recentExpired, overlap, deleted };
        var fixture = new Fixture(candidates);
        fixture.Repository.Setup(r => r.GetExpiredKeysAsync(30, It.IsAny<CancellationToken>())).ReturnsAsync(candidates);

        Assert.Equal(1, await fixture.Service.CleanupExpiredKeysAsync(30, default));

        Assert.True(eligible.IsDeleted);
        Assert.False(activeExpired.IsDeleted);
        Assert.False(recentExpired.IsDeleted);
        Assert.False(overlap.IsDeleted);
        Assert.True(overlap.IsValidForValidation());
        fixture.Repository.Verify(r => r.UpdateAsync(eligible, It.IsAny<CancellationToken>()), Times.Once);
        fixture.Repository.Verify(r => r.UpdateAsync(It.IsAny<JwtKeysEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cleanup_PersistenceFailureDoesNotCountAsDeletion()
    {
        var key = NewKey();
        key.Deactivate(1);
        SetProperty(key, nameof(key.ExpiresAt), DateTime.UtcNow.AddDays(-60));
        var fixture = new Fixture(key);
        fixture.Repository.Setup(r => r.GetExpiredKeysAsync(30, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { key });
        fixture.Repository.Setup(r => r.UpdateAsync(key, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Assert.Equal(0, await fixture.Service.CleanupExpiredKeysAsync(30, default));
    }

    [Fact]
    public async Task Create_UsesAtomicSigningKeyReplacementWithoutDeletingExpiredHistory()
    {
        var expired = NewKey();
        SetProperty(expired, nameof(expired.ExpiresAt), DateTime.UtcNow.AddDays(-1));
        var expiry = expired.ExpiresAt;
        var fixture = new Fixture(expired);
        var candidate = NewKey();

        Assert.Same(candidate, await fixture.Service.CreateAsync(candidate, default));

        Assert.False(expired.IsActive);
        Assert.False(expired.IsDeleted);
        Assert.Equal(expiry, expired.ExpiresAt);
        Assert.True(candidate.IsValidForSigning());
        fixture.Repository.Verify(r => r.ReplaceSigningKeyAsync(candidate, It.IsAny<CancellationToken>()), Times.Once);
        fixture.Repository.Verify(r => r.AddAsync(It.IsAny<JwtKeysEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Revoke_OnlySigningKeyIsAtomicallyReplacedBeforeRevocation()
    {
        var key = NewKey();
        var fixture = new Fixture(key);

        Assert.True(await fixture.Service.RevokeAsync(key.Id, "test", 1, default));

        Assert.True(key.IsRevoked());
        Assert.False(key.IsValidForValidation());
        var replacement = await fixture.Repository.Object.GetActiveKeyAsync(1, default);
        Assert.NotNull(replacement);
        Assert.NotEqual(key.KeyId, replacement.KeyId);
        Assert.True(replacement.IsValidForSigning());
        Assert.False(key.IsDeleted);
        fixture.Repository.Verify(r => r.ReplaceSigningKeyAsync(It.IsAny<JwtKeysEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        fixture.Repository.Verify(r => r.AddAsync(It.IsAny<JwtKeysEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Revoke_ReplacementFailureDoesNotRevokeOnlySigningKey()
    {
        var key = NewKey();
        var fixture = new Fixture(key);
        fixture.Repository.Setup(r => r.ReplaceSigningKeyAsync(It.IsAny<JwtKeysEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Assert.False(await fixture.Service.RevokeAsync(key.Id, "test", 1, default));

        Assert.True(key.IsValidForSigning());
        Assert.False(key.IsRevoked());
        fixture.Repository.Verify(r => r.UpdateAsync(key, It.IsAny<CancellationToken>()), Times.Never);
    }

    private static JwtKeysEntity NewKey(int tenantId = 1) => new(tenantId, "public", "encrypted", 1);

    private static void SetProperty(JwtKeysEntity key, string name, object value) =>
        typeof(JwtKeysEntity).GetProperty(name)!.SetValue(key, value);

    private static string CreateToken(JwtKeysEntity key, string privateKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKey);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "lifecycle-tests",
            Audience = "lifecycle-client",
            Subject = new ClaimsIdentity(new[] { new Claim("tenant_id", key.TenantId.ToString()) }),
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow.AddSeconds(-1),
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa) { KeyId = key.KeyId.ToString() }, SecurityAlgorithms.RsaSha256)
        });
    }

    private static async Task<bool> ValidateToken(string token, JwtKeysEntity key)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(key.PublicKey!);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = "lifecycle-tests",
            ValidAudience = "lifecycle-client",
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            IssuerSigningKey = new RsaSecurityKey(rsa) { KeyId = key.KeyId.ToString() },
            ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 }
        });
        return result.IsValid;
    }

    private sealed class Fixture
    {
        public List<JwtKeysEntity> Keys { get; }
        public Mock<IJwtKeyDataRepository> Repository { get; } = new();
        public Notify Notify { get; } = new();
        public string MasterKey { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        public JwtKeyDomainService Service { get; }

        public Fixture(params JwtKeysEntity[] keys)
        {
            Keys = keys.ToList();
            for (var index = 0; index < Keys.Count; index++) SetProperty(Keys[index], nameof(JwtKeysEntity.Id), index + 1);
            Repository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken _) => Keys.SingleOrDefault(k => k.Id == id));
            Repository.Setup(r => r.GetActiveKeyAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int tenantId, CancellationToken _) => Keys.LastOrDefault(k => k.TenantId == tenantId && k.IsValidForSigning()));
            Repository.Setup(r => r.HasActiveKeyAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int tenantId, CancellationToken _) => Keys.Any(k => k.TenantId == tenantId && k.IsValidForSigning()));
            Repository.Setup(r => r.GetKeysEligibleForRotationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Keys.Where(k => k.IsEligibleForRotation()).ToList());
            Repository.Setup(r => r.ReplaceSigningKeyAsync(It.IsAny<JwtKeysEntity>(), It.IsAny<CancellationToken>()))
                .Callback<JwtKeysEntity, CancellationToken>((key, _) =>
                {
                    foreach (var previous in Keys.Where(k => k.TenantId == key.TenantId && k.IsActive && !k.IsDeleted)) previous.Deactivate(0);
                    SetProperty(key, nameof(JwtKeysEntity.Id), Keys.Count + 1);
                    Keys.Add(key);
                }).ReturnsAsync(true);
            Repository.Setup(r => r.UpdateAsync(It.IsAny<JwtKeysEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var tenants = new Mock<ITenantDataRepository>();
            tenants.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenantEntity(1, 1, "Tenant", "tenant@example.test", null, null, null, 1));
            var validator = new Mock<IEntityDomainValidator<JwtKeysEntity>>();
            validator.Setup(v => v.ValidateForCreateAsync(It.IsAny<JwtKeysEntity>())).ReturnsAsync(new ValidationResult());
            validator.Setup(v => v.ValidateForRevokeAsync(It.IsAny<JwtKeysEntity>())).ReturnsAsync(new ValidationResult());
            var localization = new Mock<ILocalizationService>();
            localization.Setup(l => l.GetMessage(It.IsAny<string>())).Returns((string key) => key);
            var secrets = new Mock<ISecretProvider>();
            secrets.Setup(s => s.GetMasterKey()).Returns(MasterKey);
            Service = new JwtKeyDomainService(Repository.Object, tenants.Object, Notify, validator.Object,
                localization.Object, Mock.Of<ICurrentUserService>(), NullLogger<JwtKeyDomainService>.Instance, secrets.Object);
        }
    }
}
