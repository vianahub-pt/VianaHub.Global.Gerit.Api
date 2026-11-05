using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Identity;

public interface IRefreshTokenDataRepository
{
    Task AddAsync(RefreshTokensEntity entity);
    Task<RefreshTokensEntity?> GetByTokenAsync(string? token, int tenantId);
    Task<IEnumerable<RefreshTokensEntity>> GetByUserAsync(int userId, int tenantId);
    Task RevokeAsync(string? token, int revokedBy, int tenantId);
}
