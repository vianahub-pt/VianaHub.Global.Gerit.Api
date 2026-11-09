using System.Security.Claims;
using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VianaHub.Global.Gerit.Tests.Infrastructure;

public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.ContainsKey("X-Test-Anonymous"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var tenantId = Request.Headers["X-Test-Tenant"].FirstOrDefault() ?? "1";
        var permissions = (Request.Headers["X-Test-Permissions"].FirstOrDefault() ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var permissionResource = Request.Headers["X-Test-Permission-Resource"].FirstOrDefault() ?? "Roles";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Request.Headers["X-Test-User"].FirstOrDefault() ?? "-9001"),
            new("tenant_id", tenantId),
            new("permissions", JsonSerializer.Serialize(new Dictionary<string, string[]> { [permissionResource] = permissions }))
        };
        var role = Request.Headers["X-Test-Role"].FirstOrDefault() ?? "BackOffice";
        if (role != "none")
            claims.Add(new Claim(ClaimTypes.Role, role));

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
    }
}
