using AutoMapper;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitAddress;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

public class VisitAddressesMappingProfile : Profile
{
    public VisitAddressesMappingProfile()
    {
        CreateMap<VisitAddressesEntity, VisitAddressResponse>()
            .ForMember(dest => dest.AddressTypeName, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.AddressType?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)));

        CreateMap<VisitAddressesEntity, VisitAddressDetailResponse>()
            .ForMember(dest => dest.Visit, opt => opt.MapFrom(src => src.Visit == null ? null : src.Visit.Title))
            .ForMember(dest => dest.AddressType, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.AddressType?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)));

        CreateMap<ListPage<VisitAddressesEntity>, ListPageResponse<VisitAddressResponse>>();
    }
}
