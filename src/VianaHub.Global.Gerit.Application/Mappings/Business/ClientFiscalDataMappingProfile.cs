using AutoMapper;
using System.Globalization;
using VianaHub.Global.Gerit.Domain.Services;
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
            .ForMember(dest => dest.ClientTypeId, opt => opt.MapFrom(src => src.Client == null ? 0 : (int)src.Client.PartyTypeId))
            .ForMember(dest => dest.ClientType, opt => opt.MapFrom<ClientFiscalDataClientTypeResolver>())
            .ForMember(dest => dest.Client, opt => opt.MapFrom(src => src.Client == null ? null : src.Client.Name));

        CreateMap<ListPage<ClientFiscalDataEntity>, ListPageResponse<ClientFiscalDataResponse>>();
    }
}

public sealed class ClientFiscalDataClientTypeResolver : IValueResolver<ClientFiscalDataEntity, ClientFiscalDataDetailResponse, string?>
{
    public string? Resolve(ClientFiscalDataEntity source, ClientFiscalDataDetailResponse destination, string? destMember, ResolutionContext context)
    {
        var translations = source.Client?.PartyType?.Translations;
        return translations == null
            ? null
            : TranslationResolver.Resolve(translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name);
    }
}
