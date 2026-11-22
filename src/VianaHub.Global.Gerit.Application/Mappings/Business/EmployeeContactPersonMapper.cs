using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeContact;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class EmployeeContactPersonMapper
{
    public static partial EmployeeContactPersonResponse ToResponse(EmployeeContactPersonsEntity source);
    public static partial EmployeeContactPersonDetailResponse ToDetail(EmployeeContactPersonsEntity source);
    public static IEnumerable<EmployeeContactPersonResponse> ToResponses(IEnumerable<EmployeeContactPersonsEntity> source) => source.Select(ToResponse);
    public static ListPageResponse<EmployeeContactPersonResponse> ToPaged(ListPage<EmployeeContactPersonsEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);
}
