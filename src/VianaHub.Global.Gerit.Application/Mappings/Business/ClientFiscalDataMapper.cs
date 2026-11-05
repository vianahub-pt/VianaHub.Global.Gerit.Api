using System.Globalization;
using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientFiscalData;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Services;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper]
public static partial class ClientFiscalDataMapper
{
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.TenantId))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.ClientId))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.VatNumber))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.FiscalCountry))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.IsVatRegistered))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.IBAN))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.FiscalEmail))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.IsDeleted))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.Client))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.Tenant))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.CreatedBy))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.CreatedAt))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.ModifiedBy))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.ModifiedAt))]
    public static partial ClientFiscalDataResponse ToResponse(ClientFiscalDataEntity source);

    [MapProperty("Client.Name", nameof(ClientFiscalDataDetailResponse.Client))]
    [MapProperty("Client.PartyTypeId", nameof(ClientFiscalDataDetailResponse.ClientTypeId))]
    [MapProperty("Client.PartyType", nameof(ClientFiscalDataDetailResponse.ClientType), Use = nameof(MapPartyType))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.IsDeleted))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.Tenant))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.CreatedBy))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.ModifiedBy))]
    [MapperIgnoreSource(nameof(ClientFiscalDataEntity.ModifiedAt))]
    public static partial ClientFiscalDataDetailResponse ToDetail(ClientFiscalDataEntity source);

    public static ListPageResponse<ClientFiscalDataResponse> ToPaged(ListPage<ClientFiscalDataEntity> source)
    {
        return new ListPageResponse<ClientFiscalDataResponse>(
            source.Items.Select(ToResponse).ToList(),
            source.PageNumber,
            source.PageSize,
            source.TotalItems,
            source.TotalPages);
    }

    [UserMapping]
    private static string? MapPartyType(PartyTypeEntity? source)
    {
        return source?.Translations == null
            ? null
            : TranslationResolver.Resolve(source.Translations, CultureInfo.CurrentCulture.Name, x => x.LanguageCode, x => x.Name);
    }
}
