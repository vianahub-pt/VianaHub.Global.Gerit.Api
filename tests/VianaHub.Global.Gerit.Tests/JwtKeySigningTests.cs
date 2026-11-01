using System.Reflection;
using System.Security.Cryptography;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moq;
using VianaHub.Global.Gerit.Application.Configuration;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Auth;
using VianaHub.Global.Gerit.Application.Services.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Cryptography;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Repository.Identity;

namespace VianaHub.Global.Gerit.Tests;

public class JwtKeySigningTests
{
    [Theory]
    [InlineData("valid", true)]
    [InlineData("expired", false)]
    [InlineData("boundary", false)]
    [InlineData("revoked", false)]
    [InlineData("deleted", false)]
    [InlineData("inactive", false)]
    [InlineData("foreign", false)]
    public async Task SigningRepository_UsesTenantAndDomainValidity(string state, bool expected)
    {
        await using var db = CreateDatabase();
        var key = new JwtKeysEntity(state == "foreign" ? 8 : 7, "public", "encrypted", 1);
        ChangeState(key, state);
        db.Add(key);
        await db.SaveChangesAsync();
        var repository = new JwtKeyDataRepository(db, NullLogger<JwtKeyDataRepository>.Instance);

        Assert.Equal(expected, await repository.HasActiveKeyAsync(7, default));
        var selected = await repository.GetActiveKeyAsync(7, default);
        Assert.Equal(expected, selected is not null);
        if (selected is not null) Assert.True(selected.IsValidForSigning());
    }

    [Fact]
    public async Task SigningRepository_SelectsNewestValidKeyDeterministically()
    {
        await using var db = CreateDatabase();
        var older = new JwtKeysEntity(7, "public", "encrypted", 1);
        Set(older, nameof(older.ActivatedAt), DateTime.UtcNow.AddDays(-1));
        var newer = new JwtKeysEntity(7, "public", "encrypted", 1);
        db.AddRange(older, newer);
        await db.SaveChangesAsync();

        var selected = await new JwtKeyDataRepository(db, NullLogger<JwtKeyDataRepository>.Instance).GetActiveKeyAsync(7, default);

        Assert.Equal(newer.KeyId, selected!.KeyId);
    }

    [Fact]
    public async Task Replacement_UsesRealRepositoryAndPreservesHistoryAndOtherTenants()
    {
        await using var db = CreateDatabase();
        var old = new JwtKeysEntity(7, "public-old", "encrypted-old", 1);
        var foreign = new JwtKeysEntity(8, "public-foreign", "encrypted-foreign", 1);
        var expiry = old.ExpiresAt;
        db.AddRange(old, foreign);
        await db.SaveChangesAsync();
        var replacement = new JwtKeysEntity(7, "public-new", "encrypted-new", 1);
        var repository = new JwtKeyDataRepository(db, NullLogger<JwtKeyDataRepository>.Instance);

        Assert.True(await repository.ReplaceSigningKeyAsync(replacement, default));
        Assert.True(await repository.ReplaceSigningKeyAsync(replacement, default));

        var history = (await repository.GetByTenantAsync(7, default)).ToList();
        Assert.Equal(2, history.Count);
        Assert.Equal(replacement.KeyId, Assert.Single(history.Where(k => k.IsActive)).KeyId);
        var previous = Assert.Single(history.Where(k => k.KeyId == old.KeyId));
        Assert.Equal(expiry, previous.ExpiresAt);
        Assert.True(previous.IsValidForValidation());
        Assert.False(previous.IsDeleted);
        Assert.True((await repository.GetActiveKeyAsync(8, default))!.IsValidForSigning());
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("revoked")]
    [InlineData("deleted")]
    [InlineData("inactive")]
    [InlineData("foreign")]
    [InlineData("near-expiry")]
    public async Task Emission_RefusesInvalidOrInsufficientSigningKey(string state)
    {
        await using var fixture = new AuthFixture();
        ChangeState(fixture.Key, state);

        var issued = await fixture.Generate();

        Assert.Null(issued.Token);
        Assert.True(fixture.Notify.HasNotify());
        fixture.Secrets.Verify(x => x.GetMasterKey(), Times.Never);
    }

