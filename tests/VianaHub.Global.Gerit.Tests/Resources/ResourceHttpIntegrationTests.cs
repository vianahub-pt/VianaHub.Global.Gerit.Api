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
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Api.Middleware;
using VianaHub.Global.Gerit.Api.Services;
using VianaHub.Global.Gerit.Api.Configuration;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Resource;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Application.Services.Common;
using VianaHub.Global.Gerit.Application.Services.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Services.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Validators.Identity.Resource;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Repository.Identity;

namespace VianaHub.Global.Gerit.Tests.Resources;

[CollectionDefinition("Resources HTTP", DisableParallelization = true)]
public sealed class ResourcesHttpCollection;

[Collection("Resources HTTP")]
public sealed class ResourceHttpIntegrationTests
{
    [Fact]
    public async Task LifecyclePaginationDescriptionAndAuthorizationContract()
    {
        await using var host = await TestHost.StartAsync();
        using var client = host.Client(["GetAll", "GetBy", "GetPaged", "Create", "Update", "Activate", "Deactivate", "Delete"]);

        using var created = await client.PostAsJsonAsync("/v1/resources/", new { name = "  Smoke Resource  ", description = "Original description" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<GenericResponse>())!.Id;
        Assert.True(id > 0);

        var detail = await client.GetFromJsonAsync<ResourceDetailResponse>($"/v1/resources/{id}");
        Assert.Equal("Smoke Resource", detail!.Name);
        Assert.Equal("Original description", detail.Description);

        using var paged = await client.GetAsync("/v1/resources/paged?PageNumber=1&PageSize=10&Search=Smoke");
        Assert.Equal(HttpStatusCode.OK, paged.StatusCode);
        Assert.Contains(id, (await paged.Content.ReadFromJsonAsync<ListPageResponse<ResourceResponse>>())!.Items.Select(x => x.Id));

        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/v1/resources/{id}", new { description = "Updated description" })).StatusCode);
        Assert.Equal("Updated description", (await client.GetFromJsonAsync<ResourceDetailResponse>($"/v1/resources/{id}"))!.Description);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/resources/{id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/resources/{id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/resources/{id}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/resources/{id}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/v1/resources/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/resources/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/resources/{id}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/resources/{id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.DeleteAsync($"/v1/resources/{id}")).StatusCode);
        var all = await client.GetFromJsonAsync<ResourceResponse[]>("/v1/resources/") ?? [];
        Assert.Empty(all);
        Assert.Empty((await client.GetFromJsonAsync<ListPageResponse<ResourceResponse>>("/v1/resources/paged?PageNumber=1&PageSize=10&Search=Smoke"))!.Items);
    }

    [Fact]
    public async Task DescriptionValidationAndNormalizedDuplicateReturnLocalizedErrors()
    {
        await using var host = await TestHost.StartAsync();
        using var client = host.Client(["Create"]);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/v1/resources/", new { name = "Missing description" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/v1/resources/", new { name = "Too long", description = new string('x', 256) })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/v1/resources/", new { name = " Support ", description = "One" })).StatusCode);
        using var duplicate = await client.PostAsJsonAsync("/v1/resources/", new { name = "support", description = "Two" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var body = await duplicate.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Application.Service.Resource.Create.ResourceAlreadyExists", body, StringComparison.Ordinal);
        Assert.Contains("já existe", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BulkUploadRejectsInternalDuplicateAndMalformedCsvAtomically()
    {
        await using var host = await TestHost.StartAsync();
        using var client = host.Client(["BulkUpload", "GetAll"]);

        using (var duplicateContent = new MultipartFormDataContent())
        {
            duplicateContent.Add(new StringContent("Name;Description\nBulk;First\n bulk ;Second\n", Encoding.UTF8), "file", "resources.csv");
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync("/v1/resources/bulk-upload", duplicateContent)).StatusCode);
        }

        using (var malformedContent = new MultipartFormDataContent())
        {
            malformedContent.Add(new StringContent("Name;Description\nValid;\"unterminated\n", Encoding.UTF8), "file", "resources.csv");
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/v1/resources/bulk-upload", malformedContent)).StatusCode);
        }

        Assert.Empty(await host.ReadAllAsync());
    }

    [Fact]
    public async Task BulkUploadAcceptsValidBatchAndEnforcesTenThousandRowBoundary()
    {
        await using var host = await TestHost.StartAsync();
        using var client = host.Client(["BulkUpload"]);

        using (var validContent = new MultipartFormDataContent())
        {
            validContent.Add(new StringContent("Name;Description\nBatch One;First\nBatch Two;Second\n", Encoding.UTF8), "file", "resources.csv");
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/v1/resources/bulk-upload", validContent)).StatusCode);
        }

        using (var overflowContent = new MultipartFormDataContent())
        {
            var rows = new StringBuilder("Name;Description\n");
            for (var index = 0; index < 10_001; index++)
                rows.Append("Overflow ").Append(index).Append(";Description\n");
            overflowContent.Add(new StringContent(rows.ToString(), Encoding.UTF8), "file", "resources.csv");
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/v1/resources/bulk-upload", overflowContent)).StatusCode);
        }

        Assert.Equal(2, (await host.ReadAllAsync()).Count);
    }

    [Fact]
    public async Task ExactPermissionsAreRequiredAndDeactivateUsesPluralResource()
    {
        await using var host = await TestHost.StartAsync();
        using var anonymous = host.Client([], authenticated: false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/v1/resources/")).StatusCode);
        using var denied = host.Client(["GetBy"]);
        Assert.Equal(HttpStatusCode.Forbidden, (await denied.PatchAsync("/v1/resources/1/deactivate", null)).StatusCode);
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
            builder.Services.AddScoped<IResourceDataRepository, ResourceDataRepository>();
            builder.Services.AddScoped<IEntityDomainValidator<ResourceEntity>, ResourceValidator>();
            builder.Services.AddScoped<IResourceDomainService, ResourceDomainService>();
            builder.Services.AddScoped<IResourceAppService, ResourceAppService>();
            builder.Services.AddScoped<IFileValidationService, FileValidationService>();
            builder.Services.AddRouteValidatorSetup();
            var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "resources-tests",
                    ValidateAudience = true,
                    ValidAudience = "resources-tests",
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
            app.MapResourceEndpoints();
            await app.StartAsync();
            return new TestHost(app, key);
        }

        public HttpClient Client(IEnumerable<string> actions, bool authenticated = true)
        {
            var client = new HttpClient { BaseAddress = _address };
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-PT");
            if (!authenticated) return client;
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "7"), new(ClaimTypes.Role, "BackOffice"), new("tenant_id", "1") };
            claims.Add(new Claim("permissions", JsonSerializer.Serialize(new Dictionary<string, string[]> { ["Resources"] = actions.ToArray() }), JsonClaimValueTypes.Json));
            var token = new JwtSecurityToken("resources-tests", "resources-tests", claims, expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
            return client;
        }

        public async Task<List<ResourceEntity>> ReadAllAsync()
        {
            await using var scope = _app.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<GeritDbContext>().Resources.IgnoreQueryFilters().ToListAsync();
        }

        public async ValueTask DisposeAsync()
        {
            ServiceProviderHolder.ServiceProvider = _previousProvider;
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
