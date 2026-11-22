using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using VianaHub.Global.Gerit.Api.Endpoints.Business;
using VianaHub.Global.Gerit.Api.Services;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VianaHub.Global.Gerit.Api.Validators.Business.Client;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Client;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Client;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Mappings.Business;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Validators.Business.Client;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Tests.Clients;

public sealed class ClientControlledVocabularyTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(null, null)]
    [InlineData("male", "PT")]
    [InlineData("female", "BR")]
    [InlineData("non-binary", "US")]
    [InlineData("prefer-not-to-say", "CA")]
    public void CreateRouteAndDomain_AcceptCanonicalValues(string? gender, string? nationality)
    {
        var request = Create(gender, nationality);
        Assert.True(new CreateClientRouteValidator(Localization()).Validate(request).IsValid);
        Assert.True(new CreateClientValidator(Localization()).Validate(Entity(gender, nationality)).IsValid);
    }

    [Theory]
    [InlineData("M", "PT")]
    [InlineData("F", "PT")]
    [InlineData("Masc", "PT")]
    [InlineData("Masculino", "PT")]
    [InlineData("MALE", "PT")]
    [InlineData("Male", "PT")]
    [InlineData(" male", "PT")]
    [InlineData("male ", "PT")]
    [InlineData("", "PT")]
    [InlineData("unknown", "PT")]
    [InlineData("other", "PT")]
    [InlineData("male", "Português")]
    [InlineData("male", "pt")]
    [InlineData("male", " PT")]
    [InlineData("male", "PT ")]
    [InlineData("male", "")]
    [InlineData("male", "ZZ")]
    [InlineData("male", "CN")]
    public void CreateRouteAndDomain_RejectAliasesCaseWhitespaceAndOutOfRegionCodes(string? gender, string? nationality)
    {
        Assert.False(new CreateClientRouteValidator(Localization()).Validate(Create(gender, nationality)).IsValid);
        Assert.False(new CreateClientValidator(Localization()).Validate(Entity(gender, nationality)).IsValid);
    }

    [Theory]
    [InlineData("M", "Português")]
    [InlineData("F", "Brasileira")]
    [InlineData("Masc", "Portugal")]
    [InlineData("Masculino", " PT ")]
    [InlineData(" male ", "pt")]
    [InlineData("unknown-legacy", "Not-a-country")]
    [InlineData("", " ")]
    public async Task Update_LegacyExactOrdinalAndOmissionPreservePersistedStrings(string gender, string nationality)
    {
        var fixture = new Fixture(Entity(gender, nationality));
        Assert.True(await fixture.Service.UpdateAsync(23, Update(gender, nationality), default));
        Assert.Equal(gender, fixture.Client.Gender);
        Assert.Equal(nationality, fixture.Client.Nationality);
        var omitted = JsonSerializer.Deserialize<UpdateClientRequest>("{\"partyTypeId\":1,\"acquisitionSourceTypeId\":1,\"name\":\"Omitted update\",\"statusDomainId\":1,\"statusDefinitionId\":1,\"genderProvided\":true,\"nationalityProvided\":true}", JsonOptions)!;
        Assert.True(await fixture.Service.UpdateAsync(23, omitted, default));
        Assert.Equal(gender, fixture.Client.Gender);
        Assert.Equal(nationality, fixture.Client.Nationality);
        Assert.Equal("Omitted update", fixture.Client.Name);
        var detail = ClientMapper.ToDetail(fixture.Client);
        Assert.Equal(gender, detail.Gender);
        Assert.Equal(nationality, detail.Nationality);
    }

    [Theory]
    [InlineData("M", "Português", "m", "Português")]
    [InlineData("M", "Português", "M ", "Português")]
    [InlineData("M", "Português", "M", "PORTUGUÊS")]
    [InlineData("M", "Português", "M", "Português ")]
    [InlineData("M", "Português", "F", "Português")]
    [InlineData("male", "PT", "M", "PT")]
    [InlineData("male", "PT", "male", "Português")]
    public async Task Update_RejectsNewLegacyValueBeforeAnyMutation(string oldGender, string oldNationality, string gender, string nationality)
    {
        var fixture = new Fixture(Entity(oldGender, oldNationality));
        Assert.False(await fixture.Service.UpdateAsync(23, Update(gender, nationality), default));
        Assert.Equal(oldGender, fixture.Client.Gender);
        Assert.Equal(oldNationality, fixture.Client.Nationality);
        Assert.Equal("Original", fixture.Client.Name);
        Assert.Null(fixture.Client.ModifiedAt);
        Assert.Equal(HttpStatusCode.BadRequest, fixture.Notify.GetStatusCode());
        fixture.Domain.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("male", "PT")]
    [InlineData("female", "BR")]
    [InlineData("non-binary", "US")]
    [InlineData("prefer-not-to-say", "CA")]
    public async Task Update_LegacyCanBecomeCanonicalOrExplicitNullWithoutNormalizingInput(string? gender, string? nationality)
    {
        var fixture = new Fixture(Entity("M", "Português"));
        var body = new Dictionary<string, object?>
        {
            ["partyTypeId"] = 1,
            ["acquisitionSourceTypeId"] = 1,
            ["name"] = "Changed",
            ["statusDomainId"] = 1,
            ["statusDefinitionId"] = 1,
            ["gender"] = gender,
            ["nationality"] = nationality,
            ["genderProvided"] = false,
            ["nationalityProvided"] = false
        };
        var request = JsonSerializer.Deserialize<UpdateClientRequest>(JsonSerializer.Serialize(body), JsonOptions)!;
        Assert.True(await fixture.Service.UpdateAsync(23, request, default));
        Assert.Equal(gender, fixture.Client.Gender);
        Assert.Equal(nationality, fixture.Client.Nationality);
        Assert.DoesNotContain(JsonSerializer.SerializeToElement(request, JsonOptions).EnumerateObject(), property => property.Name.EndsWith("Provided", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Update_CannotBorrowLegacyExceptionFromAnotherTenant()
    {
        var fixture = new Fixture(Entity("M", "Português", tenant: 2));
        Assert.False(await fixture.Service.UpdateAsync(23, Update("M", "Português"), default));
        Assert.Equal(HttpStatusCode.Gone, fixture.Notify.GetStatusCode());
        Assert.Equal("Original", fixture.Client.Name);
        Assert.Equal("M", fixture.Client.Gender);
        Assert.Equal("Português", fixture.Client.Nationality);
        fixture.Domain.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("male", null)]
    [InlineData(null, "PT")]
    [InlineData("M", "Português")]
    [InlineData("", "")]
    public async Task Organization_RequiresNullIndividualVocabulary(string? gender, string? nationality)
    {
        var fixture = new Fixture(Entity(null, null));
        var request = Update(gender, nationality);
        request.PartyTypeId = 2;
        Assert.False(await fixture.Service.UpdateAsync(23, request, default));
        Assert.Null(fixture.Client.Gender);
        Assert.Null(fixture.Client.Nationality);
        Assert.Equal((byte)1, fixture.Client.PartyTypeId);
        fixture.Domain.VerifyNoOtherCalls();
    }

    [Fact]
    public void ReadAndEfContracts_RemainNullableStringsWithExistingColumnsAndLengths()
    {
        Assert.Equal(typeof(string), typeof(ClientEntity).GetProperty(nameof(ClientEntity.Gender))!.PropertyType);
        Assert.Equal(typeof(string), typeof(ClientEntity).GetProperty(nameof(ClientEntity.Nationality))!.PropertyType);
        Assert.Equal(typeof(string), typeof(ClientDetailResponse).GetProperty(nameof(ClientDetailResponse.Gender))!.PropertyType);
        Assert.Equal(typeof(string), typeof(ClientDetailResponse).GetProperty(nameof(ClientDetailResponse.Nationality))!.PropertyType);
        using var db = new GeritDbContext(new DbContextOptionsBuilder<GeritDbContext>().UseSqlServer("Server=unused.invalid;Database=metadata-only;Integrated Security=true;Encrypt=true").Options);
        var entity = db.Model.FindEntityType(typeof(ClientEntity))!;
        var table = StoreObjectIdentifier.Table("Clients", "dbo");
        foreach (var (name, length, columnType) in new[] { ("Gender", 30, "NVARCHAR(30)"), ("Nationality", 100, "NVARCHAR(100)") })
        {
            var property = entity.FindProperty(name)!;
            Assert.Equal(typeof(string), property.ClrType);
            Assert.True(property.IsNullable);
            Assert.Equal(name, property.GetColumnName(table));
            Assert.Equal(length, property.GetMaxLength());
            Assert.Equal(columnType, property.GetColumnType());
            Assert.Null(property.GetValueConverter());
        }
    }

    private static CreateClientRequest Create(string? gender, string? nationality) => new()
    {
        PartyTypeId = 1,
        AcquisitionSourceTypeId = 1,
        Name = "Valid",
        StatusDomainId = 1,
        StatusDefinitionId = 1,
        Gender = gender,
        Nationality = nationality
    };

    private static UpdateClientRequest Update(string? gender, string? nationality) => new()
    {
        PartyTypeId = 1,
        AcquisitionSourceTypeId = 1,
        Name = "Changed",
        StatusDomainId = 1,
        StatusDefinitionId = 1,
        Gender = gender,
        Nationality = nationality
    };

    internal static ClientEntity Entity(string? gender, string? nationality, int tenant = 1)
    {
        var entity = new ClientEntity(tenant, 1, 1, null, null, "Original", null, null, false, null, null,
            null, gender, nationality, null, null, null, 1, 1, 1);
        typeof(ClientEntity).GetProperty(nameof(ClientEntity.Id))!.SetValue(entity, 23);
        return entity;
    }

    private static ILocalizationService Localization()
    {
        var mock = new Mock<ILocalizationService>();
        mock.Setup(value => value.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        mock.Setup(value => value.GetMessage(It.IsAny<string>(), It.IsAny<object?[]>())).Returns((string key, object?[] _) => key);
        return mock.Object;
    }

    private sealed class Fixture
    {
        public ClientEntity Client { get; }
        public Mock<IClientDomainService> Domain { get; } = new(MockBehavior.Strict);
        public Notify Notify { get; } = new();
        public ClientAppService Service { get; }
        public Fixture(ClientEntity client)
        {
            Client = client;
            var repo = new Mock<IClientRepository>(MockBehavior.Strict);
            repo.Setup(value => value.GetByIdAsync(23, It.IsAny<CancellationToken>())).ReturnsAsync(client);
            var current = new Mock<ICurrentUserService>();
            current.Setup(value => value.GetTenantId()).Returns(1);
            current.Setup(value => value.GetUserId()).Returns(1);
            Domain.Setup(value => value.UpdateAsync(client, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            Service = new ClientAppService(repo.Object, Domain.Object, Notify, Localization(), current.Object,
                Mock.Of<IFileValidationService>(), NullLogger<ClientAppService>.Instance);
        }
    }
}

[Collection("Client aggregate HTTP")]
public sealed class ClientControlledVocabularyHttpTests
{
    [Fact]
    public async Task HttpCreateAndUpdate_PersistCanonicalCodesAndRejectAliasesWithoutMutation()
    {
        await using var fixture = await ClientAggregateHttpFixture.StartAsync();
        using var client = fixture.CreateAuthorizedClient();
        foreach (var gender in new string?[] { "male", "female", "non-binary", "prefer-not-to-say", null })
        {
            var body = Request(fixture, gender, "PT");
            using var create = await client.PostAsJsonAsync("/v1/clients", body);
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
            var detail = await client.GetFromJsonAsync<ClientDetailResponse>($"/v1/clients/{id}");
            Assert.Equal(gender, detail!.Gender);
            Assert.Equal("PT", detail.Nationality);
            var invalid = Request(fixture, "M", "Português");
            invalid["name"] = "Must not persist";
            using var rejected = await client.PutAsJsonAsync($"/v1/clients/{id}", invalid);
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
            var unchanged = await client.GetFromJsonAsync<ClientDetailResponse>($"/v1/clients/{id}");
            Assert.Equal(detail.Name, unchanged!.Name);
            Assert.Equal(gender, unchanged.Gender);
            Assert.Equal("PT", unchanged.Nationality);
            var omitted = Request(fixture, gender, "PT");
            omitted.Remove("gender"); omitted.Remove("nationality");
            omitted["genderProvided"] = true; omitted["nationalityProvided"] = true;
            using var preserving = await client.PutAsJsonAsync($"/v1/clients/{id}", omitted);
            Assert.Equal(HttpStatusCode.OK, preserving.StatusCode);
            var preserved = await client.GetFromJsonAsync<ClientDetailResponse>($"/v1/clients/{id}");
            Assert.Equal(gender, preserved!.Gender);
            Assert.Equal("PT", preserved.Nationality);
            var clear = Request(fixture, null, null);
            clear["genderProvided"] = false; clear["nationalityProvided"] = false;
            using var cleared = await client.PutAsJsonAsync($"/v1/clients/{id}", clear);
            Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
            var empty = await client.GetFromJsonAsync<ClientDetailResponse>($"/v1/clients/{id}");
            Assert.Null(empty!.Gender); Assert.Null(empty.Nationality);
        }
        foreach (var (gender, nationality) in new[] { ("M", "PT"), ("F", "PT"), ("Masc", "PT"), ("Masculino", "PT"), ("Male", "PT"), ("male ", "PT"), ("male", "Português"), ("male", "pt"), ("male", " PT"), ("male", "CN") })
        {
            using var rejected = await client.PostAsJsonAsync("/v1/clients", Request(fixture, gender, nationality));
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        }
    }

    [Fact]
    public async Task HttpValidationErrors_AreLocalizedForAllFourCultures()
    {
        await using var fixture = await ClientAggregateHttpFixture.StartAsync();
        foreach (var culture in new[] { "pt-PT", "pt-BR", "en-US", "es-ES" })
        {
            using var client = fixture.CreateAuthorizedClient(culture: culture);
            using var response = await client.PostAsJsonAsync("/v1/clients", Request(fixture, "M", "Português"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var text = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("Client.Gender", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Client.Nationality", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Exception", text, StringComparison.OrdinalIgnoreCase);
            var error = JsonSerializer.Deserialize<ErrorResponse>(text, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            var messages = error.Errors.Values.SelectMany(values => values).ToArray();
            var context = new DefaultHttpContext();
            context.Items["RequestCulture"] = culture;
            var localization = new LocalizationService(new HttpContextAccessor { HttpContext = context });
            Assert.Contains(localization.GetMessage("Domain.Client.GenderControlledValue"), messages);
            Assert.Contains(localization.GetMessage("Domain.Client.NationalityControlledValue"), messages);
        }
    }

    [Fact]
    public async Task HttpDetail_MaterializedLegacyStringsAreReturnedWithoutConversion()
    {
        // O provider é exclusivamente InMemory; nenhum host de produção nem SQL é iniciado.
        await using var database = new GeritDbContext(new DbContextOptionsBuilder<GeritDbContext>()
            .UseInMemoryDatabase("client-legacy-" + Guid.NewGuid()).Options);
        var legacy = ClientControlledVocabularyTests.Entity("M", "Português");
        database.Add(legacy);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        var materialized = await database.Set<ClientEntity>().AsNoTracking().SingleAsync();
        Assert.NotSame(legacy, materialized);
        var repository = new Mock<IClientRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync(23, It.IsAny<CancellationToken>())).ReturnsAsync(materialized);
        var current = new Mock<ICurrentUserService>();
        current.Setup(value => value.GetTenantId()).Returns(1);
        current.Setup(value => value.GetUserId()).Returns(1);
        var localization = new Mock<ILocalizationService>();
        var notify = new Notify();
        var service = new ClientAppService(repository.Object, Mock.Of<IClientDomainService>(), notify, localization.Object,
            current.Object, Mock.Of<IFileValidationService>(), NullLogger<ClientAppService>.Instance);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IClientAppService>(service);
        builder.Services.AddSingleton<INotify>(notify);
        builder.Services.AddSingleton(localization.Object);
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapClientEndpoints();
        await app.StartAsync();
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-Tenant", "1");
        client.DefaultRequestHeaders.Add("X-Test-Permission-Resource", "Clients");
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "GetBy");
        using var response = await client.GetAsync("/v1/clients/23");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("M", detail.GetProperty("gender").GetString());
        Assert.Equal("Português", detail.GetProperty("nationality").GetString());
        Assert.Equal(JsonValueKind.String, detail.GetProperty("gender").ValueKind);
        Assert.Equal(JsonValueKind.String, detail.GetProperty("nationality").ValueKind);
        repository.VerifyAll();
    }
    [Theory]
    [InlineData("M", "Português", "m", "PORTUGUÊS")]
    [InlineData("F", "Française", "f", "française")]
    [InlineData("Masc", "Portugal", "masc", "portugal")]
    [InlineData("Masculino", "Brasileira", "masculino", "brasileira")]
    [InlineData(" M ", " Português ", "M", "Português")]
    [InlineData("unknown-legacy", "Not-a-country", "UNKNOWN-LEGACY", "not-a-country")]
    [InlineData("", " ", " ", "")]
    public async Task HttpPut_MaterializedLegacyRespectsExactOmittedNullAndCanonicalValues(
        string legacyGender, string legacyNationality, string changedLegacyGender, string changedLegacyNationality)
    {
        // O JSON é construído como payload real: não serializa um DTO C# já desserializado.
        // Todas as dependências e escritas ficam num banco EF InMemory exclusivo deste caso.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Services.AddHttpContextAccessor();
        VianaHub.Global.Gerit.Infra.IoC.DependencyInjection.AddGeritInfrastructure(builder.Services);
        builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserApiService>();
        var databaseName = "client-put-legacy-" + Guid.NewGuid();
        builder.Services.AddDbContext<GeritDbContext>(options => options.UseInMemoryDatabase(databaseName));
        VianaHub.Global.Gerit.Api.Configuration.RouteValidatorSetup.AddRouteValidatorSetup(builder.Services);
        // Este teste não inicia sincronização de jobs ou qualquer hosted service externo.
        foreach (var hosted in builder.Services.Where(service => service.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToArray())
            builder.Services.Remove(hosted);
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GeritDbContext>();
            var party = new PartyTypeEntity("INDIVIDUAL", 1);
            typeof(PartyTypeEntity).GetProperties()
                .Single(property => property.Name == "Id" && property.PropertyType == typeof(byte)).SetValue(party, (byte)1);
            var source = new AcquisitionSourceTypeEntity("DIRECT", 1);
            var statusDomain = new StatusDomainEntity("CLIENT", 1);
            var status = new StatusDefinitionEntity(1, 1, "ACTIVE", 1, true, 1);
            var tenant = new VianaHub.Global.Gerit.Domain.Entities.Billing.TenantEntity(1, 1, "InMemory tenant", "tenant@example.test", null, null, null, 1);
            foreach (var entity in new VianaHub.Global.Gerit.Domain.Base.Entity[] { source, statusDomain, status, tenant })
                typeof(VianaHub.Global.Gerit.Domain.Base.Entity).GetProperty("Id")!.SetValue(entity, 1);
            db.AddRange(party, source, statusDomain, status, tenant,
                ClientControlledVocabularyTests.Entity(legacyGender, legacyNationality));
            Assert.True(db.Database.IsInMemory());
            await db.SaveChangesAsync();
        }
        app.UseMiddleware<VianaHub.Global.Gerit.Api.Middleware.RequestLocalizationMiddleware>();
        app.UseMiddleware<VianaHub.Global.Gerit.Api.Middleware.GlobalExceptionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapClientEndpoints();
        await app.StartAsync();
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-Tenant", "1");
        client.DefaultRequestHeaders.Add("X-Test-User", "1");
        client.DefaultRequestHeaders.Add("X-Test-Permission-Resource", "Clients");
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "GetBy,Update");

        Dictionary<string, object?> Payload(string name) => new()
        {
            ["partyTypeId"] = 1,
            ["acquisitionSourceTypeId"] = 1,
            ["statusDomainId"] = 1,
            ["statusDefinitionId"] = 1,
            ["name"] = name
        };
        async Task PutAsync(Dictionary<string, object?> payload, HttpStatusCode expected)
        {
            using var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
            using var response = await client.PutAsync("/v1/clients/23", content);
            Assert.True(response.StatusCode == expected, await response.Content.ReadAsStringAsync());
        }
        async Task<Dictionary<string, object?>> SnapshotAsync()
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<GeritDbContext>();
            Assert.True(db.Database.IsInMemory());
            var entity = await db.Set<ClientEntity>().AsNoTracking().SingleAsync(value => value.Id == 23 && value.TenantId == 1);
            return db.Entry(entity).Properties.ToDictionary(property => property.Metadata.Name, property => property.CurrentValue);
        }
        async Task AssertStoredAsync(string? gender, string? nationality, string name)
        {
            var snapshot = await SnapshotAsync();
            Assert.Equal(gender, snapshot["Gender"]);
            Assert.Equal(nationality, snapshot["Nationality"]);
            Assert.Equal(name, snapshot["Name"]);
            using var response = await client.GetAsync("/v1/clients/23");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var detail = await response.Content.ReadFromJsonAsync<ClientDetailResponse>();
            Assert.Equal(gender, detail!.Gender);
            Assert.Equal(nationality, detail.Nationality);
        }

        await AssertStoredAsync(legacyGender, legacyNationality, "Original");
        var exact = Payload("Exact legacy");
        exact["gender"] = legacyGender;
        exact["nationality"] = legacyNationality;
        await PutAsync(exact, HttpStatusCode.OK);
        await AssertStoredAsync(legacyGender, legacyNationality, "Exact legacy");

        var omitted = Payload("Omitted legacy");
        omitted["genderProvided"] = true;
        omitted["nationalityProvided"] = true;
        await PutAsync(omitted, HttpStatusCode.OK);
        await AssertStoredAsync(legacyGender, legacyNationality, "Omitted legacy");
        var beforeInvalid = await SnapshotAsync();
        foreach (var (gender, nationality) in new[]
                 { (changedLegacyGender, legacyNationality), (legacyGender, changedLegacyNationality) })
        {
            var invalid = Payload("Must not persist");
            invalid["gender"] = gender;
            invalid["nationality"] = nationality;
            await PutAsync(invalid, HttpStatusCode.BadRequest);
            var afterInvalid = await SnapshotAsync();
            Assert.Equal(beforeInvalid.OrderBy(value => value.Key, StringComparer.Ordinal), afterInvalid.OrderBy(value => value.Key, StringComparer.Ordinal));
        }
        var clearGender = Payload("Gender cleared");
        clearGender["gender"] = null;
        clearGender["genderProvided"] = false;
        clearGender["nationalityProvided"] = true;
        await PutAsync(clearGender, HttpStatusCode.OK);
        await AssertStoredAsync(null, legacyNationality, "Gender cleared");
        var clearNationality = Payload("Nationality cleared");
        clearNationality["nationality"] = null;
        clearNationality["nationalityProvided"] = false;
        await PutAsync(clearNationality, HttpStatusCode.OK);
        await AssertStoredAsync(null, null, "Nationality cleared");
        var canonical = Payload("Canonical values");
        canonical["gender"] = "female";
        canonical["nationality"] = "BR";
        await PutAsync(canonical, HttpStatusCode.OK);
        await AssertStoredAsync("female", "BR", "Canonical values");
    }
    private static Dictionary<string, object?> Request(ClientAggregateHttpFixture fixture, string? gender, string? nationality) => new()
    {
        ["partyTypeId"] = fixture.PartyTypeId,
        ["acquisitionSourceTypeId"] = fixture.AcquisitionSourceTypeId,
        ["statusDomainId"] = fixture.StatusDomainId,
        ["statusDefinitionId"] = fixture.StatusDefinitionId,
        ["name"] = "Vocabulary client",
        ["email"] = "vocabulary@example.test",
        ["gender"] = gender,
        ["nationality"] = nationality
    };
}