    [Fact]
    public async Task Emission_PreservesConfiguredLifetimeAndProducesVerifiableSignature()
    {
        await using var fixture = new AuthFixture();
        var started = DateTime.UtcNow;

        var issued = await fixture.Generate();

        Assert.NotNull(issued.Token);
        Assert.False(fixture.Notify.HasNotify());
        Assert.InRange(issued.ExpiresAt, started.AddMinutes(60), DateTime.UtcNow.AddMinutes(60));
        Assert.True(issued.ExpiresAt <= fixture.Key.ExpiresAt);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(fixture.Key.PublicKey);
        var validated = await new JsonWebTokenHandler().ValidateTokenAsync(issued.Token, new TokenValidationParameters
        {
            ValidIssuer = fixture.Settings.Issuer,
            ValidAudience = fixture.Settings.Audience,
            IssuerSigningKey = new RsaSecurityKey(rsa) { KeyId = fixture.Key.KeyId.ToString() },
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        });
        Assert.True(validated.IsValid);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Refresh_UsesTenantIdForEverySubscriptionCheck(bool trial)
    {
        await using var fixture = new AuthFixture();
        fixture.Subscriptions.Setup(x => x.IsTrialAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(trial);

        var response = await fixture.Service.RefreshAsync(new RefreshRequest { TenantId = 7, RefreshToken = "opaque-refresh" }, default);

        Assert.NotNull(response?.AccessToken);
        fixture.Subscriptions.Verify(x => x.IsTrialAsync(7, It.IsAny<CancellationToken>()), Times.Once);
        fixture.Subscriptions.Verify(x => x.IsTrialPeriodExpiredAsync(7, It.IsAny<CancellationToken>()), trial ? Times.Once() : Times.Never());
        fixture.Subscriptions.Verify(x => x.IsSubscriptionPeriodExpiredAsync(7, It.IsAny<CancellationToken>()), trial ? Times.Never() : Times.Once());
        fixture.Subscriptions.Verify(x => x.IsTrialAsync(42, It.IsAny<CancellationToken>()), Times.Never);
        fixture.Subscriptions.Verify(x => x.IsTrialPeriodExpiredAsync(42, It.IsAny<CancellationToken>()), Times.Never);
        fixture.Subscriptions.Verify(x => x.IsSubscriptionPeriodExpiredAsync(42, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_DoesNotRotateRefreshTokenWhenSigningFails()
    {
        await using var fixture = new AuthFixture();
        ChangeState(fixture.Key, "expired");

        var response = await fixture.Service.RefreshAsync(new RefreshRequest { TenantId = 7, RefreshToken = "opaque-refresh" }, default);

        Assert.Null(response);
        fixture.Refresh.Verify(x => x.AddAsync(It.IsAny<RefreshTokensEntity>()), Times.Never);
        fixture.Refresh.Verify(x => x.RevokeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_RejectsTokenFromAnotherTenantBeforeSubscriptionOrSigning()
    {
        await using var fixture = new AuthFixture();
        fixture.Refresh.Setup(x => x.GetByTokenAsync("opaque-refresh", 7))
            .ReturnsAsync(new RefreshTokensEntity(8, 42, "opaque-refresh", DateTime.UtcNow.AddDays(1), 42));

        Assert.Null(await fixture.Service.RefreshAsync(new RefreshRequest { TenantId = 7, RefreshToken = "opaque-refresh" }, default));

        fixture.Subscriptions.Verify(x => x.IsActiveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Secrets.Verify(x => x.GetMasterKey(), Times.Never);
        fixture.Refresh.Verify(x => x.AddAsync(It.IsAny<RefreshTokensEntity>()), Times.Never);
    }

    private static GeritDbContext CreateDatabase() => new(new DbContextOptionsBuilder<GeritDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static void Set(object entity, string property, object value) => entity.GetType().GetProperty(property)!.SetValue(entity, value);

    private static void ChangeState(JwtKeysEntity key, string state)
    {
        switch (state)
        {
            case "expired": Set(key, nameof(key.ExpiresAt), DateTime.UtcNow.AddMinutes(-1)); break;
            case "boundary": Set(key, nameof(key.ExpiresAt), DateTime.UtcNow); break;
            case "near-expiry": Set(key, nameof(key.ExpiresAt), DateTime.UtcNow.AddMinutes(5)); break;
            case "revoked": key.Revoke("test", 1); break;
            case "deleted": key.Delete(1); break;
            case "inactive": key.Deactivate(1); break;
            case "foreign": Set(key, nameof(key.TenantId), 8); break;
        }
    }

    private sealed class AuthFixture : IAsyncDisposable
    {
        public readonly GeritDbContext Db = CreateDatabase();
        public readonly JwtSettings Settings = new() { Issuer = "test-issuer", Audience = "test-audience", AccessTokenExpirationMinutes = 60 };
        public readonly Notify Notify = new();
        public readonly Mock<ISecretProvider> Secrets = new();
        public readonly Mock<ISubscriptionDomainService> Subscriptions = new();
        public readonly Mock<IRefreshTokenDataRepository> Refresh = new();
        public readonly JwtKeysEntity Key;
        public readonly UserEntity User;
        public readonly AuthAppService Service;

        public AuthFixture()
        {
            var pair = CryptoRSA.GenerateKeyPair();
            var master = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            Key = new JwtKeysEntity(7, pair.PublicKeyPem, CryptoRSA.EncryptPrivateKey(pair.PrivateKeyPem, master), 1);
            Secrets.Setup(x => x.GetMasterKey()).Returns(master);
            var keys = new Mock<IJwtKeyDataRepository>();
            keys.Setup(x => x.GetActiveKeyAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(() => Key);
            User = new UserEntity(7, "User", "test@example.test", "unused", null, 1);
            Set(User, nameof(User.Id), 42);
            var users = new Mock<IUserDataRepository>();
            users.Setup(x => x.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(User);
            Refresh.Setup(x => x.GetByTokenAsync("opaque-refresh", 7)).ReturnsAsync(new RefreshTokensEntity(7, 42, "opaque-refresh", DateTime.UtcNow.AddDays(1), 42));
            Subscriptions.Setup(x => x.IsActiveAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var localization = new Mock<ILocalizationService>();
            localization.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
            Service = new AuthAppService(Notify, Mock.Of<IMapper>(), Mock.Of<IRolePermissionDataRepository>(), Refresh.Object,
                users.Object, Mock.Of<IUserRoleDataRepository>(), Mock.Of<ITenantDataRepository>(), Mock.Of<IEmailSender>(),
                localization.Object, Mock.Of<ICurrentUserService>(), Options.Create(Settings), NullLogger<AuthAppService>.Instance,
                new ConfigurationBuilder().Build(), keys.Object, Db, Secrets.Object, Subscriptions.Object, Mock.Of<IRequestTenantContext>());
        }

        public Task<(string? Token, DateTime ExpiresAt)> Generate() =>
            (Task<(string? Token, DateTime ExpiresAt)>)typeof(AuthAppService)
                .GetMethod("GenerateAccessTokenAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(Service, [User, false, CancellationToken.None])!;

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
