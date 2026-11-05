using VianaHub.Global.Gerit.Application.Dtos.Response.Business.PartyType;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.PartyType;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

/// <summary>
/// Endpoints para gestão de Tipos de Party (CRUD + traduções).
/// </summary>
[EndpointMapper]
public static class PartyTypeEndpoint
{
    public static void MapPartyTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/party-types")
            .WithTags("PartyTypes")
            .WithGroupName("v1")
            .RequireAuthorization();

        // ─── CRUD principal ────────────────────────────────────────────────

        groupV1.MapGet("/", async (
            [FromServices] IPartyTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "PartyTypes", "GetAll")
        .WithName("GetPartyTypes")
        .WithSummary("Swagger.Endpoint.PartyType.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<PartyTypeResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{id}", async (
            [FromRoute] byte id,
            [FromServices] IPartyTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "PartyTypes", "GetBy")
        .WithName("GetPartyTypeById")
        .WithSummary("Swagger.Endpoint.PartyType.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<PartyTypeDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/paged", async (
            [AsParameters] PagedFilterRequest request,
            [FromServices] IPartyTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "PartyTypes", "GetPaged")
        .WithName("GetPartyTypesPaged")
        .WithSummary("Swagger.Endpoint.PartyType.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<PartyTypeResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/", async (
            [FromBody] CreatePartyTypeRequest request,
            [FromServices] IPartyTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "PartyTypes", "Create")
        .WithName("CreatePartyType")
        .WithSummary("Swagger.Endpoint.PartyType.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreatePartyTypeRequest>();

        groupV1.MapPut("/{id}", async (
            [FromRoute] byte id,
            [FromBody] UpdatePartyTypeRequest request,
            [FromServices] IPartyTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(id, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "PartyTypes", "Update")
        .WithName("UpdatePartyType")
        .WithSummary("Swagger.Endpoint.PartyType.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdatePartyTypeRequest>();

        groupV1.MapPatch("/{id}/activate", async (
            [FromRoute] byte id,
            [FromServices] IPartyTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            await appService.ActivateAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "PartyTypes", "Activate")
        .WithName("ActivatePartyType")
        .WithSummary("Swagger.Endpoint.PartyType.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{id}/deactivate", async (
            [FromRoute] byte id,
            [FromServices] IPartyTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            await appService.DeactivateAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "PartyTypes", "Deactivate")
        .WithName("DeactivatePartyType")
        .WithSummary("Swagger.Endpoint.PartyType.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{id}", async (
            [FromRoute] byte id,
            [FromServices] IPartyTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            await appService.DeleteAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "PartyTypes", "Delete")
        .WithName("DeletePartyType")
        .WithSummary("Swagger.Endpoint.PartyType.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{id}/translations", async (
            [FromRoute] byte id,
            [FromBody] CreatePartyTypeTranslationRequest request,
            [FromServices] IPartyTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var translationId = await appService.CreateTranslationAsync(id, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = translationId }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "PartyTypes", "CreateTranslation")
        .WithName("CreatePartyTypeTranslation")
        .WithSummary("Swagger.Endpoint.PartyType.CreateTranslation.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPut("/{id}/translations/{translationId}", async (
            [FromRoute] byte id,
            [FromRoute] int translationId,
            [FromBody] UpdatePartyTypeTranslationRequest request,
            [FromServices] IPartyTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var updated = await appService.UpdateTranslationAsync(id, translationId, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "PartyTypes", "UpdateTranslation")
        .WithName("UpdatePartyTypeTranslation")
        .WithSummary("Swagger.Endpoint.PartyType.UpdateTranslation.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{id}/translations/{translationId}", async (
            [FromRoute] byte id,
            [FromRoute] int translationId,
            [FromServices] IPartyTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            await appService.DeleteTranslationAsync(id, translationId, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "PartyTypes", "DeleteTranslation")
        .WithName("DeletePartyTypeTranslation")
        .WithSummary("Swagger.Endpoint.PartyType.DeleteTranslation.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
