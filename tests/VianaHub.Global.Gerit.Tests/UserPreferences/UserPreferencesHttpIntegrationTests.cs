using VianaHub.Global.Gerit.Api.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using AutoMapper;
using FluentValidation;
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
using VianaHub.Global.Gerit.Api.Endpoints.Identity;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Api.Middleware;
using VianaHub.Global.Gerit.Api.Services;
using VianaHub.Global.Gerit.Api.Validators.Identity.UserPreferences;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.UserPreferences;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.UserPreferences;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;

using VianaHub.Global.Gerit.Application.Services.Identity;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Services.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Validators.Identity.UserPreferences;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Repository.Identity;

namespace VianaHub.Global.Gerit.Tests.UserPreferences;

[CollectionDefinition("UserPreferences HTTP", DisableParallelization = true)]
public sealed class UserPreferencesHttpCollection { }

// HTTP real em loopback com JwtBearer e filtros reais; persistência EF InMemory isolada.
// Não substitui prova de RLS, collation ou constraints em SQL Server.
[Collection("UserPreferences HTTP")]
public class UserPreferencesHttpIntegrationTests
{
    private static readonly string[] Actions = ["GetAll", "GetBy", "GetPaged", "Create", "Update", "Activate", "Deactivate", "Delete"];


    private static Dictionary<string, object?> Payload() => new()
    {
        ["appearance"] = "dark",
        ["currencyCode"] = "USD",
        ["locale"] = "es-ES",
        ["timezone"] = "Europe/Lisbon",
        ["dateFormat"] = "dd/MM/yyyy",
        ["timeFormat"] = "24h",
        ["dayStart"] = "22:15",
        ["dayEnd"] = "06:30",
        ["emailNewsletter"] = true,
        ["emailWeeklyReport"] = true,
        ["emailApproval"] = true,
        ["emailAlerts"] = false,
        ["emailReminders"] = false,
        ["emailPlanner"] = false,
        ["tenantId"] = 999,
        ["userId"] = 999
    };
    private static async Task<int> Create(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/v1/user-preferences", Payload());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GenericResponse>();
        Assert.NotNull(result); Assert.True(result.Id > 0); return result.Id;
    }
    [Fact]
    public async Task CrudRoundtrip_UsesAuthenticatedIdentityAndPreservesAllFieldsAndFlags()
    {
        await using var host = await Host.StartAsync(); using var client = host.Client(Actions);
        var id = await Create(client);
        var detail = await client.GetFromJsonAsync<UserPreferencesDetailResponse>($"/v1/user-preferences/{id}");
        Assert.NotNull(detail); Assert.Equal(7, detail.TenantId); Assert.Equal(17, detail.UserId);
        Assert.Equal("Fixture tenant", detail.Tenant); Assert.Equal("Fixture user", detail.User);
        Assert.Equal("dark", detail.Appearance); Assert.Equal("USD", detail.CurrencyCode); Assert.Equal("es-ES", detail.Locale);
        Assert.Equal("22:15", detail.DayStart); Assert.Equal("06:30", detail.DayEnd);
        Assert.True(detail.EmailNewsletter); Assert.True(detail.EmailWeeklyReport); Assert.True(detail.EmailApproval);
        Assert.False(detail.EmailAlerts); Assert.False(detail.EmailReminders); Assert.False(detail.EmailPlanner);
        Assert.True(detail.CreatedAt > DateTime.MinValue); Assert.Equal(17, detail.CreatedBy);
        Assert.Equal(id, (await client.GetFromJsonAsync<UserPreferencesResponse>("/v1/user-preferences/user/17"))!.Id);
        Assert.Equal(id, Assert.Single((await client.GetFromJsonAsync<UserPreferencesResponse[]>("/v1/user-preferences"))!).Id);
        var paged = await client.GetFromJsonAsync<ListPageResponse<UserPreferencesResponse>>("/v1/user-preferences/paged?PageNumber=1&PageSize=10");
        Assert.NotNull(paged); Assert.Equal(id, Assert.Single(paged.Items).Id); Assert.Equal(1, paged.TotalItems);
        var roundtrip = JsonSerializer.Deserialize<UpdateUserPreferencesRequest>(JsonSerializer.Serialize(detail), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        using var updated = await client.PutAsJsonAsync($"/v1/user-preferences/{id}", roundtrip);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode); Assert.True(await updated.Content.ReadFromJsonAsync<bool>());
        var after = await client.GetFromJsonAsync<UserPreferencesDetailResponse>($"/v1/user-preferences/{id}");
        Assert.Equal(detail.DayStart, after!.DayStart); Assert.Equal(detail.DayEnd, after.DayEnd); Assert.Equal(detail.Locale, after.Locale);
        foreach (var op in new[] { "deactivate", "deactivate", "activate", "activate" })
        {
            Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/user-preferences/{id}/{op}", null)).StatusCode);
            Assert.Equal(op == "activate", (await client.GetFromJsonAsync<UserPreferencesDetailResponse>($"/v1/user-preferences/{id}"))!.IsActive);
        }
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/v1/user-preferences/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/user-preferences/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync("/v1/user-preferences/user/17")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.DeleteAsync($"/v1/user-preferences/{id}")).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<UserPreferencesResponse[]>("/v1/user-preferences"))!);
    }
    [Theory]
    [InlineData("currencyCode", null)]
    [InlineData("currencyCode", "")]
    [InlineData("currencyCode", "EURO")]
    [InlineData("dayStart", null)]
    [InlineData("dayStart", "")]
    [InlineData("dayStart", "25:00")]
    [InlineData("dayStart", "09:00suffix")]
    [InlineData("dayStart", "09:00:00")]
    [InlineData("dayEnd", null)]
    [InlineData("dayEnd", "")]
    [InlineData("dayEnd", "24:00")]
    [InlineData("dayEnd", "18:00suffix")]
    [InlineData("appearance", "blue")]
    [InlineData("timeFormat", "25h")]
    public async Task InvalidExplicitInputs_Are400ForCreateAndUpdateWithoutMutation(string field, string? value)
    {
        await using var host = await Host.StartAsync(); using var client = host.Client(Actions); var id = await Create(client);
        var invalid = Payload(); invalid[field] = value;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/v1/user-preferences", invalid)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/v1/user-preferences/{id}", invalid)).StatusCode);
        Assert.Equal("USD", (await client.GetFromJsonAsync<UserPreferencesDetailResponse>($"/v1/user-preferences/{id}"))!.CurrencyCode);
    }
    [Theory]
    [InlineData("Create")]
    [InlineData("Update")]
    [InlineData("Activate")]
    [InlineData("Deactivate")]
    [InlineData("Delete")]
    public async Task PersistenceFalse_NeverReturnsFalseHttpSuccess(string operation)
    {
        await using var host = await Host.StartAsync(); using var client = host.Client(Actions);
        var id = operation == "Create" ? 0 : await Create(client); host.SaveControl.Suppress = true;
        using var response = operation switch
        {
            "Create" => await client.PostAsJsonAsync("/v1/user-preferences", Payload()),
            "Update" => await client.PutAsJsonAsync($"/v1/user-preferences/{id}", Payload()),
            "Delete" => await client.DeleteAsync($"/v1/user-preferences/{id}"),
            _ => await client.PatchAsync($"/v1/user-preferences/{id}/{operation.ToLowerInvariant()}", null)
        };
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Theory]
    [InlineData("GetAll", "GET", "/v1/user-preferences")]
    [InlineData("GetBy", "GET", "/v1/user-preferences/{id}")]
    [InlineData("GetBy", "GET", "/v1/user-preferences/user/17")]
    [InlineData("GetPaged", "GET", "/v1/user-preferences/paged")]
    [InlineData("Create", "POST", "/v1/user-preferences")]
    [InlineData("Update", "PUT", "/v1/user-preferences/{id}")]
    [InlineData("Activate", "PATCH", "/v1/user-preferences/{id}/activate")]
    [InlineData("Deactivate", "PATCH", "/v1/user-preferences/{id}/deactivate")]
    [InlineData("Delete", "DELETE", "/v1/user-preferences/{id}")]
    public async Task EveryRoute_RequiresItsPermission(string action, string method, string route)
    {
        await using var host = await Host.StartAsync(); using var setup = host.Client(Actions); var id = await Create(setup);
        using var denied = host.Client(Actions.Where(x => x != action)); using var request = new HttpRequestMessage(new HttpMethod(method), route.Replace("{id}", id.ToString())) { Content = method is "POST" or "PUT" ? JsonContent.Create(Payload()) : null };
        Assert.Equal(HttpStatusCode.Forbidden, (await denied.SendAsync(request)).StatusCode);
    }
    [Theory]
    [InlineData("pt-PT")]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    [InlineData("es-ES")]
    public async Task LocalizedErrors_400409403410_AndInactiveUniqueness(string locale)
    {
        await using var host = await Host.StartAsync(); using var client = host.Client(Actions);
        client.DefaultRequestHeaders.AcceptLanguage.Clear(); client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(locale);
        var id = await Create(client); var invalid = Payload(); invalid["appearance"] = "";
        using var dictionary = await Dictionary(locale);
        await Error(await client.PostAsJsonAsync("/v1/user-preferences", invalid), 400, dictionary, "Api.Helpers.NotifyExtensions.Error.ValidationError", "Api.Validator.UserPreferences.Create.Appearance");
        await Error(await client.PostAsJsonAsync("/v1/user-preferences", Payload()), 409, dictionary, "Api.Helpers.NotifyExtensions.Error.Conflict", "Application.Service.UserPreferences.Create.ResourceAlreadyExists");
        await client.PatchAsync($"/v1/user-preferences/{id}/deactivate", null);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/v1/user-preferences", Payload())).StatusCode);
        using var denied = host.Client([]); denied.DefaultRequestHeaders.AcceptLanguage.Clear(); denied.DefaultRequestHeaders.AcceptLanguage.ParseAdd(locale);
        await Error(await denied.PostAsJsonAsync("/v1/user-preferences", Payload()), 403, dictionary, "Api.Authorization.AccessDenied", "Api.Authorization.Forbidden.Permission.Detail");
        await client.DeleteAsync($"/v1/user-preferences/{id}");
        await Error(await client.GetAsync($"/v1/user-preferences/{id}"), 410, dictionary, "Api.Helpers.NotifyExtensions.Error.Gone", "Application.Service.UserPreferences.Update.ResourceNotFound");
    }
    [Theory]
    [InlineData(null)]
    [InlineData("fr-FR")]
    public async Task Fallback_RemainsPtPt(string? language)
    {
        await using var host = await Host.StartAsync(); using var client = host.Client(Actions);
        client.DefaultRequestHeaders.AcceptLanguage.Clear(); if (language != null) client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);
        using var response = await client.GetAsync("/v1/user-preferences"); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("pt-PT", response.Headers.GetValues("X-Test-Culture").Single()); Assert.Equal("pt-PT", response.Headers.GetValues("X-Test-UI-Culture").Single());
    }
    [Fact]
    public async Task UserRole_OnlyAccessesOwnPreferences_WhileElevatedRolesRetainPermissionBasedAccess()
    {
        await using var host = await Host.StartAsync();
        using var elevated = host.Client(Actions);
        var ownId = await Create(elevated);
        var otherId = await host.SeedOtherUserPreferencesAsync();

        using var user = host.Client(Actions, role: "User");
        Assert.Equal(ownId, Assert.Single((await user.GetFromJsonAsync<UserPreferencesResponse[]>("/v1/user-preferences"))!).Id);
        var page = await user.GetFromJsonAsync<ListPageResponse<UserPreferencesResponse>>("/v1/user-preferences/paged?PageNumber=1&PageSize=10");
        Assert.NotNull(page); Assert.Equal(ownId, Assert.Single(page.Items).Id); Assert.Equal(1, page.TotalItems);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync($"/v1/user-preferences/{otherId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/v1/user-preferences/user/18")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.PutAsJsonAsync($"/v1/user-preferences/{otherId}", Payload())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.PatchAsync($"/v1/user-preferences/{otherId}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.DeleteAsync($"/v1/user-preferences/{otherId}")).StatusCode);

        Assert.Equal(2, (await elevated.GetFromJsonAsync<UserPreferencesResponse[]>("/v1/user-preferences"))!.Length);
        Assert.Equal(otherId, (await elevated.GetFromJsonAsync<UserPreferencesResponse>("/v1/user-preferences/user/18"))!.Id);
    }
    private static async Task<JsonDocument> Dictionary(string locale)
    {
        var path = locale switch { "pt-PT" => "locales/pt-PT/common.json", "pt-BR" => "locales/pt-BR/common.json", "en-US" => "locales/en-US/common.json", "es-ES" => "locales/es-ES/common.json", _ => throw new ArgumentOutOfRangeException(nameof(locale)) };
        return JsonDocument.Parse(await File.ReadAllTextAsync(Path.Join(AppContext.BaseDirectory, path)));
    }
    private static IEnumerable<string> Strings(JsonElement value) => value.ValueKind switch { JsonValueKind.String => [value.GetString()!], JsonValueKind.Object => value.EnumerateObject().SelectMany(x => Strings(x.Value)), JsonValueKind.Array => value.EnumerateArray().SelectMany(Strings), _ => [] };
    private static async Task Error(HttpResponseMessage response, int status, JsonDocument dictionary, string title, string message)
    {
        using (response) { Assert.Equal(status, (int)response.StatusCode); using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); Assert.Equal(dictionary.RootElement.GetProperty(title).GetString(), body.RootElement.GetProperty("title").GetString()); var strings = Strings(body.RootElement).ToArray(); Assert.Contains(dictionary.RootElement.GetProperty(message).GetString(), strings); Assert.DoesNotContain(strings, x => x.StartsWith("Api.") || x.StartsWith("Domain.") || x.StartsWith("Application.")); }
    }

    private sealed class Host : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly SymmetricSecurityKey _key;
        private readonly Uri _address;
        private readonly IServiceProvider? _previousProvider;
        public UserPreferencesSaveControl SaveControl { get; }

        private Host(WebApplication app, SymmetricSecurityKey key, UserPreferencesSaveControl control)
        {
            _app = app;
            _key = key;
            SaveControl = control;
            var server = app.Services.GetRequiredService<IServer>();
            _address = new Uri(server.Features.Get<IServerAddressesFeature>()!.Addresses.Single());
            _previousProvider = ServiceProviderHolder.ServiceProvider;
            ServiceProviderHolder.ServiceProvider = app.Services;
        }

        public static async Task<Host> StartAsync()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
            var control = new UserPreferencesSaveControl();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserApiService>();
            builder.Services.AddScoped<INotify, Notify>();
            builder.Services.AddSingleton<IMapper>(UserPreferencesTestSupport.Mapper());
            var database = Guid.NewGuid().ToString();
            builder.Services.AddDbContext<GeritDbContext>(options => options.UseInMemoryDatabase(database).AddInterceptors(control));
            builder.Services.AddScoped<IUserPreferencesDataRepository, UserPreferencesDataRepository>();
            builder.Services.AddScoped<IEntityDomainValidator<UserPreferencesEntity>, UserPreferencesValidator>();
            builder.Services.AddScoped<IUserPreferencesDomainService, UserPreferencesDomainService>();
            builder.Services.AddScoped<IUserPreferencesAppService, UserPreferencesAppService>();

            builder.Services.AddRouteValidatorSetup();

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "user-preferences-tests",
                    ValidateAudience = true,
                    ValidAudience = "user-preferences-tests",
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
            // Observa as duas culturas no mesmo request de erro, sem outro mecanismo de locale.
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["X-Test-Culture"] = CultureInfo.CurrentCulture.Name;
                    context.Response.Headers["X-Test-UI-Culture"] = CultureInfo.CurrentUICulture.Name;
                    return Task.CompletedTask;
                });
                await next(context);
            });
            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapUserPreferencesEndpoints();
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GeritDbContext>();
                var tenant = new VianaHub.Global.Gerit.Domain.Entities.Billing.TenantEntity(1, 1, "Fixture tenant", "fixture@example.test", null, null, null, 17);
                typeof(VianaHub.Global.Gerit.Domain.Entities.Billing.TenantEntity).GetProperty("Id")!.SetValue(tenant, 7);
                var user = new UserEntity(7, "Fixture user", "fixture@example.test", "fixture", null, 17);
                typeof(UserEntity).GetProperty("Id")!.SetValue(user, 17);
                db.AddRange(tenant, user);
                await db.SaveChangesAsync();
            }
            await app.StartAsync();
            return new Host(app, key, control);
        }

        public HttpClient Client(IEnumerable<string> actions, bool grouped = true, string role = "BackOffice", bool authenticated = true)
        {
            var client = new HttpClient { BaseAddress = _address };
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-PT");
            if (!authenticated) return client;
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "17"), new(ClaimTypes.Role, role), new("tenant_id", "7")
            };
            if (grouped) claims.Add(new Claim("permissions", JsonSerializer.Serialize(new Dictionary<string, string[]> { ["UserPreferences"] = actions.ToArray() }), JsonClaimValueTypes.Json));
            else claims.AddRange(actions.Select(action => new Claim("permission", "UserPreferences:" + action)));
            var token = new JwtSecurityToken("user-preferences-tests", "user-preferences-tests", claims, expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
            return client;
        }

        public async Task<int> SeedOtherUserPreferencesAsync()
        {
            using var scope = _app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GeritDbContext>();
            var user = new UserEntity(7, "Other user", "other@example.test", "other", null, 18);
            typeof(UserEntity).GetProperty("Id")!.SetValue(user, 18);
            var preferences = new UserPreferencesEntity(7, 18, "light", "EUR", "pt-PT", "Europe/Lisbon", "dd/MM/yyyy", "24h", TimeSpan.FromHours(9), TimeSpan.FromHours(18), 18);
            db.AddRange(user, preferences);
            await db.SaveChangesAsync();
            return preferences.Id;
        }

        public async ValueTask DisposeAsync()
        {
            ServiceProviderHolder.ServiceProvider = _previousProvider;
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
