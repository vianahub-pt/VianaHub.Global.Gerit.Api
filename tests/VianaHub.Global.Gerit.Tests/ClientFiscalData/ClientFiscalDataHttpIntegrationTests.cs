using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using VianaHub.Global.Gerit.Api.Endpoints.Business;
using VianaHub.Global.Gerit.Api.Configuration;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Api.Middleware;
using VianaHub.Global.Gerit.Api.Services;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Services.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Validators.Business.ClientFiscalData;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Repository.Business;

namespace VianaHub.Global.Gerit.Tests.ClientFiscalData;

[CollectionDefinition("ClientFiscalData HTTP", DisableParallelization = true)]
public sealed class ClientFiscalDataHttpCollection;

[Collection("ClientFiscalData HTTP")]
public sealed class ClientFiscalDataHttpIntegrationTests
{
    [Fact]
    public async Task GetById_UsesMapperlyAndPreservesNestedClientType()
    {
        await using var host = await Host.StartAsync();
        using var client = host.AuthenticatedClient();

        using var response = await client.GetAsync("/v1/clients/10/fiscal-data/44");

        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"HTTP {(int)response.StatusCode}: {content}");
        using var body = JsonDocument.Parse(content);
        var root = body.RootElement;
        Assert.Equal(44, root.GetProperty("id").GetInt32());
        Assert.Equal(10, root.GetProperty("clientId").GetInt32());
        Assert.Equal("Integration Client", root.GetProperty("client").GetString());
        Assert.Equal(1, root.GetProperty("clientTypeId").GetInt32());
        Assert.Equal("Organização", root.GetProperty("clientType").GetString());
        Assert.Equal("PT123", root.GetProperty("taxNumber").GetString());
    }

    private sealed class Host : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly SymmetricSecurityKey _key;
        private readonly Uri _address;
        private readonly IServiceProvider? _previousProvider;

        private Host(WebApplication app, SymmetricSecurityKey key)
        {
            _app = app;
            _key = key;
            _address = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
            _previousProvider = ServiceProviderHolder.ServiceProvider;
            ServiceProviderHolder.ServiceProvider = app.Services;
        }

        public static async Task<Host> StartAsync()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
            var database = Guid.NewGuid().ToString();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserApiService>();
            builder.Services.AddScoped<INotify, Notify>();
            builder.Services.AddDbContext<GeritDbContext>(options => options.UseInMemoryDatabase(database));
            builder.Services.AddScoped<IClientFiscalDataDataRepository, ClientFiscalDataDataRepository>();
            builder.Services.AddScoped<IEntityDomainValidator<ClientFiscalDataEntity>, ClientFiscalDataValidator>();
            builder.Services.AddScoped<IClientFiscalDataDomainService, ClientFiscalDataDomainService>();
            builder.Services.AddScoped<IClientFiscalDataAppService, ClientFiscalDataAppService>();
            builder.Services.AddScoped(_ => Mock.Of<IClientRepository>());
            builder.Services.AddScoped(_ => Mock.Of<IFileValidationService>());
            builder.Services.AddRouteValidatorSetup();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "fiscal-tests",
                    ValidateAudience = true,
                    ValidAudience = "fiscal-tests",
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.Zero
                };
            });
            builder.Services.AddAuthorization();

            var app = builder.Build();
            app.UseMiddleware<RequestLocalizationMiddleware>();
            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapClientFiscalDataEndpoints();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GeritDbContext>();
                var partyType = new PartyTypeEntity("ORGANIZATION", 17);
                typeof(PartyTypeEntity).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!.SetValue(partyType, (byte)1);
                partyType.Translations.Add(new PartyTypeTranslationsEntity(1, "pt-PT", "Organização", null));
                var client = new ClientEntity(7, 1, 1, null, null, "Integration Client", null, null, false, null, null, null, null, null, null, null, null, 1, 1, 17);
                typeof(ClientEntity).GetProperty("Id")!.SetValue(client, 10);
                typeof(ClientEntity).GetProperty("PartyType")!.SetValue(client, partyType);
                var fiscal = new ClientFiscalDataEntity(7, 10, "PT123", null, "PT", true, null, null, 17);
                typeof(ClientFiscalDataEntity).GetProperty("Id")!.SetValue(fiscal, 44);
                typeof(ClientFiscalDataEntity).GetProperty("Client")!.SetValue(fiscal, client);
                db.AddRange(partyType, client, fiscal);
                await db.SaveChangesAsync();
            }

            await app.StartAsync();
            return new Host(app, key);
        }

        public HttpClient AuthenticatedClient()
        {
            var client = new HttpClient { BaseAddress = _address };
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "17"),
                new Claim(ClaimTypes.Role, "BackOffice"),
                new Claim("tenant_id", "7"),
                new Claim("permissions", JsonSerializer.Serialize(new Dictionary<string, string[]> { ["ClientFiscalData"] = ["GetBy"] }), JsonClaimValueTypes.Json)
            };
            var token = new JwtSecurityToken("fiscal-tests", "fiscal-tests", claims, expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-PT");
            return client;
        }

        public async ValueTask DisposeAsync()
        {
            ServiceProviderHolder.ServiceProvider = _previousProvider;
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
