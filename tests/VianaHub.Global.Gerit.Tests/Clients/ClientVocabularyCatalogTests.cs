using System.Text.Json;
using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Api.Services;
using VianaHub.Global.Gerit.Domain.Enums;
using VianaHub.Global.Gerit.Domain.Vocabularies;

namespace VianaHub.Global.Gerit.Tests.Clients;

public sealed class ClientVocabularyCatalogTests
{
    private static readonly string[] Europe = "AL AD AT BY BE BA BG HR CZ DK EE FI FR DE GR VA HU IS IE IT LV LI LT LU MT MD MC ME NL MK NO PL PT RO RU SM RS SK SI ES SE CH UA GB".Split(' ');
    private static readonly string[] Americas = "AG AR BS BB BZ BO BR CA CL CO CR CU DM DO EC SV GD GT GY HT HN JM MX NI PA PY PE KN LC VC SR TT US UY VE".Split(' ');

    [Fact]
    public void Nationalities_HaveExactVersionedUnM49MembershipAndRegions()
    {
        Assert.Equal("2026-11-21", ClientNationalityCatalog.Version);
        Assert.Equal(44, Europe.Length);
        Assert.Equal(35, Americas.Length);
        Assert.Equal(79, ClientNationalityCatalog.Entries.Count);
        Assert.Equal(79, ClientNationalityCatalog.Codes.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(Europe.Concat(Americas).Order(StringComparer.Ordinal), ClientNationalityCatalog.Codes.Order(StringComparer.Ordinal));
        Assert.Equal(Europe.Order(StringComparer.Ordinal), ClientNationalityCatalog.Entries.Where(country => country.Region == "Europe").Select(country => country.Code).Order(StringComparer.Ordinal));
        Assert.Equal(Americas.Order(StringComparer.Ordinal), ClientNationalityCatalog.Entries.Where(country => country.Region == "Americas").Select(country => country.Code).Order(StringComparer.Ordinal));
        foreach (var country in ClientNationalityCatalog.Entries)
        {
            Assert.Matches("^[A-Z]{2}$", country.Code);
            Assert.Equal("Client.Nationality." + country.Code, country.LocalizationKey);
            Assert.True(ClientNationalityCatalog.IsCanonical(country.Code));
            Assert.False(ClientNationalityCatalog.IsCanonical(country.Code.ToLowerInvariant()));
            Assert.False(ClientNationalityCatalog.IsCanonical(" " + country.Code));
            Assert.False(ClientNationalityCatalog.IsCanonical(country.Code + " "));
        }
        Assert.True(ClientNationalityCatalog.IsCanonical(null));
        foreach (var excluded in new[] { "", " ", "GF", "PR", "XK", "CY", "TR", "CN", "AO", "AU", "PSE", "620", "Português" })
            Assert.False(ClientNationalityCatalog.IsCanonical(excluded));
    }

    [Fact]
    public void GenderCodes_HaveExplicitEnumRoundTripsWithoutNumericWireValues()
    {
        Assert.Equal(new[] { "male", "female", "non-binary", "prefer-not-to-say" }, ClientGenderCatalog.Codes);
        foreach (var code in ClientGenderCatalog.Codes)
        {
            Assert.True(ClientGenderCatalog.TryParse(code, out var value));
            Assert.Equal(code, ClientGenderCatalog.ToCode(value));
            Assert.True(ClientGenderCatalog.IsCanonical(code));
            Assert.False(ClientGenderCatalog.TryParse(((int)value).ToString(System.Globalization.CultureInfo.InvariantCulture), out _));
        }
        Assert.True(ClientGenderCatalog.IsCanonical(null));
        Assert.False(ClientGenderCatalog.TryParse(null, out _));
        Assert.Null(ClientGenderCatalog.ToCode((ClientGender)0));
        Assert.Null(ClientGenderCatalog.ToCode((ClientGender)int.MaxValue));
        foreach (var legacy in new[] { "M", "F", "Masc", "Masculino", "Male", "MALE", " male", "male ", "", "1", "unknown" })
            Assert.False(ClientGenderCatalog.IsCanonical(legacy));
    }

    [Theory]
    [InlineData("pt-PT")]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    [InlineData("es-ES")]
    public void EveryCountryGenderAndValidationKey_HasNativeTranslationWithoutFallback(string culture)
    {
        // Lê o ficheiro específico para impedir que o fallback esconda chaves em falta.
        var file = Path.Join(AppContext.BaseDirectory, "locales", culture, "common.json");
        Assert.True(File.Exists(file), file);
        using var document = JsonDocument.Parse(File.ReadAllText(file));
        var context = new DefaultHttpContext();
        context.Items["RequestCulture"] = culture;
        var localization = new LocalizationService(new HttpContextAccessor { HttpContext = context });
        var keys = ClientNationalityCatalog.Entries.Select(country => country.LocalizationKey)
            .Concat(ClientGenderCatalog.Codes.Select(code => "Client.Gender." + code))
            .Concat(new[] { "Domain.Client.GenderControlledValue", "Domain.Client.NationalityControlledValue" });
        foreach (var key in keys)
        {
            var expected = document.RootElement.GetProperty(key).GetString();
            Assert.False(string.IsNullOrWhiteSpace(expected));
            Assert.NotEqual(key, expected);
            Assert.Equal(expected, localization.GetMessage(key));
        }
        Assert.Equal("Portugal", localization.GetMessage("Client.Nationality.PT"));
    }
}
