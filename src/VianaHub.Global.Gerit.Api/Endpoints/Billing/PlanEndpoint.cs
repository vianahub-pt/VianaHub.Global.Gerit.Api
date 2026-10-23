using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.Plan;
using VianaHub.Global.Gerit.Application.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Billing;

[EndpointMapper]
public static class PlanEndpoint
{
    public static void MapPlanEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/plans").WithTags("Plans").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/", async ([FromServices] IPlanAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager,Operator", "Plans", "GetAll")
        .WithName("GetAllPlans")
        .WithSummary("Swagger.Endpoint.Plan.GetAll.Summary")
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{id}", async ([FromRoute] int id, [FromServices] IPlanAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager,Operator", "Plans", "GetBy")
        .WithName("GetPlanById")
        .WithSummary("Swagger.Endpoint.Plan.GetById.Summary")
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/paged", async ([AsParameters] PagedFilterRequest request, [FromServices] IPlanAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager,Operator", "Plans", "GetPaged")
        .WithName("GetPagedPlans")
        .WithSummary("Swagger.Endpoint.Plan.GetPaged.Summary")
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/", async ([FromBody] CreatePlanRequest request, [FromServices] IPlanAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "Plans", "Create")
        .WithName("CreatePlan")
        .WithSummary("Swagger.Endpoint.Plan.Create.Summary")
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreatePlanRequest>();

        groupV1.MapPut("/{id}", async ([FromRoute] int id, [FromBody] UpdatePlanRequest request, [FromServices] IPlanAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(id, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "Plans", "Update")
        .WithName("UpdatePlan")
        .WithSummary("Swagger.Endpoint.Plan.Update.Summary")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdatePlanRequest>();

        groupV1.MapPatch("/{id}/activate", async ([FromRoute] int id, [FromServices] IPlanAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "Plans", "Activate")
        .WithName("ActivatePlan")
        .WithSummary("Swagger.Endpoint.Plan.Activate.Summary")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{id}/deactivate", async ([FromRoute] int id, [FromServices] IPlanAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "Plans", "Deactivate")
        .WithName("DeactivatePlan")
        .WithSummary("Swagger.Endpoint.Plan.Deactivate.Summary")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{id}", async ([FromRoute] int id, [FromServices] IPlanAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "Plans", "Delete")
        .WithName("DeletePlan")
        .WithSummary("Swagger.Endpoint.Plan.Delete.Summary")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        // Upload massivo de Functions via CSV
        groupV1.MapPost("/bulk-upload", async (HttpRequest request, [FromServices] IPlanAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                // Utiliza chave de tradu��o para mensagem
                notify.Add("Api.Upload.NoFileProvided", 400);
                return notify.CustomResponse();
            }

            var file = request.Form.Files[0];
            var success = await appService.BulkUploadAsync(file, ct);
            return notify.CustomResponse(success);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "Plans", "BulkUpload")
        .WithName("BulkUploadPlans")
        .WithSummary("Swagger.Endpoint.Plan.BulkUpload.Summary")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
