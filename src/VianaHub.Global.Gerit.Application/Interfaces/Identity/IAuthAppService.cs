using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Auth;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.Tenant;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Auth;

namespace VianaHub.Global.Gerit.Application.Interfaces.Identity;

public interface IAuthAppService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<AuthResponse?> RefreshAsync(RefreshRequest request, CancellationToken ct);
    Task<IEnumerable<TenantLoginResponse>> GetLoginAsync(CancellationToken ct);
}
