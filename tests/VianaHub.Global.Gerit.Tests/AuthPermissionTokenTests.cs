using Microsoft.IdentityModel.JsonWebTokens;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using VianaHub.Global.Gerit.Api.Filters;
using VianaHub.Global.Gerit.Application.Configuration;
using VianaHub.Global.Gerit.Application.Services.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Cryptography;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Tests.Application.Services.Identity;

public class AuthPermissionTokenTests
{
    [Fact]
    public async Task GeneratedRsaToken_PreservesSemanticPermissionsAndAuthorizesEndpoint()
    {
        var user = new UserEntity(7, "Operator", "operator@example.test", "unused", null, 1);
        var role = new RoleEntity(7, "Admin", "Administrator", 1);
        var action = new ActionEntity("GetBy", "Read", 1);
        var resource = new ResourceEntity("Actions", "Actions", 1);
        typeof(UserEntity).GetProperty("Id")!.SetValue(user, 10);
        typeof(RoleEntity).GetProperty("Id")!.SetValue(role, 20);
        typeof(ResourceEntity).GetProperty("Id")!.SetValue(resource, 30);
        typeof(ActionEntity).GetProperty("Id")!.SetValue(action, 40);
        var userRole = new UserRolesEntity(7, 10, 20);
        typeof(UserRolesEntity).GetProperty("Role")!.SetValue(userRole, role);
        var permission = new RolePermissionsEntity(7, 20, 30, 40);
        typeof(RolePermissionsEntity).GetProperty("Resource")!.SetValue(permission, resource);
        typeof(RolePermissionsEntity).GetProperty("Action")!.SetValue(permission, action);
        // Atualizações de apresentação não mudam os identificadores usados no token.
        role.Update("Updated administrator", 1);
        resource.Update("Updated resource", 1);
        action.Update("Updated action", 1);

        var userRoles = new Mock<IUserRoleDataRepository>();
        userRoles.Setup(repo => repo.GetByUserAsync(7, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRolesEntity> { userRole });
        var permissions = new Mock<IRolePermissionDataRepository>();
        permissions.Setup(repo => repo.GetByRoleAsync(20, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RolePermissionsEntity> { permission });
        var keyPair = CryptoRSA.GenerateKeyPair();
        var ephemeralMasterKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var key = new JwtKeysEntity(7, keyPair.PublicKeyPem,
            CryptoRSA.EncryptPrivateKey(keyPair.PrivateKeyPem, ephemeralMasterKey), 1);
        var keys = new Mock<IJwtKeyDataRepository>();
        keys.Setup(repo => repo.GetActiveKeyAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(key);
        var secrets = new Mock<ISecretProvider>();
        secrets.Setup(provider => provider.GetMasterKey()).Returns(ephemeralMasterKey);
        var notify = new Notify();
        await using var database = new GeritDbContext(new DbContextOptionsBuilder<GeritDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var settings = new JwtSettings { Issuer = "test-issuer", Audience = "test-audience" };
        var service = new AuthAppService(notify, Mock.Of<IMapper>(), permissions.Object,
            Mock.Of<IRefreshTokenDataRepository>(), Mock.Of<IUserDataRepository>(), userRoles.Object,
            Mock.Of<ITenantDataRepository>(), Mock.Of<IEmailSender>(), Mock.Of<ILocalizationService>(),
            Mock.Of<ICurrentUserService>(), Options.Create(settings), NullLogger<AuthAppService>.Instance,
            new ConfigurationBuilder().Build(), keys.Object, database, secrets.Object,
            Mock.Of<ISubscriptionDomainService>(), Mock.Of<IRequestTenantContext>());
        // Exercita a emissão real sem acoplar a prova às regras alheias de login/subscrição.
        var method = typeof(AuthAppService).GetMethod("GenerateAccessTokenAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var operation = (Task<(string Token, DateTime ExpiresAt)>)method.Invoke(service,
            new object[] { user, false, CancellationToken.None })!;
        var issued = await operation;
        Assert.False(notify.HasNotify());
        Assert.False(string.IsNullOrWhiteSpace(issued.Token));

        using var rsa = RSA.Create();
        rsa.ImportFromPem(keyPair.PublicKeyPem);
        var handler = new JsonWebTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 }
        };
        var validation = await handler.ValidateTokenAsync(issued.Token, validationParameters);
        // Ambos os leitores devem preservar e validar audiência, validade e assinatura.
        var legacyHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var legacyPrincipal = legacyHandler.ValidateToken(issued.Token, validationParameters, out var legacyToken);
        Assert.True(legacyPrincipal.Identity?.IsAuthenticated);
        Assert.Equal(settings.Audience, Assert.Single(Assert.IsType<System.IdentityModel.Tokens.Jwt.JwtSecurityToken>(legacyToken).Audiences));
        Assert.True(legacyToken.ValidTo > DateTime.UtcNow);
        Assert.True(validation.IsValid, validation.Exception?.ToString());
        var principal = new ClaimsPrincipal(validation.ClaimsIdentity);
        var jwt = Assert.IsType<JsonWebToken>(validation.SecurityToken);
        Assert.Equal(SecurityAlgorithms.RsaSha256, jwt.Alg);
        using var payload = JsonDocument.Parse(Base64UrlEncoder.Decode(jwt.EncodedPayload));
        Assert.Equal("getby", payload.RootElement.GetProperty("permissions").GetProperty("actions")[0].GetString());
        Assert.Contains(principal.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "admin");
        Assert.Contains(jwt.Claims, claim => claim.Type == "role" && claim.Value == "admin");
        var context = new DefaultHttpContext { User = principal };
        var filter = new AuthorizationFilter("Admin", "Actions", "GetBy",
            NullLogger<AuthorizationFilter>.Instance, Mock.Of<ILocalizationService>());
        var result = await filter.InvokeAsync(new DefaultEndpointFilterInvocationContext(context),
            _ => ValueTask.FromResult<object?>("authorized"));
        Assert.Equal("authorized", result);
        permissions.Verify(repo => repo.GetByRoleAsync(20, 7, It.IsAny<CancellationToken>()), Times.Once);
    }
}
