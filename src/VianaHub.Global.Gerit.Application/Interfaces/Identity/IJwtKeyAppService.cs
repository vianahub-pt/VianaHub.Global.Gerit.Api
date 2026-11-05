using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Jwt;

namespace VianaHub.Global.Gerit.Application.Interfaces.Identity;

public interface IJwtKeyAppService
{
    Task<IEnumerable<JwtKeyResponse>> GetByTenantAsync(int tenantId, CancellationToken ct);
    Task<JwtKeyResponse?> GetActiveKeyAsync(int tenantId, CancellationToken ct);
    Task<bool> CreateInitialIfNotExistsAsync(int tenantId, CancellationToken ct);
    Task<bool> RevokeAsync(int id, string? reason, CancellationToken ct);
}
