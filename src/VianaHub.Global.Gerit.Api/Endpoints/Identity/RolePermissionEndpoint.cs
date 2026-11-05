using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.RolePermission;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.RolePermission;
using VianaHub.Global.Gerit.Api.Endpoints.Base;

namespace VianaHub.Global.Gerit.Api.Endpoints.Identity;

[EndpointMapper]
public static class RolePermissionEndpoint
{
    public static void MapRolePermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/role-permissions").WithTags("RolePermissions").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/", async ([FromServices] IRolePermissionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var result = await appService.GetAllAsync(ct);
            return notify.CustomResponse(result, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "RolePermissions", "GetAll")
        .WithName("GetAllRolePermissions")
        .WithSummary("Swagger.Endpoint.RolePermission.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IList<RolePermissionResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/roles/{roleId}/resources/{resourceId}/actions/{actionId}", async ([FromRoute] int roleId, [FromRoute] int resourceId, [FromRoute] int actionId, [FromServices] IRolePermissionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var result = await appService.GetByIdAsync(roleId, resourceId, actionId, ct);
            return notify.CustomResponse(result, result != null ? 200 : 404);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "RolePermissions", "GetBy")
        .WithName("GetRolePermissionById")
        .WithSummary("Swagger.Endpoint.RolePermission.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<RolePermissionDetailResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/role/{roleId}", async ([FromRoute] int roleId, [FromServices] IRolePermissionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var result = await appService.GetByRoleAsync(roleId, ct);
            return notify.CustomResponse(result, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "RolePermissions", "GetBy")
        .WithName("GetRolePermissionsByRole")
        .WithSummary("Swagger.Endpoint.RolePermission.GetByRole.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IList<RolePermissionResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/resource/{resourceId}", async ([FromRoute] int resourceId, [FromServices] IRolePermissionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var result = await appService.GetByResourceAsync(resourceId, ct);
            return notify.CustomResponse(result, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "RolePermissions", "GetBy")
        .WithName("GetRolePermissionsByResource")
        .WithSummary("Swagger.Endpoint.RolePermission.GetByResource.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IList<RolePermissionResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/", async ([FromBody] CreateRolePermissionRequest request, [FromServices] IRolePermissionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var result = await appService.CreateAsync(request, ct);
            return notify.CustomResponse(result == null ? null : new GenericResponse { Id = result.Id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "RolePermissions", "Create")
        .WithName("CreateRolePermission")
        .WithSummary("Swagger.Endpoint.RolePermission.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/roles/{roleId}/resources/{resourceId}/actions/{actionId}", async ([FromRoute] int roleId, [FromRoute] int resourceId, [FromRoute] int actionId, [FromServices] IRolePermissionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            await appService.DeleteAsync(roleId, resourceId, actionId, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "RolePermissions", "Delete")
        .WithName("DeleteRolePermission")
        .WithSummary("Swagger.Endpoint.RolePermission.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        // Upload massivo de roles via CSV
        groupV1.MapPost("/bulk-upload", async (HttpRequest request, [FromServices] IRolePermissionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                notify.Add("Nenhum arquivo foi enviado", 400);
                return notify.CustomResponse();
            }

            var file = request.Form.Files[0];
            var success = await appService.BulkUploadAsync(file, ct);
            return notify.CustomResponse(success);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "RolePermissions", "BulkUpload")
        .WithName("BulkUploadRolePermission")
        .WithSummary("Swagger.Endpoint.RolePermission.BulkUpload.Summary")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
