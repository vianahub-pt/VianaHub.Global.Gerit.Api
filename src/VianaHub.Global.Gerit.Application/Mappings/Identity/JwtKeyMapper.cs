using Riok.Mapperly.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Jwt;
using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Application.Mappings.Identity;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class JwtKeyMapper
{
    public static partial JwtKeyResponse ToResponse(JwtKeysEntity source);
    public static IEnumerable<JwtKeyResponse> ToResponses(IEnumerable<JwtKeysEntity> source) => source.Select(ToResponse);
}
