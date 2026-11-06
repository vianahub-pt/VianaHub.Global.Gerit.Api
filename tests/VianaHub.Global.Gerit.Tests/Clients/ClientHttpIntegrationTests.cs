using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using VianaHub.Global.Gerit.Api.Configuration;
using VianaHub.Global.Gerit.Api.Endpoints.Business;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Api.Middleware;
using VianaHub.Global.Gerit.Api.Services;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Client;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Client;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Tests.Clients;

[CollectionDefinition("Client root HTTP", DisableParallelization = true)]
public sealed class ClientHttpCollection;

[Collection("Client root HTTP")]
public sealed class ClientHttpIntegrationTests
{
    [Fact]
    public async Task AnonymousReadReturns401()
    {
        await using var host = await Host.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/v1/clients")).StatusCode);
    }

    [Fact]
    public async Task AuthorizedDetailExposesStatusPairAndLocalizedNames()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client();
        var response = await client.GetAsync("/v1/clients/10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ClientDetailResponse>();
        Assert.NotNull(body);
        Assert.Equal(1, body!.StatusDefinitionId);
        Assert.Equal(1, body.StatusDomainId);
        Assert.Equal("Ativo", body.StatusDefinitionName);
        Assert.Equal("Clientes", body.StatusDomainName);
    }

    [Fact]
    public async Task AuthorizedCreateValidationRejectsMissingNameBeforeAppService()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client();
        var response = await client.PostAsJsonAsync("/v1/clients", new { partyTypeId = 1, acquisitionSourceTypeId = 1, statusDomainId = 1, statusDefinitionId = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        host.App.Verify(x => x.CreateAsync(It.IsAny<global::VianaHub.Global.Gerit.Application.Dtos.Request.Business.Client.CreateClientRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticatedUserWithoutClientsPermissionReturns403()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client(new Dictionary<string, string[]> { ["Other"] = ["GetAll"] });
        var response = await client.GetAsync("/v1/clients");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedRootCrudLifecycleAndPagedEndpointsReturnExpectedStatuses()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client();
        var request = new CreateClientRequest { PartyTypeId = 1, AcquisitionSourceTypeId = 1, StatusDomainId = 1, StatusDefinitionId = 1, Name = "HTTP client" };
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/v1/clients")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/v1/clients/paged?pageNumber=1&pageSize=10")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/v1/clients", request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/v1/clients/10", new UpdateClientRequest
        {
            PartyTypeId = 1,
            AcquisitionSourceTypeId = 1,
            StatusDomainId = 1,
            StatusDefinitionId = 1,
            Name = "HTTP client updated"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync("/v1/clients/10/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync("/v1/clients/10/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("/v1/clients/10")).StatusCode);
        host.App.Verify(x => x.CreateAsync(It.IsAny<CreateClientRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        host.App.Verify(x => x.UpdateAsync(10, It.IsAny<UpdateClientRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        host.App.Verify(x => x.DeactivateAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        host.App.Verify(x => x.ActivateAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        host.App.Verify(x => x.DeleteAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class Host(WebApplication app, SymmetricSecurityKey key, Mock<IClientAppService> appService) : IAsyncDisposable
    {
        private readonly Uri _address = new(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
        public Uri Address => _address;
        public Mock<IClientAppService> App => appService;

        public static async Task<Host> StartAsync()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
            var service = new Mock<IClientAppService>();
            service.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new ClientDetailResponse
            {
                Id = 10,
                TenantId = 7,
                Name = "Client HTTP",
                PartyTypeId = 1,
                PartyType = "Organização",
                AcquisitionSourceTypeId = 1,
                AcquisitionSourceType = "Direto",
                StatusDefinitionId = 1,
                StatusDefinitionName = "Ativo",
                StatusDomainId = 1,
                StatusDomainName = "Clientes",
                IsActive = true
            });
            service.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<ClientResponse>());
            service.Setup(x => x.GetPagedAsync(It.IsAny<PagedFilterRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ListPageResponse<ClientResponse>());
            service.Setup(x => x.CreateAsync(It.IsAny<CreateClientRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(11);
            service.Setup(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateClientRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            service.Setup(x => x.ActivateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            service.Setup(x => x.DeactivateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            service.Setup(x => x.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserApiService>();
            builder.Services.AddScoped<INotify, Notify>();
            builder.Services.AddSingleton<IClientAppService>(service.Object);
            builder.Services.AddRouteValidatorSetup();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "client-root-tests",
                ValidateAudience = true,
                ValidAudience = "client-root-tests",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
            });
            builder.Services.AddAuthorization();
            var app = builder.Build();
            app.UseMiddleware<RequestLocalizationMiddleware>();
            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapClientEndpoints();
            await app.StartAsync();
            return new Host(app, key, service);
        }

        public HttpClient Client(Dictionary<string, string[]>? permissions = null)
        {
            var client = new HttpClient { BaseAddress = _address };
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-PT");
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "17"), new Claim(ClaimTypes.Role, "BackOffice"), new Claim("tenant_id", "7"),
                new Claim("permissions", JsonSerializer.Serialize(permissions ?? new Dictionary<string, string[]> { ["Clients"] = ["GetAll", "GetBy", "GetPaged", "Create", "Update", "Activate", "Deactivate", "Delete"] }), JsonClaimValueTypes.Json)
            };
            var token = new JwtSecurityToken("client-root-tests", "client-root-tests", claims, expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
            return client;
        }

        public async ValueTask DisposeAsync()
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
