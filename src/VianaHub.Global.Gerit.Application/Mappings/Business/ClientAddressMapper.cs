using System.Globalization;
using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientAddress;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper]
public static partial class ClientAddressMapper
{
    [MapperIgnoreSource(nameof(ClientAddressesEntity.TenantId))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.ClientId))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.AddressTypeId))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.StreetNumber))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.Complement))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.Neighborhood))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.City))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.District))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.PostalCode))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.Latitude))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.Longitude))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.Note))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.IsDeleted))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.Client))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.AddressType))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.CreatedBy))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.CreatedAt))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.ModifiedBy))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.ModifiedAt))]
    [MapperIgnoreTarget(nameof(ClientAddressResponse.AddressTypeName))]
    public static partial ClientAddressResponse ToResponse(ClientAddressesEntity source);

    [MapperIgnoreSource(nameof(ClientAddressesEntity.IsDeleted))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.Client))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.AddressType))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.CreatedBy))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.ModifiedBy))]
    [MapperIgnoreSource(nameof(ClientAddressesEntity.ModifiedAt))]
    [MapperIgnoreTarget(nameof(ClientAddressDetailResponse.TenantName))]
    [MapperIgnoreTarget(nameof(ClientAddressDetailResponse.ClientName))]
    [MapperIgnoreTarget(nameof(ClientAddressDetailResponse.AddressTypeName))]
    public static partial ClientAddressDetailResponse ToDetail(ClientAddressesEntity source);

    public static ClientAddressResponse ToLocalizedResponse(ClientAddressesEntity source)
    {
        var result = ToResponse(source);
        result.AddressTypeName = TranslationResolver.Resolve(source.AddressType?.Translations, CultureInfo.CurrentCulture.Name, x => x.LanguageCode, x => x.Name);
        return result;
    }

    public static ClientAddressDetailResponse ToLocalizedDetail(ClientAddressesEntity source)
    {
        var result = ToDetail(source);
        result.TenantName = source.Client?.Tenant?.Name;
        result.ClientName = source.Client?.Name;
        result.AddressTypeName = TranslationResolver.Resolve(source.AddressType?.Translations, CultureInfo.CurrentCulture.Name, x => x.LanguageCode, x => x.Name);
        return result;
    }

    public static IEnumerable<ClientAddressResponse> ToResponses(IEnumerable<ClientAddressesEntity> source) => source.Select(ToLocalizedResponse);

    public static ListPageResponse<ClientAddressResponse> ToPaged(ListPage<ClientAddressesEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);
}
