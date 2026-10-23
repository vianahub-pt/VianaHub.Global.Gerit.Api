using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Action;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Action;
using VianaHub.Global.Gerit.Application.Interfaces.Billing;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Identity;

[EndpointMapper]
public static class ActionEndpoint
{
    public static void MapActionEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/actions").WithTags("Actions").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/", async ([FromServices] IActionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager,Operator", "Actions", "GetAll")
        .WithName("GetAllActions")
        .WithSummary("Swagger.Endpoint.Action.GetAll.Summary")
        .Produces<IEnumerable<ActionResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{id}", async ([FromRoute] int id, [FromServices] IActionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager,Operator", "Actions", "GetBy")
        .WithName("GetActionById")
        .WithSummary("Swagger.Endpoint.Action.GetById.Summary")
        .Produces<ActionDetailResponse?>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/paged", async ([AsParameters] PagedFilterRequest request, [FromServices] IActionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager,Operator", "Actions", "GetPaged")
        .WithName("GetActionsPaged")
        .WithSummary("Swagger.Endpoint.Action.GetPaged.Summary")
        .Produces<ListPageResponse<ActionResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/", async ([FromBody] CreateActionRequest request, [FromServices] IActionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice", "Actions", "Create")
        .WithName("CreateAction")
        .WithSummary("Swagger.Endpoint.Action.Create.Summary")
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateActionRequest>();

        groupV1.MapPut("/{id}", async ([FromRoute] int id, [FromBody] UpdateActionRequest request, [FromServices] IActionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(id, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice", "Actions", "Update")
        .WithName("UpdateAction")
        .WithSummary("Swagger.Endpoint.Action.Update.Summary")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateActionRequest>();

        groupV1.MapPatch("/{id}/activate", async ([FromRoute] int id, [FromServices] IActionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice", "Actions", "Activate")
        .WithName("ActivateAction")
        .WithSummary("Swagger.Endpoint.Action.Activate.Summary")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{id}/deactivate", async ([FromRoute] int id, [FromServices] IActionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice", "Actions", "Deactivate")
        .WithName("DeactivateAction")
        .WithSummary("Swagger.Endpoint.Action.Deactivate.Summary")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{id}", async ([FromRoute] int id, [FromServices] IActionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice", "Actions", "Delete")
        .WithName("DeleteAction")
        .WithSummary("Swagger.Endpoint.Action.Delete.Summary")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        // Upload massivo de Functions via CSV
        groupV1.MapPost("/bulk-upload", async (HttpRequest request, [FromServices] IActionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                // Utiliza chave de tradução para mensagem
                notify.Add("Api.Upload.NoFileProvided", 400);
                return notify.CustomResponse();
            }

            var file = request.Form.Files[0];
            var success = await appService.BulkUploadAsync(file, ct);
            return notify.CustomResponse(success);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "Actions", "BulkUpload")
        .WithName("BulkUploadAction")
        .WithSummary("Swagger.Endpoint.Action.BulkUpload.Summary")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
