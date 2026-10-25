using AutoMapper;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientContact;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

/// <summary>
/// Profile de mapeamento para ClientContact
/// </summary>
public class ClientContactPersonsMappingProfile : Profile
{
    public ClientContactPersonsMappingProfile()
    {
        CreateMap<ClientContactPersonsEntity, ClientContactPersonResponse>();

        CreateMap<ClientContactPersonsEntity, ClientContactPersonDetailResponse>()
            .ForMember(dest => dest.Client, opt => opt.MapFrom(src => src.Client == null ? null : src.Client.Name));

        CreateMap<ListPage<ClientContactPersonsEntity>, ListPageResponse<ClientContactPersonResponse>>();
    }
}
