using AutoMapper;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.Tenant;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Billing;

public class TenantMappingProfile : Profile
{
    public TenantMappingProfile()
    {
        CreateMap<TenantEntity, TenantResponse>();
        CreateMap<TenantEntity, TenantDetailResponse>();
        CreateMap<TenantEntity, TenantLoginResponse>();
        CreateMap<ListPage<TenantEntity>, ListPageResponse<TenantResponse>>();
    }
}
