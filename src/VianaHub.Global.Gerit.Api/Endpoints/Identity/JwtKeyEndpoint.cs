using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Jwt;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Auth;
using VianaHub.Global.Gerit.Api.Endpoints.Base;

namespace VianaHub.Global.Gerit.Api.Endpoints.Identity;

[EndpointMapper]
public static class JwtKeyEndpoint
{
    public static void MapJwtKeyEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/admin/jwtkeys").WithTags("JwtKeys").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/{tenantId}", async ([FromRoute] int tenantId, [FromServices] IJwtKeyAppService service, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var result = await service.GetByTenantAsync(tenantId, ct);
            return notify.CustomResponse(result, StatusCodes.Status200OK);
        })
        .CustomAuthorize("Admin,BackOffice", "JwtKeys", "GetBy")
        .WithName("GetJwtKeysByTenant")
        .WithSummary("Swagger.Endpoint.JwtKey.GetJwtKeysByTenant.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<JwtKeyResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{tenantId}/active", async ([FromRoute] int tenantId, [FromServices] IJwtKeyAppService service, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var result = await service.GetActiveKeyAsync(tenantId, ct);
            if (result == null)
            {
                return notify.CustomResponse(StatusCodes.Status204NoContent);
            }
            return notify.CustomResponse(result, StatusCodes.Status200OK);
        })
        .CustomAuthorize("Admin,BackOffice", "JwtKeys", "GetActive")
        .WithName("GetActiveJwtKey")
        .WithSummary("Swagger.Endpoint.JwtKey.GetActiveJwtKey.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<JwtKeyResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{tenantId}/create-initial", async ([FromRoute] int tenantId, [FromServices] IJwtKeyAppService service, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await service.CreateInitialIfNotExistsAsync(tenantId, ct);
            if (!ok) return notify.CustomResponse();
            return notify.CustomResponse(new GenericResponse { Id = tenantId }, 201);
        })
        .CustomAuthorize("Admin,BackOffice", "JwtKeys", "Create")
        .WithName("CreateInitialJwtKey")
        .WithSummary("Swagger.Endpoint.JwtKey.CreateInitialJwtKey.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{id}/revoke", async ([FromRoute] int id, [FromBody] RevokeRequest req, [FromServices] IJwtKeyAppService service, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await service.RevokeAsync(id, req.Reason, ct);
            if (!ok) return notify.CustomResponse();
            return notify.CustomResponse(StatusCodes.Status200OK);
        })
        .CustomAuthorize("Admin,BackOffice", "JwtKeys", "Revoke")
        .WithName("RevokeJwtKey")
        .WithSummary("Swagger.Endpoint.JwtKey.RevokeJwtKey.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
