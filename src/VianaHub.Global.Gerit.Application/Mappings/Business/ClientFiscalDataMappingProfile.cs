using AutoMapper;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientFiscalData;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

public class ClientFiscalDataMappingProfile : Profile
{
    public ClientFiscalDataMappingProfile()
    {
        CreateMap<ClientFiscalDataEntity, ClientFiscalDataResponse>();

        CreateMap<ClientFiscalDataEntity, ClientFiscalDataDetailResponse>()
            .ForMember(dest => dest.ClientTypeId, opt => opt.MapFrom(src => (int)src.Client.PartyTypeId))
            .ForMember(dest => dest.ClientType, opt => opt.MapFrom(src => src.Client.PartyTypeId.ToString()))
            .ForMember(dest => dest.Client, opt => opt.MapFrom(src => src.Client == null ? null : src.Client.Name));

        CreateMap<ListPage<ClientFiscalDataEntity>, ListPageResponse<ClientFiscalDataResponse>>();
    }
}
