using AutoMapper;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeContact;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

public class EmployeeContactPersonsMappingProfile : Profile
{
    public EmployeeContactPersonsMappingProfile()
    {
        CreateMap<EmployeeContactPersonsEntity, EmployeeContactPersonResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.CellPhoneNumber, opt => opt.MapFrom(src => src.CellPhoneNumber))
            .ForMember(dest => dest.IsPrimary, opt => opt.MapFrom(src => src.IsPrimary))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

        CreateMap<EmployeeContactPersonsEntity, EmployeeContactPersonDetailResponse>();
        
        CreateMap<ListPage<EmployeeContactPersonsEntity>, ListPageResponse<EmployeeContactPersonResponse>>();
    }
}
