using AutoMapper;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Action;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Jwt;
using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Application.Mappings.Identity;

/// <summary>
/// Perfil de mapeamento do AutoMapper para ActionEntity
/// </summary>
public class ActionMappingProfile : Profile
{
    public ActionMappingProfile()
    {
        CreateMap<ActionEntity, ActionResponse>();
        CreateMap<ActionEntity, ActionDetailResponse>();
        CreateMap<ListPage<ActionEntity>, ListPageResponse<ActionResponse>>();
        CreateMap<JwtKeysEntity, JwtKeyResponse>();
    }
}
