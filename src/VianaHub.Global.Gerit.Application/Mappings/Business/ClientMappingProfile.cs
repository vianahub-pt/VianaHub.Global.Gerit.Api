using AutoMapper;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Client;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

public class ClientMappingProfile : Profile
{
    public ClientMappingProfile()
    {
        CreateMap<ClientEntity, ClientResponse>()
            .ForMember(dest => dest.PartyTypeName, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.PartyType?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)));

        CreateMap<ClientEntity, ClientDetailResponse>()
            .ForMember(dest => dest.UrlImage, opt => opt.MapFrom(src => src.ImageUrl))
            .ForMember(dest => dest.Tenant, opt => opt.MapFrom(src => ResolveTenant(src)))
            .ForMember(dest => dest.PartyType, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.PartyType?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)))
            .ForMember(dest => dest.AcquisitionSourceType, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.AcquisitionSourceType?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)))
            .ForMember(dest => dest.StatusDefinitionName, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.StatusDefinition?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)))
            .ForMember(dest => dest.StatusDomainName, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.StatusDomain?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)));

        CreateMap<ListPage<ClientEntity>, ListPageResponse<ClientResponse>>();
    }

    private static string? ResolvePrimaryContact(ClientEntity src)
    {
        return src.Contacts
            .FirstOrDefault(x => x.IsPrimary && !x.IsDeleted)?.Name
            ?? src.Contacts.FirstOrDefault(x => !x.IsDeleted)?.Name;
    }

    private static string? ResolveTenant(ClientEntity src)
    {
        return src.Tenant?.Name;
    }
}
