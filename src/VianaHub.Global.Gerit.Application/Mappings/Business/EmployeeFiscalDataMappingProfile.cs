using AutoMapper;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeFiscalData;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

public class EmployeeFiscalDataMappingProfile : Profile
{
    public EmployeeFiscalDataMappingProfile()
    {
        CreateMap<EmployeeFiscalDataEntity, EmployeeFiscalDataResponse>();

        CreateMap<EmployeeFiscalDataEntity, EmployeeFiscalDataDetailResponse>()
            .ForMember(dest => dest.Employee, opt => opt.MapFrom(src => src.Employee == null ? null : src.Employee.Name));

        CreateMap<ListPage<EmployeeFiscalDataEntity>, ListPageResponse<EmployeeFiscalDataResponse>>();
    }
}
