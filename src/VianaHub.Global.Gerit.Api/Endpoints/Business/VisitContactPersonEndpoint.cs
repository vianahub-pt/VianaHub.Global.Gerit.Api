using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitContactPersons;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitContactPersons;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

[EndpointMapper]
public static class VisitContactPersonEndpoint
{
    public static void MapVisitContactPersonEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/visits").WithTags("VisitContactPersons").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/{visitId}/contacts", async ([FromRoute] int visitId, [FromServices] IVisitContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(visitId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitContactPersons", "GetAll")
        .WithName("GetVisitContacts")
        .WithSummary("Swagger.Endpoint.VisitContactPerson.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<VisitContactPersonResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{visitId}/contacts/{id}", async ([FromRoute] int visitId, [FromRoute] int id, [FromServices] IVisitContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(visitId, id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitContactPersons", "GetBy")
        .WithName("GetVisitContactById")
        .WithSummary("Swagger.Endpoint.VisitContactPerson.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<VisitContactPersonDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{visitId}/contacts/paged", async ([FromRoute] int visitId, [AsParameters] PagedFilterRequest request, [FromServices] IVisitContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(visitId, request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitContactPersons", "GetPaged")
        .WithName("GetVisitContactsPaged")
        .WithSummary("Swagger.Endpoint.VisitContactPerson.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<VisitContactPersonResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{visitId}/contacts", async ([FromRoute] int visitId, [FromBody] CreateVisitContactPersonRequest request, [FromServices] IVisitContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(visitId, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitContactPersons", "Create")
        .WithName("CreateVisitContact")
        .WithSummary("Swagger.Endpoint.VisitContactPerson.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateVisitContactPersonRequest>();

        groupV1.MapPut("/{visitId}/contacts/{id}", async ([FromRoute] int visitId, [FromRoute] int id, [FromBody] UpdateVisitContactPersonRequest request, [FromServices] IVisitContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(visitId, id, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitContactPersons", "Update")
        .WithName("UpdateVisitContact")
        .WithSummary("Swagger.Endpoint.VisitContactPerson.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateVisitContactPersonRequest>();

        groupV1.MapPatch("/{visitId}/contacts/{id}/activate", async ([FromRoute] int visitId, [FromRoute] int id, [FromServices] IVisitContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(visitId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitContactPersons", "Activate")
        .WithName("ActivateVisitContact")
        .WithSummary("Swagger.Endpoint.VisitContactPerson.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{visitId}/contacts/{id}/deactivate", async ([FromRoute] int visitId, [FromRoute] int id, [FromServices] IVisitContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(visitId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitContactPersons", "Deactivate")
        .WithName("DeactivateVisitContact")
        .WithSummary("Swagger.Endpoint.VisitContactPerson.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{visitId}/contacts/{id}", async ([FromRoute] int visitId, [FromRoute] int id, [FromServices] IVisitContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(visitId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitContactPersons", "Delete")
        .WithName("DeleteVisitContact")
        .WithSummary("Swagger.Endpoint.VisitContactPerson.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{visitId}/contacts/bulk-upload", async ([FromRoute] int visitId, HttpRequest request, [FromServices] IVisitContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
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
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitContactPersons", "BulkUpload")
        .WithName("BulkUploadVisitContacts")
        .WithSummary("Swagger.Endpoint.VisitContactPerson.BulkUpload.Summary")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
