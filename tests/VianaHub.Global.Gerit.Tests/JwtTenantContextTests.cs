using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Infra.Data.Interceptors;

namespace VianaHub.Global.Gerit.Tests;

public class JwtTenantContextTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NoHttpContext_ExplicitScopedTenantIsResolved(bool connectionInterceptor)
    {
        Assert.Equal(42, Resolve(connectionInterceptor, null, 42));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NoHttpContext_WithoutScopedTenantRemainsUnresolved(bool connectionInterceptor)
    {
        Assert.Null(Resolve(connectionInterceptor, null, null));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AuthenticatedJwtTenant_TakesPrecedenceOverScopedTenant(bool connectionInterceptor)
    {
        var context = Context(new[] { new Claim("tenant_id", "7") }, authenticated: true);

        Assert.Equal(7, Resolve(connectionInterceptor, context, 42));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AuthenticatedJwtWithInvalidTenant_DoesNotFallBackToScopedTenant(bool connectionInterceptor)
    {
        var context = Context(new[] { new Claim("tenant_id", "invalid") }, authenticated: true);

        Assert.Null(Resolve(connectionInterceptor, context, 42));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AuthenticatedJwtWithoutTenant_DoesNotFallBackToScopedTenant(bool connectionInterceptor)
    {
        var context = Context(Array.Empty<Claim>(), authenticated: true);

        Assert.Null(Resolve(connectionInterceptor, context, 42));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UnauthenticatedPrincipal_UsesScopedTenantInsteadOfUntrustedClaims(bool connectionInterceptor)
    {
        var context = Context(new[] { new Claim("tenant_id", "7") }, authenticated: false);

        Assert.Equal(42, Resolve(connectionInterceptor, context, 42));
    }

    private static DefaultHttpContext Context(IEnumerable<Claim> claims, bool authenticated) => new()
    {
        User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticated ? "Bearer" : null))
    };

    private static int? Resolve(bool connectionInterceptor, HttpContext? context, int? tenantId)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(context);
        var tenant = new Mock<IRequestTenantContext>();
        tenant.SetupGet(t => t.TenantId).Returns(tenantId);
        object interceptor = connectionInterceptor
            ? new TenantSessionConnectionInterceptor(accessor.Object, NullLogger<TenantSessionConnectionInterceptor>.Instance, tenant.Object)
            : new TenantSessionCommandInterceptor(accessor.Object, tenant.Object, NullLogger<TenantSessionCommandInterceptor>.Instance);
        var method = interceptor.GetType().GetMethod(connectionInterceptor ? "ResolvetenantId" : "ResolveTenantId",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (int?)method.Invoke(interceptor, null);
    }
}
