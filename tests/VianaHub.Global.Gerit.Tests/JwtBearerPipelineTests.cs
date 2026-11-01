using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using VianaHub.Global.Gerit.Api.Configuration;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Configuration;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Auth;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Auth;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Application.Services.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Cryptography;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Infra.Data.Context;
using Xunit.Abstractions;

namespace VianaHub.Global.Gerit.Tests;

public class JwtBearerPipelineTests
{
    private readonly ITestOutputHelper _output;

    public JwtBearerPipelineTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task LoginThenProtectedGet_ShouldValidateRealBearerAndPassCustomAuthorization()
    {
        await using var host = await PipelineHost.StartAsync();
        using var login = await host.Client.PostAsJsonAsync("/v1/auth/login", new LoginRequest
        {
            Email = "operator@example.test",
            Password = PipelineHost.Password
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var session = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(session);
        Assert.False(string.IsNullOrWhiteSpace(session.AccessToken));

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        using var response = await host.Client.GetAsync("/v1/actions/1");

        _output.WriteLine($"HTTP {(int)response.StatusCode}; resolver calls={host.ResolverCalls}; token validated={host.TokenValidatedCalls}; custom authorization passed={host.CustomAuthorizationPassedCalls}; endpoint calls={host.EndpointCalls}");

        // Não inclui tokens, PEMs ou corpo da resposta nas mensagens de falha.
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"HTTP {(int)response.StatusCode}; resolver calls={host.ResolverCalls}; validation error type={host.ValidationErrorType ?? "none"}");
        Assert.Equal(1, host.TokenValidatedCalls);
        Assert.Equal(1, host.CustomAuthorizationPassedCalls);
        Assert.Equal(1, host.EndpointCalls);
        var result = await response.Content.ReadFromJsonAsync<ProtectedResponse>();
        Assert.NotNull(result);
        Assert.True(result.IsAuthenticated);
        Assert.Equal("7", result.TenantId);
    }

    [Fact]
    public async Task ProtectedGetWithoutBearer_ShouldBeRejectedBeforeEndpointFilters()
    {
        await using var host = await PipelineHost.StartAsync();

        using var response = await host.Client.GetAsync("/v1/actions/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, host.TokenValidatedCalls);
        Assert.Equal(0, host.CustomAuthorizationPassedCalls);
        Assert.Equal(0, host.EndpointCalls);
    }

    [Theory]
    [InlineData(8, false)]
    [InlineData(7, true)]
    public async Task ProtectedGet_ShouldRejectCrossTenantOrUnknownKidEvenWithKnownSigningMaterial(int tenantId, bool unknownKid)
    {
        await using var host = await PipelineHost.StartAsync();
        var token = host.CreateToken(tenantId, unknownKid ? Guid.NewGuid().ToString() : host.Key.KeyId.ToString());
        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await host.Client.GetAsync("/v1/actions/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, host.TokenValidatedCalls);
        Assert.Equal(0, host.CustomAuthorizationPassedCalls);
        Assert.Equal(0, host.EndpointCalls);
    }

    private sealed record ProtectedResponse(bool IsAuthenticated, string? TenantId);

    [Theory]
    [InlineData("expired", HttpStatusCode.Unauthorized)]
    [InlineData("revoked", HttpStatusCode.Unauthorized)]
    [InlineData("deleted", HttpStatusCode.Unauthorized)]
    [InlineData("inactive-overlap", HttpStatusCode.OK)]
    public async Task ProtectedGet_RespectsValidationLifecycle(string state, HttpStatusCode expected)
    {
        await using var host = await PipelineHost.StartAsync();
        var token = host.CreateToken(7, host.Key.KeyId.ToString());
        switch (state)
        {
            case "expired": typeof(JwtKeysEntity).GetProperty(nameof(JwtKeysEntity.ExpiresAt))!.SetValue(host.Key, DateTime.UtcNow.AddMinutes(-1)); break;
            case "revoked": host.Key.Revoke("test", 0); break;
            case "deleted": host.Key.Delete(0); break;
            default: host.Key.Deactivate(0); break;
        }
        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await host.Client.GetAsync("/v1/actions/1");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task ConcurrentRequests_KeepTenantAndKidIsolation()
    {
        await using var host = await PipelineHost.StartAsync();
        var valid = host.CreateToken(7, host.Key.KeyId.ToString());
        var foreign = host.CreateToken(8, host.Key.KeyId.ToString());
        var unknown = host.CreateToken(7, Guid.NewGuid().ToString());
        var statuses = await Task.WhenAll(Enumerable.Range(0, 12).Select(async index =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/actions/1");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", index % 3 == 0 ? valid : index % 3 == 1 ? foreign : unknown);
            using var response = await host.Client.SendAsync(request);
            return (index, response.StatusCode);
        }));

        foreach (var (index, status) in statuses)
            Assert.Equal(index % 3 == 0 ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, status);
    }

    private sealed class PipelineHost : IAsyncDisposable
    {
        public const string Password = "Only-for-pipeline-test-123!";
        private const string Issuer = "pipeline-test-issuer";
        private const string Audience = "pipeline-test-audience";
        private readonly WebApplication _app;
        private readonly GeritDbContext _database;
        private readonly string _privatePem;
        public HttpClient Client { get; }
        public JwtKeysEntity Key { get; }
        public int ResolverCalls { get; private set; }
        public int TokenValidatedCalls { get; private set; }
        public int CustomAuthorizationPassedCalls { get; private set; }
        public int EndpointCalls { get; private set; }
        public string? ValidationErrorType { get; private set; }

        private PipelineHost(WebApplication app, GeritDbContext database, JwtKeysEntity key, string privatePem)
        {
            _app = app;
            _database = database;
            Key = key;
            _privatePem = privatePem;
            Client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        }

        public static async Task<PipelineHost> StartAsync()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Development",
                ApplicationName = typeof(JwtBearerPipelineTests).Assembly.GetName().Name
            });
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = Issuer,
                ["JwtSettings:Audience"] = Audience,
                ["JwtSettings:AccessTokenExpirationMinutes"] = "15",
                ["JwtSettings:RefreshTokenExpirationDays"] = "7",
                ["JwtSettings:ClockSkewMinutes"] = "0",
                ["JwtSettings:RequireHttpsMetadata"] = "false"
            });
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IRequestTenantContext, RequestTenantContext>();
            builder.Services.AddJwt(builder.Configuration);
            builder.Services.AddAuthorization();

            var localization = new Mock<ILocalizationService>();
            localization.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
            builder.Services.AddSingleton(localization.Object);
            var keyPair = CryptoRSA.GenerateKeyPair();
            var masterKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var key = new JwtKeysEntity(7, keyPair.PublicKeyPem,
                CryptoRSA.EncryptPrivateKey(keyPair.PrivateKeyPem, masterKey), 1);
            var keys = new Mock<IJwtKeyDataRepository>();
            keys.Setup(x => x.GetActiveKeyAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(key);
            // Retorna todas as chaves deliberadamente: o teste exige defesa no pipeline mesmo se o repositório fornecer dados de outro tenant.
            keys.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { key });
            keys.Setup(x => x.GetByTenantAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { key });
            builder.Services.AddSingleton(keys.Object);

            var user = new UserEntity(7, "Operator", "operator@example.test", DomainExtensions.HashClientSecret(Password), null, 1);
            var tenant = new TenantEntity(1, 1, "Tenant", "tenant@example.test", null, null, null, 1);
            typeof(UserEntity).GetProperty("Id")!.SetValue(user, 10);
            typeof(TenantEntity).GetProperty("Id")!.SetValue(tenant, 7);
            var role = new RoleEntity(7, "Admin", "Administrator", 1);
            typeof(RoleEntity).GetProperty("Id")!.SetValue(role, 20);
            var action = new ActionEntity("GetBy", "Read", 1);
            var resource = new ResourceEntity("Actions", "Actions", 1);
            var userRole = new UserRolesEntity(7, 10, 20);
            typeof(UserRolesEntity).GetProperty("Role")!.SetValue(userRole, role);
            var permission = new RolePermissionsEntity(7, 20, 30, 40);
            typeof(RolePermissionsEntity).GetProperty("Resource")!.SetValue(permission, resource);
            typeof(RolePermissionsEntity).GetProperty("Action")!.SetValue(permission, action);

            var users = new Mock<IUserDataRepository>();
            users.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
            var tenants = new Mock<ITenantDataRepository>();
            tenants.Setup(x => x.GetByUserEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
            var userRoles = new Mock<IUserRoleDataRepository>();
            userRoles.Setup(x => x.GetByUserAsync(7, 10, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { userRole });
            var permissions = new Mock<IRolePermissionDataRepository>();
            permissions.Setup(x => x.GetByRoleAsync(20, 7, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { permission });
            var subscription = new Mock<ISubscriptionDomainService>();
            subscription.Setup(x => x.IsActiveAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var database = new GeritDbContext(new DbContextOptionsBuilder<GeritDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            var notify = new Notify();
            var auth = new AuthAppService(notify, Mock.Of<IMapper>(), permissions.Object,
                Mock.Of<IRefreshTokenDataRepository>(), users.Object, userRoles.Object, tenants.Object,
                Mock.Of<IEmailSender>(), localization.Object, Mock.Of<ICurrentUserService>(),
                Options.Create(builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!),
                NullLogger<AuthAppService>.Instance, builder.Configuration, keys.Object, database,
                Mock.Of<ISecretProvider>(x => x.GetMasterKey() == masterKey), subscription.Object, new RequestTenantContext());
            builder.Services.AddSingleton<IAuthAppService>(auth);
            var host = new PipelineHost(builder.Build(), database, key, keyPair.PrivateKeyPem);

            // Envolve os callbacks reais apenas para observar checkpoints, preservando seus retornos.
            var options = host._app.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
            var resolver = options.TokenValidationParameters.IssuerSigningKeyResolver;
            if (resolver is not null)
            {
                options.TokenValidationParameters.IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                {
                    host.ResolverCalls++;
                    return resolver(token, securityToken, kid, parameters);
                };
            }
            var validated = options.Events.OnTokenValidated;
            options.Events.OnTokenValidated = async context =>
            {
                host.TokenValidatedCalls++;
                await validated(context);
            };
            var failed = options.Events.OnAuthenticationFailed;
            options.Events.OnAuthenticationFailed = async context =>
            {
                host.ValidationErrorType = context.Exception.GetType().FullName;
                await failed(context);
            };

            host._app.UseAuthentication();
            host._app.UseAuthorization();
            host._app.MapPost("/v1/auth/login", async (LoginRequest request, IAuthAppService service, CancellationToken ct) =>
            {
                var response = await service.LoginAsync(request, ct);
                return response is null ? Results.Unauthorized() : Results.Ok(response);
            }).AllowAnonymous();
            host._app.MapGet("/v1/actions/1", (HttpContext context) =>
            {
                host.EndpointCalls++;
                return Results.Ok(new ProtectedResponse(context.User.Identity?.IsAuthenticated == true, context.User.FindFirst("tenant_id")?.Value));
            }).RequireAuthorization().CustomAuthorize("Admin", "Actions", "GetBy")
                .AddEndpointFilter(async (context, next) =>
                {
                    host.CustomAuthorizationPassedCalls++;
                    return await next(context);
                });
            await host._app.StartAsync();
            host.Client.BaseAddress = new Uri(host._app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
            return host;
        }

        public string CreateToken(int tenantId, string kid)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(_privatePem);
            var token = new JwtSecurityToken(Issuer, Audience, new[]
            {
                new Claim("sub", "10"), new Claim("tenant_id", tenantId.ToString()), new Claim("role", "admin"),
                new Claim("permissions", "{\"actions\":[\"getby\"]}")
            }, DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddMinutes(10),
                new SigningCredentials(new RsaSecurityKey(rsa)
                {
                    KeyId = kid,
                    CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
                }, SecurityAlgorithms.RsaSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.StopAsync();
            await _app.DisposeAsync();
            await _database.DisposeAsync();
        }
    }
}
