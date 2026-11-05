using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitAttachment;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitAttachment;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

[EndpointMapper]
public static class VisitAttachmentEndpoint
{
    public static void MapVisitAttachmentEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/visits").WithTags("VisitAttachments").WithGroupName("v1").RequireAuthorization();

        // Nested routes under /v1/visits/{visitId}/attachments
        groupV1.MapGet("/{visitId}/attachments", async ([FromRoute] int visitId, [FromServices] IVisitAttachmentAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(visitId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAttachments", "GetAll")
        .WithName("GetVisitAttachments")
        .WithSummary("Swagger.Endpoint.VisitAttachment.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<VisitAttachmentResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{visitId}/attachments/{id}", async ([FromRoute] int visitId, [FromRoute] int id, [FromServices] IVisitAttachmentAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(visitId, id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAttachments", "GetBy")
        .WithName("GetVisitAttachmentById")
        .WithSummary("Swagger.Endpoint.VisitAttachment.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<VisitAttachmentDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{visitId}/attachments/paged", async ([FromRoute] int visitId, [AsParameters] PagedFilterRequest request, [FromServices] IVisitAttachmentAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(visitId, request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAttachments", "GetPaged")
        .WithName("GetVisitAttachmentsPaged")
        .WithSummary("Swagger.Endpoint.VisitAttachment.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<VisitAttachmentResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{visitId}/attachments/primary", async ([FromRoute] int visitId, [FromServices] IVisitAttachmentAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPrimaryByVisitIdAsync(visitId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAttachments", "GetBy")
        .WithName("GetPrimaryVisitAttachment")
        .WithSummary("Swagger.Endpoint.VisitAttachment.GetPrimaryByVisitId.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<VisitAttachmentResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{visitId}/attachments", async ([FromRoute] int visitId, [FromBody] CreateVisitAttachmentRequest request, [FromServices] IVisitAttachmentAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(visitId, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAttachments", "Create")
        .WithName("CreateVisitAttachment")
        .WithSummary("Swagger.Endpoint.VisitAttachment.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateVisitAttachmentRequest>();

        groupV1.MapPut("/{visitId}/attachments/{id}", async ([FromRoute] int visitId, [FromRoute] int id, [FromBody] UpdateVisitAttachmentRequest request, [FromServices] IVisitAttachmentAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(visitId, id, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAttachments", "Update")
        .WithName("UpdateVisitAttachment")
        .WithSummary("Swagger.Endpoint.VisitAttachment.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateVisitAttachmentRequest>();

        groupV1.MapPatch("/{visitId}/attachments/{id}/set-primary", async ([FromRoute] int visitId, [FromRoute] int id, [FromServices] IVisitAttachmentAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.SetAsPrimaryAsync(visitId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAttachments", "SetPrimary")
        .WithName("SetVisitAttachmentAsPrimary")
        .WithSummary("Swagger.Endpoint.VisitAttachment.SetAsPrimary.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{visitId}/attachments/{id}/activate", async ([FromRoute] int visitId, [FromRoute] int id, [FromServices] IVisitAttachmentAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(visitId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAttachments", "Activate")
        .WithName("ActivateVisitAttachment")
        .WithSummary("Swagger.Endpoint.VisitAttachment.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{visitId}/attachments/{id}/deactivate", async ([FromRoute] int visitId, [FromRoute] int id, [FromServices] IVisitAttachmentAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(visitId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAttachments", "Deactivate")
        .WithName("DeactivateVisitAttachment")
        .WithSummary("Swagger.Endpoint.VisitAttachment.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{visitId}/attachments/{id}", async ([FromRoute] int visitId, [FromRoute] int id, [FromServices] IVisitAttachmentAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(visitId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAttachments", "Delete")
        .WithName("DeleteVisitAttachment")
        .WithSummary("Swagger.Endpoint.VisitAttachment.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        // Global route (not nested) — kept at /attachments/public/{publicId}
        groupV1.MapGet("/attachments/public/{publicId}", async ([FromRoute] Guid publicId, [FromServices] IVisitAttachmentAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByPublicIdAsync(publicId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAttachments", "GetBy")
        .WithName("GetVisitAttachmentByPublicId")
        .WithSummary("Swagger.Endpoint.VisitAttachment.GetByPublicId.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<VisitAttachmentResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
