using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeAddress;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class EmployeeAddressMapper
{
    public static EmployeeAddressResponse ToResponse(EmployeeAddressesEntity source, string culture)
    {
        var response = MapResponse(source);
        response.AddressTypeName = TranslationResolver.Resolve(source.AddressType?.Translations, culture, t => t.LanguageCode, t => t.Name);
        return response;
    }

    public static EmployeeAddressDetailResponse ToDetail(EmployeeAddressesEntity source, string culture)
    {
        var response = MapDetail(source);
        response.Employee = source.Employee == null ? null : source.Employee.Name;
        response.AddressType = TranslationResolver.Resolve(source.AddressType?.Translations, culture, t => t.LanguageCode, t => t.Name);
        return response;
    }

    public static IEnumerable<EmployeeAddressResponse> ToResponses(IEnumerable<EmployeeAddressesEntity> source, string culture) =>
        source.Select(entity => ToResponse(entity, culture));

    public static ListPageResponse<EmployeeAddressResponse> ToPaged(ListPage<EmployeeAddressesEntity> source, string culture) =>
        new(ToResponses(source.Items, culture).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(EmployeeAddressResponse.AddressTypeName))]
    private static partial EmployeeAddressResponse MapResponse(EmployeeAddressesEntity source);

    [MapperIgnoreTarget(nameof(EmployeeAddressDetailResponse.Employee))]
    [MapperIgnoreTarget(nameof(EmployeeAddressDetailResponse.AddressType))]
    private static partial EmployeeAddressDetailResponse MapDetail(EmployeeAddressesEntity source);
}
