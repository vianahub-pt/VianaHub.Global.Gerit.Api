using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamEquipments;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeamEquipments;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

[EndpointMapper]
public static class VisitTeamEquipmentsEndpoint
{
    public static void MapVisitTeamEquipmentsEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/visit-teams").WithTags("VisitTeamEquipments").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/{visitTeamId}/equipments", async ([FromRoute] int visitTeamId, [FromServices] IVisitTeamEquipmentsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(visitTeamId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEquipments", "GetAll")
        .WithName("GetVisitTeamEquipments")
        .WithSummary("Swagger.Endpoint.VisitTeamEquipment.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<VisitTeamEquipmentResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{visitTeamId}/equipments/{id}", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromServices] IVisitTeamEquipmentsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(visitTeamId, id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEquipments", "GetBy")
        .WithName("GetVisitTeamEquipmentById")
        .WithSummary("Swagger.Endpoint.VisitTeamEquipment.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<VisitTeamEquipmentDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{visitTeamId}/equipments/paged", async ([FromRoute] int visitTeamId, [AsParameters] PagedFilterRequest request, [FromServices] IVisitTeamEquipmentsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(visitTeamId, request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEquipments", "GetPaged")
        .WithName("GetVisitTeamEquipmentsPaged")
        .WithSummary("Swagger.Endpoint.VisitTeamEquipment.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<VisitTeamEquipmentResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{visitTeamId}/equipments", async ([FromRoute] int visitTeamId, [FromBody] CreateVisitTeamEquipmentRequest request, [FromServices] IVisitTeamEquipmentsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(visitTeamId, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEquipments", "Create")
        .WithName("CreateVisitTeamEquipment")
        .WithSummary("Swagger.Endpoint.VisitTeamEquipment.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateVisitTeamEquipmentRequest>();

        groupV1.MapPut("/{visitTeamId}/equipments/{id}", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromBody] UpdateVisitTeamEquipmentRequest request, [FromServices] IVisitTeamEquipmentsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(visitTeamId, id, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEquipments", "Update")
        .WithName("UpdateVisitTeamEquipment")
        .WithSummary("Swagger.Endpoint.VisitTeamEquipment.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateVisitTeamEquipmentRequest>();

        groupV1.MapPatch("/{visitTeamId}/equipments/{id}/activate", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromServices] IVisitTeamEquipmentsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(visitTeamId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEquipments", "Activate")
        .WithName("ActivateVisitTeamEquipment")
        .WithSummary("Swagger.Endpoint.VisitTeamEquipment.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{visitTeamId}/equipments/{id}/deactivate", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromServices] IVisitTeamEquipmentsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(visitTeamId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEquipments", "Deactivate")
        .WithName("DeactivateVisitTeamEquipment")
        .WithSummary("Swagger.Endpoint.VisitTeamEquipment.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{visitTeamId}/equipments/{id}", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromServices] IVisitTeamEquipmentsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(visitTeamId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEquipments", "Delete")
        .WithName("DeleteVisitTeamEquipment")
        .WithSummary("Swagger.Endpoint.VisitTeamEquipment.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{visitTeamId}/equipments/bulk-upload", async ([FromRoute] int visitTeamId, HttpRequest request, [FromServices] IVisitTeamEquipmentsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
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
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEquipments", "BulkUpload")
        .WithName("BulkUploadVisitTeamEquipments")
        .WithSummary("Swagger.Endpoint.VisitTeamEquipment.BulkUpload.Summary")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
