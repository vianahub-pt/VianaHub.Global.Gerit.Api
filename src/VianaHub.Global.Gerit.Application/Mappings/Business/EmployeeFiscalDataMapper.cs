using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeFiscalData;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class EmployeeFiscalDataMapper
{
    public static partial EmployeeFiscalDataResponse ToResponse(EmployeeFiscalDataEntity source);

    public static EmployeeFiscalDataDetailResponse ToDetail(EmployeeFiscalDataEntity source)
    {
        var response = MapDetail(source);
        response.Employee = source.Employee == null ? null : source.Employee.Name;
        return response;
    }

    public static IEnumerable<EmployeeFiscalDataResponse> ToResponses(IEnumerable<EmployeeFiscalDataEntity> source) => source.Select(ToResponse);
    public static ListPageResponse<EmployeeFiscalDataResponse> ToPaged(ListPage<EmployeeFiscalDataEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(EmployeeFiscalDataDetailResponse.Employee))]
    private static partial EmployeeFiscalDataDetailResponse MapDetail(EmployeeFiscalDataEntity source);
}
