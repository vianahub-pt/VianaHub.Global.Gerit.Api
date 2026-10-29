using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.UserRole;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.UserRole;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Identity;

[EndpointMapper]
public static class UserRoleEndpoint
{
    public static void MapUserRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/user-roles").WithTags("UserRoles").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/", async ([FromServices] IUserRoleAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var result = await appService.GetAllAsync(ct);
            return notify.CustomResponse(result, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "UserRoles", "GetAll")
        .WithName("GetAllUserRoles")
        .WithSummary("Swagger.Endpoint.UserRole.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IList<UserRoleResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/users/{userId}/roles/{roleId}", async ([FromRoute] int userId, [FromRoute] int roleId, [FromServices] IUserRoleAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var result = await appService.GetByIdAsync(userId, roleId, ct);
            return notify.CustomResponse(result, result != null ? 200 : 404);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "UserRoles", "GetBy")
        .WithName("GetUserRoleById")
        .WithSummary("Swagger.Endpoint.UserRole.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<UserRoleDetailResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/user/{userId}", async ([FromRoute] int userId, [FromServices] IUserRoleAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var result = await appService.GetByUserAsync(userId, ct);
            return notify.CustomResponse(result, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager,User", "UserRoles", "GetBy")
        .WithName("GetUserRolesByUser")
        .WithSummary("Swagger.Endpoint.UserRole.GetByUser.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IList<UserRoleResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/role/{roleId}", async ([FromRoute] int roleId, [FromServices] IUserRoleAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var result = await appService.GetByRoleAsync(roleId, ct);
            return notify.CustomResponse(result, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager,User", "UserRoles", "GetBy")
        .WithName("GetUserRolesByRole")
        .WithSummary("Swagger.Endpoint.UserRole.GetByRole.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IList<UserRoleResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/", async ([FromBody] CreateUserRoleRequest request, [FromServices] IUserRoleAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var result = await appService.CreateAsync(request, ct);
            return notify.CustomResponse(result == null ? null : new GenericResponse { Id = result.Id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "UserRoles", "Create")
        .WithName("CreateUserRole")
        .WithSummary("Swagger.Endpoint.UserRole.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateUserRoleRequest>();

        groupV1.MapDelete("/users/{userId}/roles/{roleId}", async ([FromRoute] int userId, [FromRoute] int roleId, [FromServices] IUserRoleAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            await appService.DeleteAsync(userId, roleId, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "UserRoles", "Delete")
        .WithName("DeleteUserRole")
        .WithSummary("Swagger.Endpoint.UserRole.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/bulk-upload", async (HttpRequest request, [FromServices] IUserRoleAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
            {
                notify.Add("Api.Upload.NoFileProvided", 400);
                return notify.CustomResponse();
            }

            var form = await request.ReadFormAsync(ct);
            if (form.Files.Count == 0)
            {
                notify.Add("Api.Upload.NoFileProvided", 400);
                return notify.CustomResponse();
            }

            var file = form.Files[0];
            var success = await appService.BulkUploadAsync(file, ct);
            return notify.CustomResponse(success);
        })
        .CustomAuthorize("Admin,BackOffice", "UserRoles", "BulkUpload")
        .WithName("BulkUploadUserRole")
        .WithSummary("Swagger.Endpoint.UserRole.BulkUpload.Summary")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
