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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using VianaHub.Global.Gerit.Api.Configuration;
using VianaHub.Global.Gerit.Api.Endpoints.Business;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Api.Middleware;
using VianaHub.Global.Gerit.Api.Services;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Services.Business;
using VianaHub.Global.Gerit.Domain.Validators.Business.ClientDocument;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Tests.ClientDocuments;

[CollectionDefinition("ClientDocument HTTP", DisableParallelization = true)]
public sealed class ClientDocumentHttpCollection;

[Collection("ClientDocument HTTP")]
public sealed class ClientDocumentHttpIntegrationTests
{
    [Fact]
    public async Task LifecycleAndDetailUseMapperlyAndPreserveIdempotency()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client();
        var payload = new { documentTypeId = 1, documentNumber = "DOC-HTTP-1", issuingCountryCode = "pt", issuedAt = "2026-01-01", expiresAt = "2026-01-01", isPrimary = true };

        using var created = await client.PostAsJsonAsync("/v1/clients/10/documents", payload);
        var createBody = await created.Content.ReadAsStringAsync();
        Assert.True(created.StatusCode == HttpStatusCode.Created, $"HTTP {(int)created.StatusCode}: {createBody}");
        var createdBody = JsonSerializer.Deserialize<JsonElement>(createBody);
        var id = createdBody.GetProperty("id").GetInt32();

        var detail = await client.GetFromJsonAsync<JsonElement>($"/v1/clients/10/documents/{id}");
        Assert.Equal("PT", detail.GetProperty("issuingCountryCode").GetString());
        Assert.Equal("Identity", detail.GetProperty("documentType").GetString());
        Assert.Equal("2026-01-01", detail.GetProperty("issuedAt").GetString());

        using var updated = await client.PutAsJsonAsync($"/v1/clients/10/documents/{id}", new { payload.documentTypeId, documentNumber = "DOC-HTTP-2", payload.issuingCountryCode, payload.issuedAt, payload.expiresAt, isPrimary = false });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/v1/clients/10/documents/paged?PageNumber=1&PageSize=10")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/clients/10/documents/{id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/clients/10/documents/{id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/clients/10/documents/{id}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/clients/10/documents/{id}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/v1/clients/10/documents/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/clients/10/documents/{id}")).StatusCode);
    }

    private sealed class Host : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly SymmetricSecurityKey _key;
        private readonly Uri _address;
        private readonly IServiceProvider? _previous;

        private Host(WebApplication app, SymmetricSecurityKey key)
        {
            _app = app; _key = key;
            _address = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
            _previous = ServiceProviderHolder.ServiceProvider; ServiceProviderHolder.ServiceProvider = app.Services;
        }

        public static async Task<Host> StartAsync()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
            var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
            var database = Guid.NewGuid().ToString();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<VianaHub.Global.Gerit.Domain.Interfaces.Base.ILocalizationService, LocalizationService>();
            builder.Services.AddScoped<VianaHub.Global.Gerit.Domain.Interfaces.Base.ICurrentUserService, CurrentUserApiService>();
            builder.Services.AddScoped<VianaHub.Global.Gerit.Domain.Tools.Notifications.INotify, VianaHub.Global.Gerit.Domain.Tools.Notifications.Notify>();
            builder.Services.AddDbContext<GeritDbContext>(o => o.UseInMemoryDatabase(database));
            builder.Services.AddScoped<IClientDocumentDataRepository, VianaHub.Global.Gerit.Infra.Data.Repository.Business.ClientDocumentDataRepository>();
            builder.Services.AddScoped<IEntityDomainValidator<ClientDocumentsEntity>, ClientDocumentValidator>();
            builder.Services.AddScoped<IClientDocumentDomainService, ClientDocumentDomainService>();
            builder.Services.AddScoped<IClientDocumentAppService, ClientDocumentAppService>();
            builder.Services.AddScoped(_ => Mock.Of<IClientRepository>(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>()) == Task.FromResult<ClientEntity?>(SeedClient())));
            builder.Services.AddScoped(_ => Mock.Of<IDocumentTypeDataRepository>(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()) == Task.FromResult<DocumentTypeEntity?>(DocumentType())));
            builder.Services.AddRouteValidatorSetup();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => { o.MapInboundClaims = false; o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = "client-doc-tests", ValidateAudience = true, ValidAudience = "client-doc-tests", ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = key, ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ClockSkew = TimeSpan.Zero }; });
            builder.Services.AddAuthorization();
            var app = builder.Build();
            app.UseMiddleware<RequestLocalizationMiddleware>(); app.UseMiddleware<GlobalExceptionMiddleware>(); app.UseAuthentication(); app.UseAuthorization(); app.MapClientDocumentEndpoints();
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GeritDbContext>();
                db.AddRange(SeedClient(), DocumentType());
                await db.SaveChangesAsync();
            }
            await app.StartAsync(); return new Host(app, key);
        }

        public HttpClient Client()
        {
            var client = new HttpClient { BaseAddress = _address }; client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-PT");
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "17"), new Claim(ClaimTypes.Role, "BackOffice"), new Claim("tenant_id", "7"), new Claim("permissions", JsonSerializer.Serialize(new Dictionary<string, string[]> { ["ClientDocuments"] = ["GetAll", "GetBy", "GetPaged", "Create", "Update", "Activate", "Deactivate", "Delete"] }), JsonClaimValueTypes.Json) };
            var token = new JwtSecurityToken("client-doc-tests", "client-doc-tests", claims, expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token)); return client;
        }

        public async ValueTask DisposeAsync() { ServiceProviderHolder.ServiceProvider = _previous; await _app.StopAsync(); await _app.DisposeAsync(); }
        private static ClientEntity SeedClient() { var c = new ClientEntity(7, 1, 1, null, null, "Client", null, null, false, null, null, null, null, null, null, null, null, 1, 1, 17); typeof(ClientEntity).GetProperty("Id")!.SetValue(c, 10); return c; }
        private static DocumentTypeEntity DocumentType() { var d = new DocumentTypeEntity("IDENTITY", 17); typeof(DocumentTypeEntity).GetProperty("Id")!.SetValue(d, 1); d.Translations.Add(new DocumentTypeTranslationsEntity(1, "pt-PT", "Identity", null)); return d; }
    }
}
