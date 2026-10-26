using VianaHub.Global.Gerit.Application.Dtos.Response.Business.StatusDefinition;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDefinition;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

/// <summary>
/// Endpoints para gestão de Definições de Status (CRUD + traduções).
/// </summary>
[EndpointMapper]
public static class StatusDefinitionEndpoint
{
    public static void MapStatusDefinitionEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/status-definitions")
            .WithTags("StatusDefinitions")
            .WithGroupName("v1")
            .RequireAuthorization();

        // ─── CRUD principal ────────────────────────────────────────────────

        groupV1.MapGet("/", async (
            [FromServices] IStatusDefinitionAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDefinitions", "GetAll")
        .WithName("GetStatusDefinitions")
        .WithSummary("Swagger.Endpoint.StatusDefinition.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<StatusDefinitionResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{id}", async (
            [FromRoute] int id,
            [FromServices] IStatusDefinitionAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDefinitions", "GetBy")
        .WithName("GetStatusDefinitionById")
        .WithSummary("Swagger.Endpoint.StatusDefinition.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<StatusDefinitionDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/paged", async (
            [AsParameters] PagedFilterRequest request,
            [FromServices] IStatusDefinitionAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDefinitions", "GetPaged")
        .WithName("GetStatusDefinitionsPaged")
        .WithSummary("Swagger.Endpoint.StatusDefinition.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<StatusDefinitionResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/", async (
            [FromBody] CreateStatusDefinitionRequest request,
            [FromServices] IStatusDefinitionAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDefinitions", "Create")
        .WithName("CreateStatusDefinition")
        .WithSummary("Swagger.Endpoint.StatusDefinition.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateStatusDefinitionRequest>();

        groupV1.MapPut("/{id}", async (
            [FromRoute] int id,
            [FromBody] UpdateStatusDefinitionRequest request,
            [FromServices] IStatusDefinitionAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(id, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDefinitions", "Update")
        .WithName("UpdateStatusDefinition")
        .WithSummary("Swagger.Endpoint.StatusDefinition.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateStatusDefinitionRequest>();

        groupV1.MapPatch("/{id}/activate", async (
            [FromRoute] int id,
            [FromServices] IStatusDefinitionAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            await appService.ActivateAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDefinitions", "Activate")
        .WithName("ActivateStatusDefinition")
        .WithSummary("Swagger.Endpoint.StatusDefinition.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{id}/deactivate", async (
            [FromRoute] int id,
            [FromServices] IStatusDefinitionAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            await appService.DeactivateAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDefinitions", "Deactivate")
        .WithName("DeactivateStatusDefinition")
        .WithSummary("Swagger.Endpoint.StatusDefinition.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{id}", async (
            [FromRoute] int id,
            [FromServices] IStatusDefinitionAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            await appService.DeleteAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDefinitions", "Delete")
        .WithName("DeleteStatusDefinition")
        .WithSummary("Swagger.Endpoint.StatusDefinition.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{id}/translations", async (
            [FromRoute] int id,
            [FromBody] CreateStatusDefinitionTranslationRequest request,
            [FromServices] IStatusDefinitionAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var translationId = await appService.CreateTranslationAsync(id, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = translationId }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDefinitions", "CreateTranslation")
        .WithName("CreateStatusDefinitionTranslation")
        .WithSummary("Swagger.Endpoint.StatusDefinition.CreateTranslation.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPut("/{id}/translations/{translationId}", async (
            [FromRoute] int id,
            [FromRoute] int translationId,
            [FromBody] UpdateStatusDefinitionTranslationRequest request,
            [FromServices] IStatusDefinitionAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var updated = await appService.UpdateTranslationAsync(id, translationId, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDefinitions", "UpdateTranslation")
        .WithName("UpdateStatusDefinitionTranslation")
        .WithSummary("Swagger.Endpoint.StatusDefinition.UpdateTranslation.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{id}/translations/{translationId}", async (
            [FromRoute] int id,
            [FromRoute] int translationId,
            [FromServices] IStatusDefinitionAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            await appService.DeleteTranslationAsync(id, translationId, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDefinitions", "DeleteTranslation")
        .WithName("DeleteStatusDefinitionTranslation")
        .WithSummary("Swagger.Endpoint.StatusDefinition.DeleteTranslation.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
