using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Auth;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Auth;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Identity;

[EndpointMapper]
public static class AuthEndpoint
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/auth")
            .WithTags("Auth")
            .WithGroupName("v1")
            .AllowAnonymous();

        groupV1.MapPost("/register", async ([FromBody] RegisterRequest request, [FromServices] IAuthAppService authService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var result = await authService.RegisterAsync(request, ct);
            return notify.CustomResponse(result);
        })
        .WithName("Auth.Register")
        .RequireRateLimiting("authentication")
        .WithValidation<RegisterRequest>()
        .WithSummary("Swagger.Endpoint.Auth.Register.Summary")
        .Produces<AuthResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict);

        groupV1.MapPost("/login", async ([FromBody] LoginRequest request, [FromServices] IAuthAppService authService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var result = await authService.LoginAsync(request, ct);
            return notify.CustomResponse(result);
        })
        .WithName("Auth.Login")
        .RequireRateLimiting("authentication")
        .WithValidation<LoginRequest>()
        .WithSummary("Swagger.Endpoint.Auth.Login.Summary")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        groupV1.MapPost("/refresh", async ([FromBody] RefreshRequest request, [FromServices] IAuthAppService authService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var result = await authService.RefreshAsync(request, ct);
            return notify.CustomResponse(result);
        })
        .WithName("Auth.Refresh")
        .RequireRateLimiting("refreshtoken")
        .WithValidation<RefreshRequest>()
        .WithSummary("Swagger.Endpoint.Auth.Refresh.Summary")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        groupV1.MapGet("/tenants", async ([FromServices] IAuthAppService authService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var result = await authService.GetLoginAsync(ct);
            return notify.CustomResponse(result);
        })
        .WithName("Auth.Tenant")
        .RequireRateLimiting("tenant")
        .WithValidation<RefreshRequest>()
        .WithSummary("Swagger.Endpoint.Auth.Tenant.Summary")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);
    }
}
