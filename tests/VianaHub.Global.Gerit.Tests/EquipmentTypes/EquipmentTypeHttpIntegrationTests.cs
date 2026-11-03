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
using VianaHub.Global.Gerit.Api.Endpoints.Business;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Api.Middleware;
using VianaHub.Global.Gerit.Api.Services;
using VianaHub.Global.Gerit.Api.Validators.Business.EquipmentType;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.EquipmentType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EquipmentType;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Services.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Validators.Business.EquipmentType;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Repository.Business;
using Xunit.Abstractions;

namespace VianaHub.Global.Gerit.Tests.EquipmentTypes;

[CollectionDefinition("EquipmentType HTTP", DisableParallelization = true)]
public sealed class EquipmentTypeHttpCollection { }

// HTTP real em loopback com JwtBearer e filtros reais; persistência EF InMemory isolada.
// Não substitui prova de RLS, collation ou constraints em SQL Server.
[Collection("EquipmentType HTTP")]
public class EquipmentTypeHttpIntegrationTests(ITestOutputHelper output)
{
    private static readonly string[] Actions = ["GetAll", "GetBy", "GetPaged", "Create", "Update", "Activate", "Deactivate", "Delete"];

    [Fact]
    public async Task CrudJourney_PreservesContractAndIdempotency()
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client(Actions);
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-PT");
        var first = await Create(client, "Alpha");
        var detail = await client.GetFromJsonAsync<EquipmentTypeDetailResponse>($"/v1/equipment-types/{first}");
        Assert.NotNull(detail);
        Assert.Equal(7, detail.TenantId);
        Assert.Equal("Alpha", detail.Name);
        Assert.Equal("Description", detail.Description);
        Assert.True(detail.IsActive);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/v1/equipment-types/", Payload("Alpha"))).StatusCode);
        var second = await Create(client, "Beta");
        Assert.NotEqual(first, second);
        Assert.Equal(2, (await client.GetFromJsonAsync<EquipmentTypeResponse[]>("/v1/equipment-types/"))!.Length);
        var page = await client.GetFromJsonAsync<ListPageResponse<EquipmentTypeResponse>>(
            "/v1/equipment-types/paged?PageNumber=2&PageSize=1&SortBy=Name&SortDirection=asc");
        Assert.NotNull(page);
        Assert.Equal(2, page.TotalItems);
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(1, page.PageSize);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(second, Assert.Single(page.Items).Id);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/v1/equipment-types/{first}", Payload("Alpha", "Edited"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/v1/equipment-types/{first}", Payload("Beta"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/v1/equipment-types/{first}", Payload("Gamma", "Renamed"))).StatusCode);
        detail = await client.GetFromJsonAsync<EquipmentTypeDetailResponse>($"/v1/equipment-types/{first}");
        Assert.Equal("Gamma", detail!.Name);
        Assert.Equal("Renamed", detail.Description);
        foreach (var operation in new[] { "deactivate", "deactivate", "activate", "activate" })
        {
            Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/equipment-types/{first}/{operation}", null)).StatusCode);
            detail = await client.GetFromJsonAsync<EquipmentTypeDetailResponse>($"/v1/equipment-types/{first}");
            Assert.Equal(operation == "activate", detail!.IsActive);
        }
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/v1/equipment-types/{first}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/equipment-types/{first}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.DeleteAsync($"/v1/equipment-types/{first}")).StatusCode);
        Assert.Equal(second, Assert.Single((await client.GetFromJsonAsync<EquipmentTypeResponse[]>("/v1/equipment-types/"))!).Id);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/v1/equipment-types/{second}")).StatusCode);
    }

    [Theory]
    [InlineData("GetAll", "GET", "/v1/equipment-types/", 200)]
    [InlineData("GetBy", "GET", "/v1/equipment-types/{id}", 200)]
    [InlineData("GetPaged", "GET", "/v1/equipment-types/paged", 200)]
    [InlineData("Create", "POST", "/v1/equipment-types/", 201)]
    [InlineData("Update", "PUT", "/v1/equipment-types/{id}", 204)]
    [InlineData("Activate", "PATCH", "/v1/equipment-types/{id}/activate", 200)]
    [InlineData("Deactivate", "PATCH", "/v1/equipment-types/{id}/deactivate", 200)]
    [InlineData("Delete", "DELETE", "/v1/equipment-types/{id}", 200)]
    public async Task EveryEndpoint_RequiresItsExactEquipmentTypesPermission(string action, string method, string route, int success)
    {
        await using var host = await Host.StartAsync();
        using var setup = host.Client(Actions);
        var id = await Create(setup, "Existing");
        var target = route.Replace("{id}", id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        using var denied = host.Client(Actions.Where(a => a != action));
        using var forbiddenRequest = Request(method, target, "Other");
        Assert.Equal(HttpStatusCode.Forbidden, (await denied.SendAsync(forbiddenRequest)).StatusCode);
        using var allowed = host.Client([action]);
        using var allowedRequest = Request(method, target, "Other");
        Assert.Equal(success, (int)(await allowed.SendAsync(allowedRequest)).StatusCode);
    }

    [Fact]
    public async Task AuthenticationAndRoleAreRequiredEvenWithPermissionClaims()
    {
        await using var host = await Host.StartAsync();
        using var anonymous = host.Client([], authenticated: false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/v1/equipment-types/")).StatusCode);
        using var wrongRole = host.Client(Actions, role: "Viewer");
        Assert.Equal(HttpStatusCode.Forbidden, (await wrongRole.GetAsync("/v1/equipment-types/")).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthorizationAcceptsSupportedPermissionClaimFormats(bool grouped)
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client(["GetAll"], grouped: grouped);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/v1/equipment-types/")).StatusCode);
    }

    [Theory]
    [InlineData("pt-PT")]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    [InlineData("es-ES")]
    public async Task CreateAndUpdateDuplicatesUseSameLocalized409Message(string locale)
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client(Actions);
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(locale);
        var first = await Create(client, "First");
        await Create(client, "Second");
        using var dictionary = await ReadDictionaryAsync(locale);
        var expected = dictionary.RootElement.GetProperty("Application.Service.EquipmentType.Create.ResourceAlreadyExists").GetString();
        foreach (var response in new[]
        {
            await client.PostAsJsonAsync("/v1/equipment-types/", Payload("Second")),
            await client.PutAsJsonAsync($"/v1/equipment-types/{first}", Payload("Second"))
        })
        {
            using (response)
            {
                Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Assert.Equal(dictionary.RootElement.GetProperty("Api.Helpers.NotifyExtensions.Error.Conflict").GetString(),
                    body.RootElement.GetProperty("title").GetString());
                var strings = Strings(body.RootElement).ToArray();
                Assert.Contains(expected, strings);
                AssertLocalizedPayload(body.RootElement, locale);
                AssertCultures(response, locale);
            }
        }
    }

    [Theory]
    [InlineData("", "Description")]
    [InlineData("Crew", null)]
    public async Task InvalidPayloadIs400AtCreateAndUpdate(string name, string? description)
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client(Actions);
        var id = await Create(client, "Valid");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/v1/equipment-types/", Payload(name, description))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/v1/equipment-types/{id}", Payload(name, description))).StatusCode);
        Assert.Equal("Valid", (await client.GetFromJsonAsync<EquipmentTypeDetailResponse>($"/v1/equipment-types/{id}"))!.Name);
    }

    [Theory]
    [InlineData("pt-PT")]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    [InlineData("es-ES")]
    public async Task Validation_AcceptLanguage_SetsBothCulturesAndPreservesErrorShape(string locale)
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client(Actions);
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(locale);
        var id = await Create(client, "Valid");
        using var dictionary = await ReadDictionaryAsync(locale);
        var expected = dictionary.RootElement.GetProperty("Api.EquipmentType.NameRequired").GetString();
        foreach (var response in new[]
        {
            await client.PostAsJsonAsync("/v1/equipment-types/", Payload("")),
            await client.PutAsJsonAsync($"/v1/equipment-types/{id}", Payload(""))
        })
        {
            using (response)
            {
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                AssertCultures(response, locale);
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Assert.Equal(new[] { "errors", "title" }, body.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray());
                Assert.Equal(dictionary.RootElement.GetProperty("Api.Helpers.NotifyExtensions.Error.ValidationError").GetString(),
                    body.RootElement.GetProperty("title").GetString());
                var errors = body.RootElement.GetProperty("errors");
                Assert.Equal("Name", Assert.Single(errors.EnumerateObject()).Name);
                Assert.Equal(expected, Assert.Single(errors.GetProperty("Name").EnumerateArray()).GetString());
                AssertLocalizedPayload(body.RootElement, locale);
                output.WriteLine("EquipmentType validation locale={0}; cultures={1}/{2}; status=400; title={3}", locale,
                    response.Headers.GetValues("X-Test-Culture").Single(), response.Headers.GetValues("X-Test-UI-Culture").Single(),
                    body.RootElement.GetProperty("title").GetString());
            }
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("fr-FR")]
    public async Task MissingOrUnsupportedLanguage_UsesExistingPtPtFallbackWithoutRejectingValidRequests(string? language)
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client(Actions);
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        if (language is not null) client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);
        using var valid = await client.GetAsync("/v1/equipment-types/");
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        AssertCultures(valid, "pt-PT");
        await Create(client, "Valid without supported header");
        using var response = await client.PostAsJsonAsync("/v1/equipment-types/", Payload(""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertCultures(response, "pt-PT");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        using var dictionary = await ReadDictionaryAsync("pt-PT");
        Assert.Equal(dictionary.RootElement.GetProperty("Api.Helpers.NotifyExtensions.Error.ValidationError").GetString(),
            body.RootElement.GetProperty("title").GetString());
        Assert.Contains(dictionary.RootElement.GetProperty("Api.EquipmentType.NameRequired").GetString(), Strings(body.RootElement));
        AssertLocalizedPayload(body.RootElement, "pt-PT");
    }

    [Theory]
    [InlineData("pt-PT")]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    [InlineData("es-ES")]
    public async Task AuthorizationDenied_RespectsAcceptLanguage(string locale)
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client(Actions.Where(a => a != "Create"));
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(locale);
        using var response = await client.PostAsJsonAsync("/v1/equipment-types/", Payload("Forbidden"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        AssertCultures(response, locale);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        using var dictionary = await ReadDictionaryAsync(locale);
        Assert.Equal(dictionary.RootElement.GetProperty("Api.Authorization.AccessDenied").GetString(),
            body.RootElement.GetProperty("title").GetString());
        Assert.Contains(dictionary.RootElement.GetProperty("Api.Authorization.Forbidden.Permission.Detail").GetString(), Strings(body.RootElement));
        AssertLocalizedPayload(body.RootElement, locale);
    }

    [Theory]
    [InlineData("pt-PT")]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    [InlineData("es-ES")]
    public async Task GoneAndRepeatedDelete_RespectsAcceptLanguage(string locale)
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client(Actions);
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(locale);
        var id = await Create(client, "Removed");
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/v1/equipment-types/{id}")).StatusCode);
        using var dictionary = await ReadDictionaryAsync(locale);
        foreach (var response in new[] { await client.GetAsync($"/v1/equipment-types/{id}"), await client.DeleteAsync($"/v1/equipment-types/{id}") })
        {
            using (response)
            {
                Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
                AssertCultures(response, locale);
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Assert.Equal(dictionary.RootElement.GetProperty("Api.Helpers.NotifyExtensions.Error.Gone").GetString(),
                    body.RootElement.GetProperty("title").GetString());
                // Cada operação mantém sua chave de localização existente.
                Assert.Contains(dictionary.RootElement.GetProperty(response.RequestMessage!.Method == HttpMethod.Get ? "Application.Service.EquipmentType.GetById.ResourceNotFound" : "Application.Service.EquipmentType.Delete.ResourceNotFound").GetString(), Strings(body.RootElement));
                AssertLocalizedPayload(body.RootElement, locale);
            }
        }
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Update")]
    [InlineData("Activate")]
    [InlineData("Deactivate")]
    [InlineData("Delete")]
    public async Task NoRowsPersisted_DoesNotProduceHttpSuccess(string operation)
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client(Actions);
        var id = operation == "Create" ? 0 : await Create(client, "Existing");
        if (operation == "Activate")
            Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/equipment-types/{id}/deactivate", null)).StatusCode);
        host.SaveControl.Suppress = true;
        using var response = operation switch
        {
            "Create" => await client.PostAsJsonAsync("/v1/equipment-types/", Payload("New")),
            "Update" => await client.PutAsJsonAsync($"/v1/equipment-types/{id}", Payload("Changed")),
            "Delete" => await client.DeleteAsync($"/v1/equipment-types/{id}"),
            _ => await client.PatchAsync($"/v1/equipment-types/{id}/{operation.ToLowerInvariant()}", null)
        };
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        host.SaveControl.Suppress = false;
        if (operation == "Create") Assert.Empty((await client.GetFromJsonAsync<EquipmentTypeResponse[]>("/v1/equipment-types/"))!);
        else
        {
            var detail = await client.GetFromJsonAsync<EquipmentTypeDetailResponse>($"/v1/equipment-types/{id}");
            Assert.NotNull(detail);
            Assert.Equal("Existing", detail.Name);
            Assert.Equal(operation != "Activate", detail.IsActive);
        }
    }

    [Theory]
    [InlineData("pt-PT")]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    [InlineData("es-ES")]
    public async Task NullDescription_IsLocalized400AndEmptyDescriptionRemainsValid(string locale)
    {
        await using var host = await Host.StartAsync();
        using var client = host.Client(Actions);
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(locale);
        var id = await Create(client, "Existing");
        using var dictionary = await ReadDictionaryAsync(locale);
        foreach (var response in new[]
        {
            await client.PostAsJsonAsync("/v1/equipment-types/", Payload("New", null)),
            await client.PutAsJsonAsync($"/v1/equipment-types/{id}", Payload("Edited", null))
        })
        {
            using (response)
            {
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Assert.Contains(dictionary.RootElement.GetProperty("Api.EquipmentType.DescriptionRequired").GetString(), Strings(body.RootElement));
                AssertLocalizedPayload(body.RootElement, locale);
            }
        }
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/v1/equipment-types/{id}", Payload("Existing", ""))).StatusCode);
        Assert.Equal("", (await client.GetFromJsonAsync<EquipmentTypeDetailResponse>($"/v1/equipment-types/{id}"))!.Description);
    }

    private static object Payload(string name, string? description = "Description") => new { name, description, tenantId = 999 };

    private static async Task<int> Create(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync("/v1/equipment-types/", Payload(name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<GenericResponse>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        return created.Id;
    }

    private static HttpRequestMessage Request(string method, string target, string name) => new(new HttpMethod(method), target)
    {
        Content = method is "POST" or "PUT" ? JsonContent.Create(Payload(name)) : null
    };

    private static IEnumerable<string> Strings(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => [element.GetString()!],
        JsonValueKind.Object => element.EnumerateObject().SelectMany(p => Strings(p.Value)),
        JsonValueKind.Array => element.EnumerateArray().SelectMany(Strings),
        _ => []
    };

    private static async Task<JsonDocument> ReadDictionaryAsync(string locale)
    {
        // Somente caminhos literais conhecidos podem ser resolvidos dentro do output de testes.
        var relativePath = locale switch
        {
            "pt-PT" => "locales/pt-PT/common.json",
            "pt-BR" => "locales/pt-BR/common.json",
            "en-US" => "locales/en-US/common.json",
            "es-ES" => "locales/es-ES/common.json",
            _ => throw new ArgumentOutOfRangeException(nameof(locale))
        };
        return JsonDocument.Parse(await File.ReadAllTextAsync(Path.Join(AppContext.BaseDirectory, relativePath)));
    }

    private static void AssertCultures(HttpResponseMessage response, string locale)
    {
        Assert.Equal(locale, response.Headers.GetValues("X-Test-Culture").Single());
        Assert.Equal(locale, response.Headers.GetValues("X-Test-UI-Culture").Single());
    }

    private static void AssertLocalizedPayload(JsonElement payload, string locale)
    {
        var values = Strings(payload).ToArray();
        Assert.DoesNotContain(values, value => value.StartsWith("Api.", StringComparison.Ordinal) ||
            value.StartsWith("Application.", StringComparison.Ordinal) || value.StartsWith("Domain.", StringComparison.Ordinal));
        if (locale is "en-US" or "es-ES")
        {
            Assert.DoesNotContain(values, value => value.Contains("Erro de validação", StringComparison.Ordinal) ||
                value.Contains("Não tem permissão", StringComparison.Ordinal) || value.Contains("Já existe um tipo", StringComparison.Ordinal) ||
                value.Contains("O nome do tipo", StringComparison.Ordinal) || value.Contains("Tipo de equipamento", StringComparison.Ordinal));
        }
    }

    private sealed class Host : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly SymmetricSecurityKey _key;
        private readonly Uri _address;
        private readonly IServiceProvider? _previousProvider;
        public EquipmentTypeSaveControl SaveControl { get; }

        private Host(WebApplication app, SymmetricSecurityKey key, EquipmentTypeSaveControl control)
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
            var control = new EquipmentTypeSaveControl();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserApiService>();
            builder.Services.AddScoped<INotify, Notify>();
            builder.Services.AddSingleton<IMapper>(EquipmentTypeTestSupport.Mapper());
            var database = Guid.NewGuid().ToString();
            builder.Services.AddDbContext<GeritDbContext>(options => options.UseInMemoryDatabase(database).AddInterceptors(control));
            builder.Services.AddScoped<IEquipmentTypeDataRepository, EquipmentTypeDataRepository>();
            builder.Services.AddScoped<IEntityDomainValidator<EquipmentTypeEntity>, EquipmentTypeValidator>();
            builder.Services.AddScoped<IEquipmentTypeDomainService, EquipmentTypeDomainService>();
            builder.Services.AddScoped<IEquipmentTypeAppService, EquipmentTypeAppService>();
            builder.Services.AddSingleton(Mock.Of<IFileValidationService>());
            builder.Services.AddRouteValidatorSetup();

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "equipment-type-tests",
                    ValidateAudience = true,
                    ValidAudience = "equipment-type-tests",
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
            app.MapEquipmentTypeEndpoints();
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
            if (grouped) claims.Add(new Claim("permissions", JsonSerializer.Serialize(new Dictionary<string, string[]> { ["EquipmentTypes"] = actions.ToArray() }), JsonClaimValueTypes.Json));
            else claims.AddRange(actions.Select(action => new Claim("permission", "EquipmentTypes:" + action)));
            var token = new JwtSecurityToken("equipment-type-tests", "equipment-type-tests", claims, expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
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
