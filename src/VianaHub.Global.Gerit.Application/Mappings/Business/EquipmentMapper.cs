using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Equipment;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class EquipmentMapper
{
    public static EquipmentResponse ToResponse(EquipmentEntity source)
    {
        var response = MapResponse(source);
        response.EquipmentType = source.EquipmentType?.Name ?? string.Empty;
        return response;
    }

    public static EquipmentDetailResponse ToDetail(EquipmentEntity source, string culture)
    {
        var response = MapDetail(source);
        response.EquipmentType = source.EquipmentType?.Name ?? string.Empty;
        response.StatusDefinition = TranslationResolver.Resolve(source.StatusDefinition?.Translations, culture, t => t.LanguageCode, t => t.Name);
        response.StatusDefinitionName = response.StatusDefinition;
        response.StatusDomainName = TranslationResolver.Resolve(source.StatusDefinition?.StatusDomain?.Translations, culture, t => t.LanguageCode, t => t.Name);
        return response;
    }

    public static IEnumerable<EquipmentResponse> ToResponses(IEnumerable<EquipmentEntity> source) => source.Select(ToResponse);
    public static ListPageResponse<EquipmentResponse> ToPaged(ListPage<EquipmentEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(EquipmentResponse.EquipmentType))]
    private static partial EquipmentResponse MapResponse(EquipmentEntity source);

    [MapperIgnoreTarget(nameof(EquipmentDetailResponse.EquipmentType))]
    [MapperIgnoreTarget(nameof(EquipmentDetailResponse.StatusDefinition))]
    [MapperIgnoreTarget(nameof(EquipmentDetailResponse.StatusDefinitionName))]
    [MapperIgnoreTarget(nameof(EquipmentDetailResponse.StatusDomainName))]
    private static partial EquipmentDetailResponse MapDetail(EquipmentEntity source);
}
