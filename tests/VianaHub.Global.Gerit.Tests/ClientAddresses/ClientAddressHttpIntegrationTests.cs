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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using VianaHub.Global.Gerit.Api.Configuration;
using VianaHub.Global.Gerit.Api.Endpoints.Business;
using VianaHub.Global.Gerit.Api.Middleware;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Api.Services;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Application.Services.Common;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Services.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Domain.Validators.Business.ClientAddress;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Tests.ClientAddresses;

[CollectionDefinition("ClientAddress HTTP", DisableParallelization = true)]
public sealed class ClientAddressHttpCollection;

[Collection("ClientAddress HTTP")]
public sealed class ClientAddressHttpIntegrationTests
{
    [Fact]
    public async Task InvalidPayloadIsRejectedByRegisteredRouteValidator()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client();
        var payload = new { addressTypeId = 0, countryCode = "P", street = "", city = "", postalCode = "", isPrimary = false };
        using var created = await client.PostAsJsonAsync("/v1/clients/10/addresses", payload);
        Assert.Equal(HttpStatusCode.BadRequest, created.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedRequestReturns401()
    {
        await using var host = await Host.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/v1/clients/10/addresses")).StatusCode);
    }

    [Fact]
    public async Task AuthenticatedRequestWithoutGrantReturns403()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client("OtherResource", "GetAll");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/v1/clients/10/addresses")).StatusCode);
    }

