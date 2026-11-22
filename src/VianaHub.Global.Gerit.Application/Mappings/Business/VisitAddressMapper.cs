using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitAddress;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class VisitAddressMapper
{
    public static VisitAddressResponse ToResponse(VisitAddressesEntity source, string culture)
    {
        var response = MapResponse(source);
        response.AddressTypeName = TranslationResolver.Resolve(source.AddressType?.Translations, culture, t => t.LanguageCode, t => t.Name);
        return response;
    }

    public static VisitAddressDetailResponse ToDetail(VisitAddressesEntity source, string culture)
    {
        var response = MapDetail(source);
        response.Visit = source.Visit?.Title;
        response.AddressType = TranslationResolver.Resolve(source.AddressType?.Translations, culture, t => t.LanguageCode, t => t.Name);
        return response;
    }

    public static IEnumerable<VisitAddressResponse> ToResponses(IEnumerable<VisitAddressesEntity> source, string culture) => source.Select(e => ToResponse(e, culture));
    public static ListPageResponse<VisitAddressResponse> ToPaged(ListPage<VisitAddressesEntity> source, string culture) =>
        new(ToResponses(source.Items, culture).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(VisitAddressResponse.AddressTypeName))]
    private static partial VisitAddressResponse MapResponse(VisitAddressesEntity source);
    [MapperIgnoreTarget(nameof(VisitAddressDetailResponse.Visit))]
    [MapperIgnoreTarget(nameof(VisitAddressDetailResponse.AddressType))]
    private static partial VisitAddressDetailResponse MapDetail(VisitAddressesEntity source);
}
