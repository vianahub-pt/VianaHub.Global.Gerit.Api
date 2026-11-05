using System.Globalization;
using VianaHub.Global.Gerit.Application.Mappings.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Tests.ClientAddresses;

public sealed class ClientAddressMapperTests
{
    [Theory]
    [InlineData("pt-PT", "Rua")]
    [InlineData("pt-BR", "Rua")]
    [InlineData("en-US", "Street")]
    [InlineData("es-ES", "Calle")]
    public void DetailMapsStructuralFieldsAndLocalizedAddressType(string culture, string expected)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var type = new AddressTypeEntity("HOME", 1);
            type.AddTranslation(new AddressTypeTranslationsEntity(1, "pt-PT", "Rua", null));
            type.AddTranslation(new AddressTypeTranslationsEntity(1, "pt-BR", "Rua", null));
            type.AddTranslation(new AddressTypeTranslationsEntity(1, "en-US", "Street", null));
            type.AddTranslation(new AddressTypeTranslationsEntity(1, "es-ES", "Calle", null));
            var entity = new ClientAddressesEntity(1, 10, 1, "PT", "Main", null, null, "Centre", "Lisbon", null, "1000", null, null, null, false, 1);
            typeof(ClientAddressesEntity).GetProperty(nameof(ClientAddressesEntity.AddressType))!.SetValue(entity, type);

            var result = ClientAddressMapper.ToLocalizedDetail(entity);

            Assert.Equal("PT", result.CountryCode);
            Assert.Equal(expected, result.AddressTypeName);
            Assert.Equal(10, result.ClientId);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}
