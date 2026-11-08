using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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
using VianaHub.Global.Gerit.Api.Endpoints.Identity;
using VianaHub.Global.Gerit.Api.Configuration;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Api.Middleware;
using VianaHub.Global.Gerit.Api.Services;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Action;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Application.Services.Common;
using VianaHub.Global.Gerit.Application.Services.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Services.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Validators.Identity.Action;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Repository.Identity;

namespace VianaHub.Global.Gerit.Tests.Actions;

[CollectionDefinition("Actions HTTP", DisableParallelization = true)]
public sealed class ActionsHttpCollection;

[Collection("Actions HTTP")]
public sealed class ActionHttpIntegrationTests
{
    [Fact]
    public async Task CrudLifecyclePaginationAndValidationPreserveContract()
    {
        await using var host = await TestHost.StartAsync();
        using var client = host.Client(["GetAll", "GetBy", "GetPaged", "Create", "Update", "Activate", "Deactivate", "Delete"]);

        using var created = await client.PostAsJsonAsync("/v1/actions/", new { name = "  Manage Users  ", description = new string('x', 255) });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<GenericResponse>())!.Id;
        Assert.True(id > 0);
        Assert.Single(await host.ReadAllAsync());
        var detail = await client.GetFromJsonAsync<ActionDetailResponse>($"/v1/actions/{id}");
        Assert.Equal("Manage Users", detail!.Name);
        Assert.Equal(255, detail.Description!.Length);

        using var page = await client.GetAsync("/v1/actions/paged?PageNumber=1&PageSize=10&SortBy=Name&SortDirection=asc");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal(id, Assert.Single((await page.Content.ReadFromJsonAsync<ListPageResponse<ActionResponse>>())!.Items).Id);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/v1/actions/{id}", new { description = new string('x', 256) })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/v1/actions/{id}", new { description = "Edited" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/actions/{id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/actions/{id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/actions/{id}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/actions/{id}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/v1/actions/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/actions/{id}")).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<ActionResponse[]>("/v1/actions/"))!);
        Assert.Equal(HttpStatusCode.Gone, (await client.PutAsJsonAsync($"/v1/actions/{id}", new { description = "Nope" })).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/actions/{id}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/actions/{id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.DeleteAsync($"/v1/actions/{id}")).StatusCode);
    }

    [Fact]
    public async Task AuthenticationAndExactPermissionAreRequired()
    {
        await using var host = await TestHost.StartAsync();
        using var anonymous = host.Client([], authenticated: false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/v1/actions/")).StatusCode);
        using var denied = host.Client(["GetBy"]);
        Assert.Equal(HttpStatusCode.Forbidden, (await denied.GetAsync("/v1/actions/")).StatusCode);
    }

    [Fact]
    public async Task BulkDuplicateInsideCsvPersistsNothing()
    {
        await using var host = await TestHost.StartAsync();
        using var client = host.Client(["BulkUpload"]);
        using var content = new MultipartFormDataContent();
        var csv = "Name;Description\nCreate;First\n create ;Second\n";
        content.Add(new StringContent(csv, Encoding.UTF8), "file", "actions.csv");
        using var response = await client.PostAsync("/v1/actions/bulk-upload", content);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Empty(await host.ReadAllAsync());
    }

    [Fact]
    public async Task MalformedCsvFailsTheWholeUpload()
    {
        await using var host = await TestHost.StartAsync();
        using var client = host.Client(["BulkUpload"]);
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Name;Description\nValid;\"unterminated\n", Encoding.UTF8), "file", "actions.csv");

        using var response = await client.PostAsync("/v1/actions/bulk-upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await host.ReadAllAsync());
    }

    private sealed class TestHost : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly SymmetricSecurityKey _key;
        private readonly Uri _address;
        private readonly IServiceProvider? _previousProvider;

        private TestHost(WebApplication app, SymmetricSecurityKey key)
        {
            _app = app;
            _key = key;
            _address = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
            _previousProvider = ServiceProviderHolder.ServiceProvider;
            ServiceProviderHolder.ServiceProvider = app.Services;
        }

        public static async Task<TestHost> StartAsync()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserApiService>();
            builder.Services.AddScoped<INotify, Notify>();
            var database = Guid.NewGuid().ToString();
            builder.Services.AddDbContext<GeritDbContext>(o => o.UseInMemoryDatabase(database));
            builder.Services.AddScoped<IActionDataRepository, ActionDataRepository>();
            builder.Services.AddScoped<IEntityDomainValidator<ActionEntity>, ActionValidator>();
            builder.Services.AddScoped<IActionDomainService, ActionDomainService>();
            builder.Services.AddScoped<IActionAppService, ActionAppService>();
            builder.Services.AddScoped<IFileValidationService, FileValidationService>();
            builder.Services.AddRouteValidatorSetup();
            var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "actions-tests",
                    ValidateAudience = true,
                    ValidAudience = "actions-tests",
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
            app.MapActionEndpoints();
            await app.StartAsync();
            return new TestHost(app, key);
        }

        public HttpClient Client(IEnumerable<string> actions, bool authenticated = true)
        {
            var client = new HttpClient { BaseAddress = _address };
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-PT");
            if (!authenticated) return client;
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "7"), new(ClaimTypes.Role, "BackOffice"), new("tenant_id", "1") };
            claims.Add(new Claim("permissions", JsonSerializer.Serialize(new Dictionary<string, string[]> { ["Actions"] = actions.ToArray() }), JsonClaimValueTypes.Json));
            var token = new JwtSecurityToken("actions-tests", "actions-tests", claims, expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
            return client;
        }

        public async Task<List<ActionEntity>> ReadAllAsync()
        {
            await using var scope = _app.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GeritDbContext>().Actions.IgnoreQueryFilters().ToListAsync();
        }

        public async ValueTask DisposeAsync()
        {
            ServiceProviderHolder.ServiceProvider = _previousProvider;
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
