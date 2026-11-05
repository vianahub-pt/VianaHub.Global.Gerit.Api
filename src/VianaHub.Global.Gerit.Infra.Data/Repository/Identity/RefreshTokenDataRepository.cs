using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Repository.Identity;

public class RefreshTokenDataRepository : IRefreshTokenDataRepository
{
    private readonly GeritDbContext _context;

    public RefreshTokenDataRepository(GeritDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefreshTokensEntity entity)
    {
        await _context.Set<RefreshTokensEntity>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<RefreshTokensEntity?> GetByTokenAsync(string? token, int tenantId)
    {
        return await _context.Set<RefreshTokensEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenHash == token && x.TenantId == tenantId);
    }

    public async Task<IEnumerable<RefreshTokensEntity>> GetByUserAsync(int userId, int tenantId)
    {
        return await _context.Set<RefreshTokensEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task RevokeAsync(string? token, int revokedBy, int tenantId)
    {
        var entity = await _context.Set<RefreshTokensEntity>()
            .FirstOrDefaultAsync(x => x.TokenHash == token && x.TenantId == tenantId);

        if (entity == null)
            return;

        entity.Revoke(revokedBy);
        _context.Set<RefreshTokensEntity>().Update(entity);
        await _context.SaveChangesAsync();
    }
}
