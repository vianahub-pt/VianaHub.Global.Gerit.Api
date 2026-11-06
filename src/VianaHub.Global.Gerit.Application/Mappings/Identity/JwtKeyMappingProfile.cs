using AutoMapper;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Jwt;
using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Application.Mappings.Identity;

public sealed class JwtKeyMappingProfile : Profile
{
    public JwtKeyMappingProfile() => CreateMap<JwtKeysEntity, JwtKeyResponse>();
}
