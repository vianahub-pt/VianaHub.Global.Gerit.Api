using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using VianaHub.Global.Gerit.Api.Configuration;
using VianaHub.Global.Gerit.Api.Endpoints.Business;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Api.Middleware;
using VianaHub.Global.Gerit.Api.Services;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientContact;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Mappings.Business;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Application.Services.Common;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Services.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Validators.Business.ClientContact;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Repository.Business;

namespace VianaHub.Global.Gerit.Tests.ClientContactPersons;

[CollectionDefinition("ClientContactPerson HTTP", DisableParallelization = true)]
public sealed class ClientContactPersonHttpCollection;

[Collection("ClientContactPerson HTTP")]
public class ClientContactPersonHttpIntegrationTests
{
    private static readonly string[] Actions = ["GetAll", "GetBy", "GetPaged", "Create", "Update", "Activate", "Deactivate", "Delete", "BulkUpload"];
    private static Dictionary<string, object?> Payload(string email = "contact@example.test", bool primary = false) => new()
    {
        ["name"] = "Contact",
        ["email"] = email,
        ["phoneNumber"] = "+351210000000",
        ["cellPhoneNumber"] = "+351910000000",
        ["isWhatsapp"] = true,
        ["isCellPhoneWhatsapp"] = false,
        ["jobTitle"] = "Manager",
        ["department"] = "Sales",
        ["isPrimary"] = primary,
        ["tenantId"] = 999
    };

    private static async Task<int> Create(HttpClient client, int clientId = 10, string email = "contact@example.test", bool primary = false)
    {
        using var response = await client.PostAsJsonAsync($"/v1/clients/{clientId}/contacts", Payload(email, primary));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var id = body.RootElement.GetProperty("id").GetInt32();
        Assert.True(id > 0);
        return id;
    }

