using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientContact;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

[Mapper]
public static partial class ClientContactPersonMapper
{
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.TenantId))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.ClientId))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.PhoneNumber))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.JobTitle))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.Department))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.IsCellPhoneWhatsapp))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.Email))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.IsWhatsapp))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.IsDeleted))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.Client))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.CreatedBy))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.CreatedAt))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.ModifiedBy))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.ModifiedAt))]
    public static partial ClientContactPersonResponse ToResponse(ClientContactPersonsEntity source);

    [MapProperty("Client.Name", nameof(ClientContactPersonDetailResponse.Client))]
    [MapProperty(nameof(ClientContactPersonsEntity.IsCellPhoneWhatsapp), nameof(ClientContactPersonDetailResponse.IsWhatsapp))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.IsWhatsapp))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.IsDeleted))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.CreatedBy))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.ModifiedBy))]
    [MapperIgnoreSource(nameof(ClientContactPersonsEntity.ModifiedAt))]
    public static partial ClientContactPersonDetailResponse ToDetail(ClientContactPersonsEntity source);

    public static ListPageResponse<ClientContactPersonResponse> ToPaged(ListPage<ClientContactPersonsEntity> source) =>
        new(source.Items.Select(ToResponse).ToList(), source.PageNumber, source.PageSize, source.TotalItems, source.TotalPages);
}
