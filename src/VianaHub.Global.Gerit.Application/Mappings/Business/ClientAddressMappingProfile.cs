using AutoMapper;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientAddress;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

/// <summary>
/// Perfil de mapeamento para ClientAddress
/// </summary>
public class ClientAddressesMappingProfile : Profile
{
    public ClientAddressesMappingProfile()
    {
        CreateMap<ClientAddressesEntity, ClientAddressResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.AddressTypeName, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.AddressType?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)))
            .ForMember(dest => dest.CountryCode, opt => opt.MapFrom(src => src.CountryCode))
            .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Street))
            .ForMember(dest => dest.IsPrimary, opt => opt.MapFrom(src => src.IsPrimary))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

        CreateMap<ClientAddressesEntity, ClientAddressDetailResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
            .ForMember(dest => dest.ClientId, opt => opt.MapFrom(src => src.ClientId))
            .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => src.Client == null ? null : src.Client.Name))
            .ForMember(dest => dest.AddressTypeId, opt => opt.MapFrom(src => src.AddressTypeId))
            .ForMember(dest => dest.AddressTypeName, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.AddressType?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)))
            .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Street))
            .ForMember(dest => dest.StreetNumber, opt => opt.MapFrom(src => src.StreetNumber))
            .ForMember(dest => dest.Complement, opt => opt.MapFrom(src => src.Complement))
            .ForMember(dest => dest.Neighborhood, opt => opt.MapFrom(src => src.Neighborhood))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City))
            .ForMember(dest => dest.District, opt => opt.MapFrom(src => src.District))
            .ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.PostalCode))
            .ForMember(dest => dest.CountryCode, opt => opt.MapFrom(src => src.CountryCode))
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Latitude))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Longitude))
            .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note))
            .ForMember(dest => dest.IsPrimary, opt => opt.MapFrom(src => src.IsPrimary))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<ListPage<ClientAddressesEntity>, ListPageResponse<ClientAddressResponse>>();
    }
}
