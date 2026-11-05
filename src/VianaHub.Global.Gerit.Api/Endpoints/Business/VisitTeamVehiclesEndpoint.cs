using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamVehicles;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamVehicle;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeamVehicles;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

[EndpointMapper]
public static class VisitTeamVehiclesEndpoint
{
    public static void MapVisitTeamVehiclesEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/visit-teams").WithTags("VisitTeamVehicles").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/{visitTeamId}/vehicles", async ([FromRoute] int visitTeamId, [FromServices] IVisitTeamVehiclesAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(visitTeamId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamVehicles", "GetAll")
        .WithName("GetVisitTeamVehicles")
        .WithSummary("Swagger.Endpoint.VisitTeamVehicle.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<VisitTeamVehicleResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{visitTeamId}/vehicles/{id}", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromServices] IVisitTeamVehiclesAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(visitTeamId, id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamVehicles", "GetBy")
        .WithName("GetVisitTeamVehicleById")
        .WithSummary("Swagger.Endpoint.VisitTeamVehicle.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<VisitTeamVehicleDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{visitTeamId}/vehicles/paged", async ([FromRoute] int visitTeamId, [AsParameters] PagedFilterRequest request, [FromServices] IVisitTeamVehiclesAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(visitTeamId, request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamVehicles", "GetPaged")
        .WithName("GetVisitTeamVehiclesPaged")
        .WithSummary("Swagger.Endpoint.VisitTeamVehicle.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<VisitTeamVehicleResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{visitTeamId}/vehicles", async ([FromRoute] int visitTeamId, [FromBody] CreateVisitTeamVehicleRequest request, [FromServices] IVisitTeamVehiclesAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(visitTeamId, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamVehicles", "Create")
        .WithName("CreateVisitTeamVehicle")
        .WithSummary("Swagger.Endpoint.VisitTeamVehicle.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateVisitTeamVehicleRequest>();

        groupV1.MapPut("/{visitTeamId}/vehicles/{id}", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromBody] UpdateVisitTeamVehicleRequest request, [FromServices] IVisitTeamVehiclesAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(visitTeamId, id, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamVehicles", "Update")
        .WithName("UpdateVisitTeamVehicle")
        .WithSummary("Swagger.Endpoint.VisitTeamVehicle.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateVisitTeamVehicleRequest>();

        groupV1.MapPatch("/{visitTeamId}/vehicles/{id}/activate", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromServices] IVisitTeamVehiclesAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(visitTeamId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamVehicles", "Activate")
        .WithName("ActivateVisitTeamVehicle")
        .WithSummary("Swagger.Endpoint.VisitTeamVehicle.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{visitTeamId}/vehicles/{id}/deactivate", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromServices] IVisitTeamVehiclesAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(visitTeamId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamVehicles", "Deactivate")
        .WithName("DeactivateVisitTeamVehicle")
        .WithSummary("Swagger.Endpoint.VisitTeamVehicle.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{visitTeamId}/vehicles/{id}", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromServices] IVisitTeamVehiclesAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(visitTeamId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamVehicles", "Delete")
        .WithName("DeleteVisitTeamVehicle")
        .WithSummary("Swagger.Endpoint.VisitTeamVehicle.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{visitTeamId}/vehicles/bulk-upload", async ([FromRoute] int visitTeamId, HttpRequest request, [FromServices] IVisitTeamVehiclesAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                notify.Add("Api.Upload.NoFileProvided", 400);
                return notify.CustomResponse();
            }

            var file = request.Form.Files[0];
            var success = await appService.BulkUploadAsync(visitTeamId, file, ct);
            return notify.CustomResponse(success);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamVehicles", "BulkUpload")
        .WithName("BulkUploadVisitTeamVehicles")
        .WithSummary("Swagger.Endpoint.VisitTeamVehicle.BulkUpload.Summary")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
