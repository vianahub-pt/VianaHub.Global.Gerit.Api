using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.TenantContactPerson;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Billing;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class TenantContactPersonMapper
{
    public static partial TenantContactPersonResponse ToResponse(TenantContactPersonsEntity source);
    public static partial TenantContactPersonDetailResponse ToDetail(TenantContactPersonsEntity source);
    public static IEnumerable<TenantContactPersonResponse> ToResponses(IEnumerable<TenantContactPersonsEntity> source) => source.Select(ToResponse);
    public static ListPageResponse<TenantContactPersonResponse> ToPaged(ListPage<TenantContactPersonsEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);
}
