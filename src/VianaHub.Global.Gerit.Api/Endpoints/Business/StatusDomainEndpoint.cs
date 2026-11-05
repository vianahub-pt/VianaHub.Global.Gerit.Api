using VianaHub.Global.Gerit.Application.Dtos.Response.Business.StatusDomain;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDomain;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

/// <summary>
/// Endpoints para gestão de Domínios de Status (CRUD + traduções).
/// </summary>
[EndpointMapper]
public static class StatusDomainEndpoint
{
    public static void MapStatusDomainEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/status-domains")
            .WithTags("StatusDomains")
            .WithGroupName("v1")
            .RequireAuthorization();

        // ─── CRUD principal ────────────────────────────────────────────────

        groupV1.MapGet("/", async (
            [FromServices] IStatusDomainAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDomains", "GetAll")
        .WithName("GetStatusDomains")
        .WithSummary("Swagger.Endpoint.StatusDomain.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<StatusDomainResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{id}", async (
            [FromRoute] int id,
            [FromServices] IStatusDomainAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDomains", "GetBy")
        .WithName("GetStatusDomainById")
        .WithSummary("Swagger.Endpoint.StatusDomain.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<StatusDomainDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/paged", async (
            [AsParameters] PagedFilterRequest request,
            [FromServices] IStatusDomainAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDomains", "GetPaged")
        .WithName("GetStatusDomainsPaged")
        .WithSummary("Swagger.Endpoint.StatusDomain.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<StatusDomainResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/", async (
            [FromBody] CreateStatusDomainRequest request,
            [FromServices] IStatusDomainAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDomains", "Create")
        .WithName("CreateStatusDomain")
        .WithSummary("Swagger.Endpoint.StatusDomain.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateStatusDomainRequest>();

        groupV1.MapPut("/{id}", async (
            [FromRoute] int id,
            [FromBody] UpdateStatusDomainRequest request,
            [FromServices] IStatusDomainAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(id, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDomains", "Update")
        .WithName("UpdateStatusDomain")
        .WithSummary("Swagger.Endpoint.StatusDomain.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateStatusDomainRequest>();

        groupV1.MapPatch("/{id}/activate", async (
            [FromRoute] int id,
            [FromServices] IStatusDomainAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            await appService.ActivateAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDomains", "Activate")
        .WithName("ActivateStatusDomain")
        .WithSummary("Swagger.Endpoint.StatusDomain.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{id}/deactivate", async (
            [FromRoute] int id,
            [FromServices] IStatusDomainAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            await appService.DeactivateAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDomains", "Deactivate")
        .WithName("DeactivateStatusDomain")
        .WithSummary("Swagger.Endpoint.StatusDomain.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{id}", async (
            [FromRoute] int id,
            [FromServices] IStatusDomainAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            await appService.DeleteAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDomains", "Delete")
        .WithName("DeleteStatusDomain")
        .WithSummary("Swagger.Endpoint.StatusDomain.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{id}/translations", async (
            [FromRoute] int id,
            [FromBody] CreateStatusDomainTranslationRequest request,
            [FromServices] IStatusDomainAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var translationId = await appService.CreateTranslationAsync(id, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = translationId }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDomains", "CreateTranslation")
        .WithName("CreateStatusDomainTranslation")
        .WithSummary("Swagger.Endpoint.StatusDomain.CreateTranslation.Summary")
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
            [FromBody] UpdateStatusDomainTranslationRequest request,
            [FromServices] IStatusDomainAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var updated = await appService.UpdateTranslationAsync(id, translationId, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDomains", "UpdateTranslation")
        .WithName("UpdateStatusDomainTranslation")
        .WithSummary("Swagger.Endpoint.StatusDomain.UpdateTranslation.Summary")
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
            [FromServices] IStatusDomainAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            await appService.DeleteTranslationAsync(id, translationId, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "StatusDomains", "DeleteTranslation")
        .WithName("DeleteStatusDomainTranslation")
        .WithSummary("Swagger.Endpoint.StatusDomain.DeleteTranslation.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