    [Fact]
    public async Task CrudReadsLifecycleAndLegacyWhatsappRoundtrip()
    {
        await using var host = await Host.StartAsync(); using var client = host.Client(Actions);
        var id = await Create(client);
        var detail = await client.GetFromJsonAsync<ClientContactPersonDetailResponse>($"/v1/clients/10/contacts/{id}");
        Assert.NotNull(detail); Assert.Equal(7, detail.TenantId); Assert.Equal(10, detail.ClientId);
        Assert.True(detail.IsWhatsapp); Assert.True(detail.IsCellPhoneWhatsapp);
        Assert.Equal(id, Assert.Single((await client.GetFromJsonAsync<ClientContactPersonResponse[]>("/v1/clients/10/contacts"))!).Id);
        var page = await client.GetFromJsonAsync<ListPageResponse<ClientContactPersonResponse>>("/v1/clients/10/contacts/paged?PageNumber=1&PageSize=10");
        Assert.Equal(id, Assert.Single(page!.Items).Id);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/v1/clients/10/contacts/{id}", Payload())).StatusCode);
        foreach (var op in new[] { "deactivate", "deactivate", "activate", "activate" })
            Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/clients/10/contacts/{id}/{op}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/v1/clients/10/contacts/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/clients/10/contacts/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.DeleteAsync($"/v1/clients/10/contacts/{id}")).StatusCode);
    }

    [Fact]
    public async Task CrossClientIdCannotReadOrMutateContact()
    {
        await using var host = await Host.StartAsync(); using var client = host.Client(Actions); var id = await Create(client);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/clients/11/contacts/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PutAsJsonAsync($"/v1/clients/11/contacts/{id}", Payload())).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/clients/11/contacts/{id}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/clients/11/contacts/{id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.DeleteAsync($"/v1/clients/11/contacts/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/v1/clients/10/contacts/{id}")).StatusCode);
    }

    [Fact]
    public async Task EmailAndPrimaryConflictsAreParentScopedAndOwnUpdateIsAllowed()
    {
        await using var host = await Host.StartAsync(); using var client = host.Client(Actions);
        var id = await Create(client, 10, "same@example.test", true);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/v1/clients/10/contacts", Payload("same@example.test"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/v1/clients/10/contacts", Payload("other@example.test", true))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/v1/clients/11/contacts", Payload("same@example.test", true))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/v1/clients/10/contacts/{id}", Payload("same@example.test", true))).StatusCode);
        var second = await Create(client, 10, "second@example.test");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/v1/clients/10/contacts/{second}", Payload("same@example.test"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/v1/clients/10/contacts/{second}", Payload("second@example.test", true))).StatusCode);
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Update")]
    [InlineData("Activate")]
    [InlineData("Deactivate")]
    [InlineData("Delete")]
    public async Task PersistenceFalseNeverReturnsSuccess(string operation)
    {
        await using var host = await Host.StartAsync(); using var client = host.Client(Actions);
        var id = operation == "Create" ? 0 : await Create(client); host.SaveControl.Suppress = true;
        using var response = operation switch
        {
            "Create" => await client.PostAsJsonAsync("/v1/clients/10/contacts", Payload("new@example.test")),
            "Update" => await client.PutAsJsonAsync($"/v1/clients/10/contacts/{id}", Payload()),
            "Delete" => await client.DeleteAsync($"/v1/clients/10/contacts/{id}"),
            _ => await client.PatchAsync($"/v1/clients/10/contacts/{id}/{operation.ToLowerInvariant()}", null)
        };
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("GetAll", "GET", "/v1/clients/10/contacts")]
    [InlineData("GetBy", "GET", "/v1/clients/10/contacts/{id}")]
    [InlineData("GetPaged", "GET", "/v1/clients/10/contacts/paged")]
    [InlineData("Create", "POST", "/v1/clients/10/contacts")]
    [InlineData("Update", "PUT", "/v1/clients/10/contacts/{id}")]
    [InlineData("Activate", "PATCH", "/v1/clients/10/contacts/{id}/activate")]
    [InlineData("Deactivate", "PATCH", "/v1/clients/10/contacts/{id}/deactivate")]
    [InlineData("Delete", "DELETE", "/v1/clients/10/contacts/{id}")]
    public async Task EveryRouteRequiresItsPermission(string action, string method, string route)
    {
        await using var host = await Host.StartAsync(); using var setup = host.Client(Actions); var id = await Create(setup);
        using var denied = host.Client(Actions.Where(x => x != action));
        using var request = new HttpRequestMessage(new HttpMethod(method), route.Replace("{id}", id.ToString()))
        { Content = method is "POST" or "PUT" ? JsonContent.Create(Payload()) : null };
        Assert.Equal(HttpStatusCode.Forbidden, (await denied.SendAsync(request)).StatusCode);
    }

    [Theory]
    [InlineData("pt-PT")]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    [InlineData("es-ES")]
    public async Task LocalizedValidationConflictAuthorizationAndGone(string locale)
    {
        await using var host = await Host.StartAsync(); using var client = host.Client(Actions); client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(locale);
        var invalid = Payload(); invalid["email"] = "invalid";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/v1/clients/10/contacts", invalid)).StatusCode);
        var id = await Create(client);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/v1/clients/10/contacts", Payload())).StatusCode);
        using var denied = host.Client([]); denied.DefaultRequestHeaders.AcceptLanguage.Clear(); denied.DefaultRequestHeaders.AcceptLanguage.ParseAdd(locale);
        Assert.Equal(HttpStatusCode.Forbidden, (await denied.GetAsync("/v1/clients/10/contacts")).StatusCode);
        await client.DeleteAsync($"/v1/clients/10/contacts/{id}");
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/clients/10/contacts/{id}")).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("fr-FR")]
    public async Task MissingOrUnsupportedLanguageFallsBackToPtPt(string? language)
    {
        await using var host = await Host.StartAsync(); using var client = host.Client(Actions);
        client.DefaultRequestHeaders.AcceptLanguage.Clear(); if (language != null) client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);
        using var response = await client.GetAsync("/v1/clients/10/contacts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("pt-PT", response.Headers.GetValues("X-Test-Culture").Single());
    }

    [Fact]
    public async Task BulkUploadRejectsMissingAndEmptyAndPersistsValidRows()
    {
        await using var host = await Host.StartAsync(); using var client = host.Client(Actions);
        using var missing = new MultipartFormDataContent();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/v1/clients/10/contacts/bulk-upload", missing)).StatusCode);
        using var empty = new MultipartFormDataContent(); empty.Add(new ByteArrayContent([]), "file", "contacts.csv");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/v1/clients/10/contacts/bulk-upload", empty)).StatusCode);
        var csv = "Name;Email;Phone;JobTitle;Department;CellPhoneNumber;IsCellPhoneWhatsapp;IsPrimary\nBulk;bulk@example.test;;;;;false;false\n";
        using var valid = new MultipartFormDataContent(); valid.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "contacts.csv");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/v1/clients/10/contacts/bulk-upload", valid)).StatusCode);
        Assert.Contains((await client.GetFromJsonAsync<ClientContactPersonResponse[]>("/v1/clients/10/contacts"))!, x => x.Name == "Bulk");
    }

    private sealed class Host : IAsyncDisposable
    {
        private readonly WebApplication _app; private readonly SymmetricSecurityKey _key; private readonly Uri _address; private readonly IServiceProvider? _previous;
        public SaveControl SaveControl { get; }
        private Host(WebApplication app, SymmetricSecurityKey key, SaveControl control)
        { _app = app; _key = key; SaveControl = control; _address = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()); _previous = ServiceProviderHolder.ServiceProvider; ServiceProviderHolder.ServiceProvider = app.Services; }
        public static async Task<Host> StartAsync()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" }); builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
            var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64)); var control = new SaveControl();
            builder.Services.AddHttpContextAccessor(); builder.Services.AddSingleton<ILocalizationService, LocalizationService>(); builder.Services.AddScoped<ICurrentUserService, CurrentUserApiService>(); builder.Services.AddScoped<INotify, Notify>();
            builder.Services.AddSingleton<IMapper>(new MapperConfiguration(c => c.AddProfile<ClientContactPersonsMappingProfile>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper());
            var database = Guid.NewGuid().ToString();
            builder.Services.AddDbContext<GeritDbContext>(o => o.UseInMemoryDatabase(database).AddInterceptors(control));
            builder.Services.AddScoped<IClientContactPersonDataRepository, ClientContactPersonRepository>(); builder.Services.AddScoped<IEntityDomainValidator<ClientContactPersonsEntity>, ClientContactValidator>();
            builder.Services.AddScoped<IClientContactPersonDomainService, ClientContactPersonDomainService>(); builder.Services.AddScoped<IClientContactPersonAppService, ClientContactPersonAppService>(); builder.Services.AddScoped<IFileValidationService, FileValidationService>(); builder.Services.AddRouteValidatorSetup();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => { o.MapInboundClaims = false; o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = "tests", ValidateAudience = true, ValidAudience = "tests", ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = key, ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ClockSkew = TimeSpan.Zero }; }); builder.Services.AddAuthorization();
            var app = builder.Build(); app.UseMiddleware<RequestLocalizationMiddleware>(); app.Use(async (ctx, next) => { ctx.Response.OnStarting(() => { ctx.Response.Headers["X-Test-Culture"] = CultureInfo.CurrentCulture.Name; return Task.CompletedTask; }); await next(ctx); }); app.UseMiddleware<GlobalExceptionMiddleware>(); app.UseAuthentication(); app.UseAuthorization(); app.MapClientContactPersonEndpoints();
            using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<GeritDbContext>(); db.AddRange(Client(10, "A"), Client(11, "B")); await db.SaveChangesAsync(); }
            await app.StartAsync(); return new Host(app, key, control);
        }
        private static ClientEntity Client(int id, string name) { var entity = new ClientEntity(7, 1, 1, null, null, name, null, null, false, null, null, null, null, null, null, null, null, 1, 1, 17); typeof(ClientEntity).GetProperty("Id")!.SetValue(entity, id); return entity; }
        public HttpClient Client(IEnumerable<string> actions)
        { var client = new HttpClient { BaseAddress = _address }; client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-PT"); var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "17"), new(ClaimTypes.Role, "BackOffice"), new("tenant_id", "7"), new("permissions", JsonSerializer.Serialize(new Dictionary<string, string[]> { ["ClientContactPersons"] = actions.ToArray() }), JsonClaimValueTypes.Json) }; var token = new JwtSecurityToken("tests", "tests", claims, expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token)); return client; }
        public async ValueTask DisposeAsync() { ServiceProviderHolder.ServiceProvider = _previous; await _app.StopAsync(); await _app.DisposeAsync(); }
    }
    private sealed class SaveControl : SaveChangesInterceptor { public bool Suppress { get; set; } public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData e, InterceptionResult<int> r, CancellationToken c = default) => ValueTask.FromResult(Suppress ? InterceptionResult<int>.SuppressWithResult(0) : r); }
}
