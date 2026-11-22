using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.Tenant;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Billing;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class TenantMapper
{
    public static partial TenantLoginResponse ToLoginResponse(TenantEntity source);
    public static IEnumerable<TenantLoginResponse> ToLoginResponses(IEnumerable<TenantEntity> source) => source.Select(ToLoginResponse);

    public static TenantResponse ToResponse(TenantEntity source) => MapResponse(source);
    public static TenantDetailResponse? ToDetail(TenantEntity? source) => source is null ? null : MapDetail(source);
    public static IEnumerable<TenantResponse> ToResponses(IEnumerable<TenantEntity> source) => source.Select(ToResponse);
    public static ListPageResponse<TenantResponse> ToPaged(ListPage<TenantEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(TenantResponse.AcquisitionSourceTypeName))]
    private static partial TenantResponse MapResponse(TenantEntity source);

    [MapperIgnoreTarget(nameof(TenantDetailResponse.AcquisitionSourceTypeName))]
    private static partial TenantDetailResponse MapDetail(TenantEntity source);
}