    [Fact]
    public async Task AuthorizedCreateReturns201()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client();
        var payload = new { addressTypeId = 1, countryCode = "pt", street = "Rua HTTP", city = "Lisboa", postalCode = "1000", isPrimary = false };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/v1/clients/10/addresses", payload)).StatusCode);
    }

    [Fact]
    public async Task CrudLifecycleIsExercisedThroughHttpPipeline()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client();
        var payload = new { addressTypeId = 1, countryCode = "pt", street = "Rua HTTP", city = "Lisboa", postalCode = "1000", isPrimary = false };
        using var created = await client.PostAsJsonAsync("/v1/clients/10/addresses", payload);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/v1/clients/10/addresses")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/v1/clients/10/addresses/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/v1/clients/10/addresses/paged?pageNumber=1&pageSize=10")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/v1/clients/10/addresses/{id}", payload)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/clients/10/addresses/{id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/clients/10/addresses/{id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/clients/10/addresses/{id}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/clients/10/addresses/{id}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/v1/clients/10/addresses/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/clients/10/addresses/{id}")).StatusCode);
    }

    [Fact]
    public async Task DifferentClientRouteCannotReadOrMutateAddress()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client();
        var payload = new { addressTypeId = 1, countryCode = "PT", street = "Owned", city = "Lisboa", postalCode = "1000", isPrimary = false };
        var created = await client.PostAsJsonAsync("/v1/clients/10/addresses", payload);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/clients/11/addresses/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PutAsJsonAsync($"/v1/clients/11/addresses/{id}", payload)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/clients/11/addresses/{id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.DeleteAsync($"/v1/clients/11/addresses/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/v1/clients/10/addresses/{id}")).StatusCode);
    }

    [Fact]
    public async Task PrimaryConflictIsReturnedAs409()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client();
        var first = new { addressTypeId = 1, countryCode = "PT", street = "Primary 1", city = "Lisboa", postalCode = "1000", isPrimary = true };
        var second = new { addressTypeId = 1, countryCode = "PT", street = "Primary 2", city = "Lisboa", postalCode = "1001", isPrimary = true };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/v1/clients/10/addresses", first)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/v1/clients/10/addresses", second)).StatusCode);
    }

    [Fact]
    public async Task TenantBDoesNotSeeTenantAAddressThroughHttpPipeline()
    {
        await using var host = await Host.StartAsync();
        using var tenantA = host.Client(tenantId: 7);
        var created = await tenantA.PostAsJsonAsync("/v1/clients/10/addresses", new { addressTypeId = 1, countryCode = "PT", street = "Tenant A", city = "Lisboa", postalCode = "1000", isPrimary = false });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        using var tenantB = host.Client(tenantId: 8);
        Assert.Equal(HttpStatusCode.OK, (await tenantB.GetAsync("/v1/clients/20/addresses")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await tenantB.GetAsync($"/v1/clients/10/addresses/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await tenantB.PutAsJsonAsync($"/v1/clients/10/addresses/{id}", new { addressTypeId = 1, countryCode = "PT", street = "Tampered", city = "Lisboa", postalCode = "1000", isPrimary = false })).StatusCode);
        using var tenantARead = host.Client(tenantId: 7);
        var original = await tenantARead.GetAsync($"/v1/clients/10/addresses/{id}");
        Assert.Equal(HttpStatusCode.OK, original.StatusCode);
        Assert.Contains("Tenant A", await original.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task InactiveAndDeletedParentsRejectMutations()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client();
        var payload = new { addressTypeId = 1, countryCode = "PT", street = "Inactive", city = "Lisboa", postalCode = "1000", isPrimary = false };
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync("/v1/clients/11/addresses", payload)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync("/v1/clients/12/addresses", payload)).StatusCode);
        using var bulk = new MultipartFormDataContent();
        bulk.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("AddressTypeId;CountryCode;Street;City;PostalCode;IsPrimary\n1;PT;Inactive;Lisboa;1000;false\n")), "file", "addresses.csv");
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsync("/v1/clients/11/addresses/bulk-upload", bulk)).StatusCode);
        Assert.DoesNotContain("Inactive", await client.GetStringAsync("/v1/clients/11/addresses"));
    }

    [Fact]
    public async Task InactiveAndDeletedAddressTypesRejectMutations()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client();
        var payload = new { addressTypeId = 2, countryCode = "PT", street = "Inactive type", city = "Lisboa", postalCode = "1000", isPrimary = false };
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync("/v1/clients/10/addresses", payload)).StatusCode);
        payload = new { addressTypeId = 3, countryCode = "PT", street = "Deleted type", city = "Lisboa", postalCode = "1001", isPrimary = false };
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync("/v1/clients/10/addresses", payload)).StatusCode);
        using var bulk = new MultipartFormDataContent();
        bulk.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("AddressTypeId;CountryCode;Street;City;PostalCode;IsPrimary\n3;PT;Deleted bulk;Lisboa;1002;false\n")), "file", "addresses.csv");
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsync("/v1/clients/10/addresses/bulk-upload", bulk)).StatusCode);
        Assert.DoesNotContain("Deleted bulk", await client.GetStringAsync("/v1/clients/10/addresses"));
    }

    [Fact]
    public async Task DuplicateCreateAndUpdateReturn409AndSelfUpdateSucceeds()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client();
        var firstPayload = new { addressTypeId = 1, countryCode = "PT", street = "Duplicate", city = "Lisboa", postalCode = "1000", isPrimary = false };
        var first = await client.PostAsJsonAsync("/v1/clients/10/addresses", firstPayload);
        var firstId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/v1/clients/10/addresses", firstPayload)).StatusCode);
        var secondPayload = new { addressTypeId = 1, countryCode = "PT", street = "Second", city = "Lisboa", postalCode = "1001", isPrimary = false };
        var second = await client.PostAsJsonAsync("/v1/clients/10/addresses", secondPayload);
        var secondId = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/v1/clients/10/addresses/{secondId}", firstPayload)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/v1/clients/10/addresses/{firstId}", firstPayload)).StatusCode);
    }

    [Fact]
    public async Task PrimaryLifecycleReleasesPrimaryOnDeactivate()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client();
        var a = new { addressTypeId = 1, countryCode = "PT", street = "Primary A", city = "Lisboa", postalCode = "1000", isPrimary = true };
        var b = new { addressTypeId = 1, countryCode = "PT", street = "Primary B", city = "Lisboa", postalCode = "1001", isPrimary = false };
        var first = await client.PostAsJsonAsync("/v1/clients/10/addresses", a);
        var firstId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/v1/clients/10/addresses", new { addressTypeId = 1, countryCode = "PT", street = "Primary C", city = "Lisboa", postalCode = "1000", isPrimary = true })).StatusCode);
        var second = await client.PostAsJsonAsync("/v1/clients/10/addresses", b);
        var secondId = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/v1/clients/10/addresses/{secondId}", new { addressTypeId = 1, countryCode = "PT", street = "Primary B", city = "Lisboa", postalCode = "1001", isPrimary = true })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/clients/10/addresses/{firstId}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/clients/10/addresses/{firstId}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/v1/clients/10/addresses/{secondId}", new { addressTypeId = 1, countryCode = "PT", street = "Primary B", city = "Lisboa", postalCode = "1001", isPrimary = true })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/v1/clients/10/addresses/{secondId}")).StatusCode);
    }

    [Fact]
    public async Task BulkValidRowsPersistAndNormalizeCountry()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client();
        using var form = new MultipartFormDataContent();
        var csv = "AddressTypeId;CountryCode;Street;City;PostalCode;IsPrimary\n1;pt;Bulk One;Lisboa;1000;false\n1;PT;Bulk Two;Porto;4000;false\n";
        form.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(csv)), "file", "addresses.csv");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/v1/clients/10/addresses/bulk-upload", form)).StatusCode);
        var all = await client.GetStringAsync("/v1/clients/10/addresses");
        Assert.Contains("Bulk One", all); Assert.Contains("Bulk Two", all); Assert.Contains("PT", all);
    }

    [Fact]
    public async Task BulkDuplicateAndMultiplePrimaryAreRejectedWithoutRows()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client();
        using var duplicate = new MultipartFormDataContent();
        duplicate.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("AddressTypeId;CountryCode;Street;City;PostalCode;IsPrimary\n1;PT;Same;Lisboa;1000;false\n1;PT;Same;Lisboa;1000;false\n")), "file", "addresses.csv");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync("/v1/clients/10/addresses/bulk-upload", duplicate)).StatusCode);
        using var primary = new MultipartFormDataContent();
        primary.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("AddressTypeId;CountryCode;Street;City;PostalCode;IsPrimary\n1;PT;P1;Lisboa;1002;true\n1;PT;P2;Lisboa;1003;true\n")), "file", "addresses.csv");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync("/v1/clients/10/addresses/bulk-upload", primary)).StatusCode);
    }

    [Fact]
    public async Task BulkRowAndFileSizeBoundariesAreEnforced()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client();
        var maxRows = VianaHub.Global.Gerit.Domain.Helpers.DomainExtensions.GetMaxCsvRows();
        var header = "AddressTypeId;CountryCode;Street;City;PostalCode;IsPrimary\n";
        var one = "1;PT;Boundary-{0};Lisboa;1000;false\n";
        using var atLimit = new MultipartFormDataContent();
        atLimit.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(header + string.Concat(Enumerable.Range(0, maxRows).Select(i => string.Format(one, i))))), "file", "addresses.csv");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/v1/clients/10/addresses/bulk-upload", atLimit)).StatusCode);
        using var over = new MultipartFormDataContent();
        over.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(header + string.Concat(Enumerable.Range(0, maxRows + 1).Select(i => string.Format(one, i))))), "file", "addresses.csv");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/v1/clients/10/addresses/bulk-upload", over)).StatusCode);
        using var tooLarge = new MultipartFormDataContent();
        tooLarge.Add(new ByteArrayContent(new byte[(int)VianaHub.Global.Gerit.Domain.Helpers.DomainExtensions.GetMaxCsvFileSize() + 1]), "file", "addresses.csv");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/v1/clients/10/addresses/bulk-upload", tooLarge)).StatusCode);
    }

    [Fact]
    public async Task BulkMalformedCsvReturns400()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client();
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("invalid-header\n")), "file", "addresses.csv");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/v1/clients/10/addresses/bulk-upload", form)).StatusCode);
    }

    [Theory]
    [InlineData("pt-PT")]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    [InlineData("es-ES")]
    public async Task ValidationUsesLocalizedResponse(string language)
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client(); client.DefaultRequestHeaders.AcceptLanguage.Clear(); client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);
        using var response = await client.PostAsJsonAsync("/v1/clients/10/addresses", new { addressTypeId = 0, countryCode = "P", street = "", city = "", postalCode = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Api.Validator", body);
    }

    private sealed class Host : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly SymmetricSecurityKey _key;
        private readonly Uri _address;
        private readonly IServiceProvider? _previous;
        public Uri Address => _address;
        private Host(WebApplication app, SymmetricSecurityKey key)
        {
            _app = app; _key = key;
            _address = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
            _previous = ServiceProviderHolder.ServiceProvider; ServiceProviderHolder.ServiceProvider = app.Services;
        }
        public static async Task<Host> StartAsync()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserApiService>();
            builder.Services.AddScoped<INotify, Notify>();
            builder.Services.AddDbContext<GeritDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            builder.Services.AddSingleton<IClientAddressDataRepository, InMemoryAddressRepository>();
            builder.Services.AddSingleton<IClientRepository, InMemoryClientRepository>();
            builder.Services.AddSingleton<IAddressTypeDataRepository, InMemoryAddressTypeRepository>();
            builder.Services.AddScoped<IEntityDomainValidator<ClientAddressesEntity>, ClientAddressValidator>();
            builder.Services.AddScoped<IClientAddressDomainService, ClientAddressDomainService>();
            builder.Services.AddScoped<IClientAddressAppService, ClientAddressAppService>();
            builder.Services.AddScoped<IFileValidationService, FileValidationService>();
            builder.Services.AddRouteValidatorSetup();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = "client-address-tests", ValidateAudience = true, ValidAudience = "client-address-tests", ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = key, ValidAlgorithms = [SecurityAlgorithms.HmacSha256] });
            builder.Services.AddAuthorization();
            var app = builder.Build();
            app.UseMiddleware<RequestLocalizationMiddleware>(); app.UseMiddleware<GlobalExceptionMiddleware>(); app.UseAuthentication(); app.UseAuthorization(); app.MapClientAddressEndpoints();
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GeritDbContext>();
                db.Clients.Add(SeedClient());
                await db.SaveChangesAsync();
            }
            await app.StartAsync();
            return new Host(app, key);
        }
        public HttpClient Client(string resource = "ClientAddresses", string action = "GetAll", int tenantId = 7)
        {
            var client = new HttpClient { BaseAddress = _address }; client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-PT");
            var permissions = resource == "ClientAddresses"
                ? new Dictionary<string, string[]> { [resource] = ["GetAll", "GetBy", "GetPaged", "Create", "Update", "Activate", "Deactivate", "Delete", "BulkUpload"] }
                : new Dictionary<string, string[]> { [resource] = [action] };
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "17"), new Claim(ClaimTypes.Role, "BackOffice"), new Claim("tenant_id", tenantId.ToString()), new Claim("permissions", JsonSerializer.Serialize(permissions), JsonClaimValueTypes.Json) };
            var token = new JwtSecurityToken("client-address-tests", "client-address-tests", claims, expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token)); return client;
        }
        public async ValueTask DisposeAsync() { ServiceProviderHolder.ServiceProvider = _previous; await _app.StopAsync(); await _app.DisposeAsync(); }
        internal static ClientEntity SeedClient(int id = 10, int tenantId = 7, bool active = true, bool deleted = false)
        {
            var c = new ClientEntity(tenantId, 1, 1, null, null, "Client " + id, null, null, false, null, null, null, null, null, null, null, null, 1, 1, 17);
            typeof(ClientEntity).GetProperty("Id")!.SetValue(c, id);
            if (!active) c.Deactivate(17);
            if (deleted) c.Delete(17);
            return c;
        }
    }

    private sealed class InMemoryClientRepository : IClientRepository
    {
        private readonly Dictionary<int, ClientEntity> _clients = new();
        private readonly IHttpContextAccessor _http;
        public InMemoryClientRepository(IHttpContextAccessor http)
        {
            _http = http;
            Add(Host.SeedClient(10, 7, true, false));
            Add(Host.SeedClient(11, 7, false, false));
            Add(Host.SeedClient(12, 7, false, true));
            Add(Host.SeedClient(20, 8, true, false));
        }
        private void Add(ClientEntity client) => _clients[client.Id] = client;
        public Task<ClientEntity?> GetByIdAsync(int id, CancellationToken ct) => Task.FromResult(_clients.TryGetValue(id, out var value) && value.TenantId == Tenant() ? value : null);
        public Task<IEnumerable<ClientEntity>> GetAllAsync(CancellationToken ct) => Task.FromResult<IEnumerable<ClientEntity>>(_clients.Values.Where(x => x.TenantId == Tenant()).ToList());
        public Task<ListPage<ClientEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct) => Task.FromResult(new ListPage<ClientEntity>());
        public Task<bool> ExistsByIdAsync(int id, CancellationToken ct) => Task.FromResult(_clients.TryGetValue(id, out var value) && value.TenantId == Tenant());
        public Task<bool> AddAsync(ClientEntity entity, CancellationToken ct) { _clients[entity.Id] = entity; return Task.FromResult(true); }
        public Task<bool> UpdateAsync(ClientEntity entity, CancellationToken ct) { _clients[entity.Id] = entity; return Task.FromResult(true); }
        private int Tenant() => int.TryParse(_http.HttpContext?.User.FindFirst("tenant_id")?.Value, out var tenant) ? tenant : 7;
    }

    private sealed class InMemoryAddressTypeRepository : IAddressTypeDataRepository
    {
        private readonly Dictionary<int, AddressTypeEntity> _types = new();
        public InMemoryAddressTypeRepository()
        {
            Add(1, true, false); Add(2, false, false); Add(3, false, true);
        }
        private void Add(int id, bool active, bool deleted)
        {
            var entity = new AddressTypeEntity("TYPE-" + id, 17);
            typeof(AddressTypeEntity).GetProperty("Id")!.SetValue(entity, id);
            typeof(AddressTypeEntity).GetProperty(nameof(AddressTypeEntity.IsActive))!.SetValue(entity, active);
            typeof(AddressTypeEntity).GetProperty(nameof(AddressTypeEntity.IsDeleted))!.SetValue(entity, deleted);
            _types[id] = entity;
        }
        public Task<bool> ExistsByTranslationAsync(string? languageCode, string? name, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> ExistsByCodeAsync(string? code, CancellationToken ct) => Task.FromResult(_types.Values.Any(x => x.Code == code && x.IsActive && !x.IsDeleted));
        public Task<IEnumerable<AddressTypeEntity>> GetAllAsync(CancellationToken ct) => Task.FromResult<IEnumerable<AddressTypeEntity>>(_types.Values.Where(x => x.IsActive && !x.IsDeleted).ToList());
        public Task<AddressTypeEntity?> GetByIdAsync(int id, CancellationToken ct) => Task.FromResult(_types.TryGetValue(id, out var value) ? value : null);
        public Task<ListPage<AddressTypeEntity>> GetPagedAsync(PagedFilter filter, CancellationToken ct) => Task.FromResult(new ListPage<AddressTypeEntity>());
        public Task<bool> ExistsByIdAsync(int id, CancellationToken ct) => Task.FromResult(_types.TryGetValue(id, out var value) && value.IsActive && !value.IsDeleted);
        public Task<bool> ExistsByNameAsync(string? name, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> AddAsync(AddressTypeEntity entity, CancellationToken ct) { _types[entity.Id] = entity; return Task.FromResult(true); }
        public Task<bool> UpdateAsync(AddressTypeEntity entity, CancellationToken ct) { _types[entity.Id] = entity; return Task.FromResult(true); }
    }

    private sealed class InMemoryAddressRepository : IClientAddressDataRepository
    {
        private readonly List<ClientAddressesEntity> _items = [];
        private int _nextId = 1;
        private readonly IHttpContextAccessor _http;
        public InMemoryAddressRepository(IHttpContextAccessor http) => _http = http;
        private int Tenant() => int.TryParse(_http.HttpContext?.User.FindFirst("tenant_id")?.Value, out var tenant) ? tenant : 7;
        private bool Visible(ClientAddressesEntity x) => x.TenantId == Tenant();
        public Task<ClientAddressesEntity?> GetByIdAsync(int clientId, int id, CancellationToken ct) => Task.FromResult(_items.SingleOrDefault(x => Visible(x) && x.ClientId == clientId && x.Id == id && !x.IsDeleted));
        public Task<IEnumerable<ClientAddressesEntity>> GetAllAsync(int clientId, CancellationToken ct) => Task.FromResult<IEnumerable<ClientAddressesEntity>>(_items.Where(x => Visible(x) && x.ClientId == clientId && !x.IsDeleted).ToList());
        public Task<ListPage<ClientAddressesEntity>> GetPagedAsync(int clientId, PagedFilter request, CancellationToken ct)
        {
            var items = _items.Where(x => Visible(x) && x.ClientId == clientId && !x.IsDeleted).ToList();
            return Task.FromResult(new ListPage<ClientAddressesEntity> { Items = items, PageNumber = 1, PageSize = items.Count == 0 ? 1 : items.Count, TotalItems = items.Count, TotalPages = items.Count == 0 ? 0 : 1 });
        }
        public Task<bool> ExistsByIdAsync(int id, CancellationToken ct) => Task.FromResult(_items.Any(x => Visible(x) && x.Id == id && !x.IsDeleted));
        public Task<bool> ExistsByClientIdAsync(int clientId, string? countryCode, string? street, string? streetNumber, string? neighborhood, string? city, string? district, string? postalCode, CancellationToken ct) => Task.FromResult(FindIdentity(clientId, null, countryCode, street, streetNumber, neighborhood, city, district, postalCode));
        public Task<bool> ExistsByIdentityAsync(int clientId, int? excludeId, string? countryCode, string? street, string? streetNumber, string? neighborhood, string? city, string? district, string? postalCode, CancellationToken ct) => Task.FromResult(FindIdentity(clientId, excludeId, countryCode, street, streetNumber, neighborhood, city, district, postalCode));
        public Task<bool> ExistsPrimaryAsync(int clientId, int? excludeId, CancellationToken ct) => Task.FromResult(_items.Any(x => Visible(x) && x.ClientId == clientId && (!excludeId.HasValue || x.Id != excludeId) && x.IsPrimary && x.IsActive && !x.IsDeleted));
        public Task<bool> ExistsByClientAndAddressTypeAsync(int clientId, int addressTypeId, CancellationToken ct) => Task.FromResult(_items.Any(x => Visible(x) && x.ClientId == clientId && x.AddressTypeId == addressTypeId && !x.IsDeleted));
        public Task<bool> CreateAsync(ClientAddressesEntity entity, CancellationToken ct) { typeof(ClientAddressesEntity).GetProperty(nameof(ClientAddressesEntity.Id))!.SetValue(entity, _nextId++); _items.Add(entity); return Task.FromResult(true); }
        public Task<bool> CreateManyAsync(IReadOnlyCollection<ClientAddressesEntity> entities, CancellationToken ct) { foreach (var entity in entities) CreateAsync(entity, ct); return Task.FromResult(true); }
        public Task<bool> UpdateAsync(ClientAddressesEntity entity, CancellationToken ct) => Task.FromResult(_items.Contains(entity));
        public Task<bool> DeleteAsync(ClientAddressesEntity entity, CancellationToken ct) => Task.FromResult(_items.Contains(entity));
        private bool FindIdentity(int clientId, int? excludeId, string? countryCode, string? street, string? streetNumber, string? neighborhood, string? city, string? district, string? postalCode) => _items.Any(x => Visible(x) && x.ClientId == clientId && (!excludeId.HasValue || x.Id != excludeId) && Eq(x.CountryCode, countryCode, true) && Eq(x.Street, street) && Eq(x.StreetNumber, streetNumber) && Eq(x.Neighborhood, neighborhood) && Eq(x.City, city) && Eq(x.District, district) && Eq(x.PostalCode, postalCode) && !x.IsDeleted);
        private static bool Eq(string? left, string? right, bool ignoreCase = false) => string.Equals(left ?? string.Empty, right ?? string.Empty, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
}
