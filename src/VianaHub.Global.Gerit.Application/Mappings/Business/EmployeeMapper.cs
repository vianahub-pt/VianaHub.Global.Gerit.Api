using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Employee;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class EmployeeMapper
{
    public static partial EmployeeResponse ToResponse(EmployeeEntity source);

    public static EmployeeDetailResponse ToDetail(EmployeeEntity source, string culture)
    {
        var response = MapDetail(source);
        response.StatusDefinitionName = TranslationResolver.Resolve(source.StatusDefinition?.Translations, culture, t => t.LanguageCode, t => t.Name);
        response.StatusDomainName = TranslationResolver.Resolve(source.StatusDefinition?.StatusDomain?.Translations, culture, t => t.LanguageCode, t => t.Name);
        return response;
    }

    public static IEnumerable<EmployeeResponse> ToResponses(IEnumerable<EmployeeEntity> source) => source.Select(ToResponse);
    public static ListPageResponse<EmployeeResponse> ToPaged(ListPage<EmployeeEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(EmployeeDetailResponse.StatusDefinitionName))]
    [MapperIgnoreTarget(nameof(EmployeeDetailResponse.StatusDomainName))]
    private static partial EmployeeDetailResponse MapDetail(EmployeeEntity source);
}
