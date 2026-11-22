using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.TenantAddress;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Billing;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class TenantAddressMapper
{
    public static TenantAddressResponse ToResponse(TenantAddressesEntity source, string culture)
    {
        var response = MapResponse(source);
        response.AddressTypeName = TranslationResolver.Resolve(source.AddressType?.Translations, culture, t => t.LanguageCode, t => t.Name);
        return response;
    }

    public static TenantAddressDetailResponse ToDetail(TenantAddressesEntity source, string culture)
    {
        var response = MapDetail(source);
        response.AddressTypeName = TranslationResolver.Resolve(source.AddressType?.Translations, culture, t => t.LanguageCode, t => t.Name);
        return response;
    }

    public static IEnumerable<TenantAddressResponse> ToResponses(IEnumerable<TenantAddressesEntity> source, string culture) =>
        source.Select(entity => ToResponse(entity, culture));

    public static ListPageResponse<TenantAddressResponse> ToPaged(ListPage<TenantAddressesEntity> source, string culture) =>
        new(ToResponses(source.Items, culture).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(TenantAddressResponse.AddressTypeName))]
    private static partial TenantAddressResponse MapResponse(TenantAddressesEntity source);

    [MapperIgnoreTarget(nameof(TenantAddressDetailResponse.AddressTypeName))]
    private static partial TenantAddressDetailResponse MapDetail(TenantAddressesEntity source);
}
