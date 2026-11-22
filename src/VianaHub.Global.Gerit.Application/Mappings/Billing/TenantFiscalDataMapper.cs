using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.TenantFiscalData;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Billing;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class TenantFiscalDataMapper
{
    public static partial TenantFiscalDataResponse ToResponse(TenantFiscalDataEntity source);
    public static partial TenantFiscalDataDetailResponse ToDetail(TenantFiscalDataEntity source);
    public static IEnumerable<TenantFiscalDataResponse> ToResponses(IEnumerable<TenantFiscalDataEntity> source) => source.Select(ToResponse);
    public static ListPageResponse<TenantFiscalDataResponse> ToPaged(ListPage<TenantFiscalDataEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);
}
