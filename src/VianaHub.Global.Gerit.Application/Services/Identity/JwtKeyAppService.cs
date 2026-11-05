using AutoMapper;
using Azure.Core;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Jwt;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Identity;

public class JwtKeyAppService : IJwtKeyAppService
{
    private readonly IJwtKeyDataRepository _repo;
    private readonly IJwtKeyDomainService _domain;
    private readonly INotify _notify;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly ILocalizationService _localization;
    private readonly IRequestTenantContext _requestTenantContext;

    public JwtKeyAppService(
        IJwtKeyDataRepository repo,
        IJwtKeyDomainService domain,
        INotify notify,
        IMapper mapper,
        ICurrentUserService currentUser,
        ILocalizationService localization,
        IRequestTenantContext requestTenantContext)
    {
        _repo = repo;
        _domain = domain;
        _notify = notify;
        _mapper = mapper;
        _currentUser = currentUser;
        _localization = localization;
        _requestTenantContext = requestTenantContext;
    }

    public async Task<IEnumerable<JwtKeyResponse>> GetByTenantAsync(int tenantId, CancellationToken ct)
    {
        var entities = await _repo.GetByTenantAsync(tenantId, ct);
        return _mapper.Map<IEnumerable<JwtKeyResponse>>(entities);
    }

    public async Task<JwtKeyResponse?> GetActiveKeyAsync(int tenantId, CancellationToken ct)
    {
        var entity = await _repo.GetActiveKeyAsync(tenantId, ct);
        return entity == null ? null : _mapper.Map<JwtKeyResponse>(entity);
    }

    public async Task<bool> CreateInitialIfNotExistsAsync(int tenantId, CancellationToken ct)
    {
        _requestTenantContext.SetTenantId(tenantId);

        var exists = await _repo.HasActiveKeyAsync(tenantId, ct);
        if (exists)
            return true;

        // Delegate to domain service to ensure or create initial key
        var created = await _domain.EnsureKeyExistsAsync(tenantId, _currentUser.GetUserId(), ct);
        return created != null;
    }

    public async Task<bool> RevokeAsync(int id, string? reason, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Jwt.Revoke.ResourceNotFound"), 410);
            return false;
        }

        if (entity.IsRevoked())
        {
            _notify.Add(_localization.GetMessage("Application.Service.Jwt.Revoke.AlreadyRevoked"), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Jwt.Revoke.ReasonRequired"), 400);
            return false;
        }

        // If it's active and the only key, domain will handle creating a replacement
        return await _domain.RevokeAsync(id, reason, _currentUser.GetUserId(), ct);
    }
}
