using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EquipmentType;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class EquipmentTypeMapper
{
    public static partial EquipmentTypeResponse ToResponse(EquipmentTypeEntity source);
    public static partial EquipmentTypeDetailResponse ToDetail(EquipmentTypeEntity source);
    public static IEnumerable<EquipmentTypeResponse> ToResponses(IEnumerable<EquipmentTypeEntity> source) => source.Select(ToResponse);
    public static ListPageResponse<EquipmentTypeResponse> ToPaged(ListPage<EquipmentTypeEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);
}
