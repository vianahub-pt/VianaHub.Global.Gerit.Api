using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Vehicle;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class VehicleMapper
{
    public static partial VehicleResponse ToResponse(VehicleEntity source);

    public static VehicleDetailResponse ToDetail(VehicleEntity source, string culture)
    {
        var response = MapDetail(source);
        response.StatusDefinitionName = TranslationResolver.Resolve(source.StatusDefinition?.Translations, culture, t => t.LanguageCode, t => t.Name);
        response.StatusDomainName = TranslationResolver.Resolve(source.StatusDefinition?.StatusDomain?.Translations, culture, t => t.LanguageCode, t => t.Name);
        return response;
    }

    public static IEnumerable<VehicleResponse> ToResponses(IEnumerable<VehicleEntity> source) => source.Select(ToResponse);
    public static ListPageResponse<VehicleResponse> ToPaged(ListPage<VehicleEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);

    [MapperIgnoreTarget(nameof(VehicleDetailResponse.StatusDefinitionName))]
    [MapperIgnoreTarget(nameof(VehicleDetailResponse.StatusDomainName))]
    private static partial VehicleDetailResponse MapDetail(VehicleEntity source);
}
