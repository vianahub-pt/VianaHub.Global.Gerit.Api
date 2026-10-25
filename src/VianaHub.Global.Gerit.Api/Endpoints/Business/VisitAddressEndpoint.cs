using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitAddress;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitAddress;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

/// <summary>
/// Endpoints para VisitAddress
/// </summary>
[EndpointMapper]
public static class VisitAddressEndpoint
{
    public static void MapVisitAddressEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/visits").WithTags("VisitAddresses").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/{visitId}/addresses", async ([FromRoute] int visitId, [FromServices] IVisitAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(visitId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAddresses", "GetAll")
        .WithName("GetVisitAddresses")
        .WithSummary("Swagger.Endpoint.VisitAddress.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<VisitAddressResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{visitId}/addresses/{id}", async ([FromRoute] int visitId, [FromRoute] int id, [FromServices] IVisitAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(visitId, id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAddresses", "GetBy")
        .WithName("GetVisitAddressById")
        .WithSummary("Swagger.Endpoint.VisitAddress.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<VisitAddressDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{visitId}/addresses/paged", async ([FromRoute] int visitId, [AsParameters] PagedFilterRequest request, [FromServices] IVisitAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(visitId, request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAddresses", "GetPaged")
        .WithName("GetVisitAddressesPaged")
        .WithSummary("Swagger.Endpoint.VisitAddress.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<VisitAddressResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{visitId}/addresses", async ([FromRoute] int visitId, [FromBody] CreateVisitAddressRequest request, [FromServices] IVisitAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(visitId, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAddresses", "Create")
        .WithName("CreateVisitAddress")
        .WithSummary("Swagger.Endpoint.VisitAddress.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateVisitAddressRequest>();

        groupV1.MapPut("/{visitId}/addresses/{id}", async ([FromRoute] int visitId, [FromRoute] int id, [FromBody] UpdateVisitAddressRequest request, [FromServices] IVisitAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(visitId, id, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAddresses", "Update")
        .WithName("UpdateVisitAddress")
        .WithSummary("Swagger.Endpoint.VisitAddress.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateVisitAddressRequest>();

        groupV1.MapPatch("/{visitId}/addresses/{id}/activate", async ([FromRoute] int visitId, [FromRoute] int id, [FromServices] IVisitAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(visitId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAddresses", "Activate")
        .WithName("ActivateVisitAddress")
        .WithSummary("Swagger.Endpoint.VisitAddress.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{visitId}/addresses/{id}/deactivate", async ([FromRoute] int visitId, [FromRoute] int id, [FromServices] IVisitAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(visitId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAddresses", "Deactivate")
        .WithName("DeactivateVisitAddress")
        .WithSummary("Swagger.Endpoint.VisitAddress.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{visitId}/addresses/{id}", async ([FromRoute] int visitId, [FromRoute] int id, [FromServices] IVisitAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(visitId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAddresses", "Delete")
        .WithName("DeleteVisitAddress")
        .WithSummary("Swagger.Endpoint.VisitAddress.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{visitId}/addresses/bulk-upload", async ([FromRoute] int visitId, HttpRequest request, [FromServices] IVisitAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                notify.Add("Api.Upload.NoFileProvided", 400);
                return notify.CustomResponse();
            }

            var file = request.Form.Files[0];
            var success = await appService.BulkUploadAsync(visitId, file, ct);
            return notify.CustomResponse(success);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitAddresses", "BulkUpload")
        .WithName("BulkUploadVisitAddresses")
        .WithSummary("Swagger.Endpoint.VisitAddress.BulkUpload.Summary")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
