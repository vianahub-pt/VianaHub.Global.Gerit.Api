using System.Globalization;
using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Client;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class ClientMapper
{
    [MapperIgnoreTarget(nameof(ClientResponse.PartyTypeName))]
    public static partial ClientResponse ToResponse(ClientEntity source);

    [MapProperty(nameof(ClientEntity.ImageUrl), nameof(ClientDetailResponse.UrlImage))]
    [MapperIgnoreTarget(nameof(ClientDetailResponse.Tenant))]
    [MapperIgnoreTarget(nameof(ClientDetailResponse.PartyType))]
    [MapperIgnoreTarget(nameof(ClientDetailResponse.AcquisitionSourceType))]
    [MapperIgnoreTarget(nameof(ClientDetailResponse.StatusDefinitionName))]
    [MapperIgnoreTarget(nameof(ClientDetailResponse.StatusDomainName))]
    public static partial ClientDetailResponse ToDetail(ClientEntity source);

    public static ClientResponse ToLocalizedResponse(ClientEntity source)
    {
        var result = ToResponse(source);
        result.PartyTypeName = TranslationResolver.Resolve(source.PartyType?.Translations, CultureInfo.CurrentCulture.Name, x => x.LanguageCode, x => x.Name);
        return result;
    }

    public static ClientDetailResponse ToLocalizedDetail(ClientEntity source)
    {
        var result = ToDetail(source);
        result.Tenant = source.Tenant?.Name;
        result.PartyType = TranslationResolver.Resolve(source.PartyType?.Translations, CultureInfo.CurrentCulture.Name, x => x.LanguageCode, x => x.Name);
        result.AcquisitionSourceType = TranslationResolver.Resolve(source.AcquisitionSourceType?.Translations, CultureInfo.CurrentCulture.Name, x => x.LanguageCode, x => x.Name);
        result.StatusDefinitionName = TranslationResolver.Resolve(source.StatusDefinition?.Translations, CultureInfo.CurrentCulture.Name, x => x.LanguageCode, x => x.Name);
        result.StatusDomainName = TranslationResolver.Resolve(source.StatusDefinition?.StatusDomain?.Translations, CultureInfo.CurrentCulture.Name, x => x.LanguageCode, x => x.Name);
        return result;
    }

    public static IEnumerable<ClientResponse> ToResponses(IEnumerable<ClientEntity> source) => source.Select(ToLocalizedResponse);

    public static ListPageResponse<ClientResponse> ToPaged(ListPage<ClientEntity> source) =>
        new(ToResponses(source.Items).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);
}
