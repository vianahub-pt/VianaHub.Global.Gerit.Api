using AutoMapper;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeAddress;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

public class EmployeeAddressesMappingProfile : Profile
{
    public EmployeeAddressesMappingProfile()
    {
        CreateMap<EmployeeAddressesEntity, EmployeeAddressResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.AddressTypeName, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.AddressType?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)))
            .ForMember(dest => dest.CountryCode, opt => opt.MapFrom(src => src.CountryCode))
            .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Street))
            .ForMember(dest => dest.IsPrimary, opt => opt.MapFrom(src => src.IsPrimary))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

        CreateMap<EmployeeAddressesEntity, EmployeeAddressDetailResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
            .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.EmployeeId))
            .ForMember(dest => dest.Employee, opt => opt.MapFrom(src => src.Employee == null ? null : src.Employee.Name))
            .ForMember(dest => dest.AddressTypeId, opt => opt.MapFrom(src => src.AddressTypeId))
            .ForMember(dest => dest.AddressType, opt => opt.MapFrom((src, _, _, _) =>
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

        CreateMap<ListPage<EmployeeAddressesEntity>, ListPageResponse<EmployeeAddressResponse>>();
    }
}
