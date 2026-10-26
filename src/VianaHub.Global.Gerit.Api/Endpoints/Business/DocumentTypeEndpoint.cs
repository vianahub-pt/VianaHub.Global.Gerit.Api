using VianaHub.Global.Gerit.Application.Dtos.Response.Business.DocumentType;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.DocumentType;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

/// <summary>
/// Endpoints para gestão de Tipos de Documento (CRUD + traduções).
/// </summary>
[EndpointMapper]
public static class DocumentTypeEndpoint
{
    public static void MapDocumentTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/document-types")
            .WithTags("DocumentTypes")
            .WithGroupName("v1")
            .RequireAuthorization();

        // ─── CRUD principal ────────────────────────────────────────────────

        groupV1.MapGet("/", async (
            [FromServices] IDocumentTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "DocumentTypes", "GetAll")
        .WithName("GetDocumentTypes")
        .WithSummary("Swagger.Endpoint.DocumentType.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<DocumentTypeResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{id}", async (
            [FromRoute] int id,
            [FromServices] IDocumentTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "DocumentTypes", "GetBy")
        .WithName("GetDocumentTypeById")
        .WithSummary("Swagger.Endpoint.DocumentType.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<DocumentTypeDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/paged", async (
            [AsParameters] PagedFilterRequest request,
            [FromServices] IDocumentTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "DocumentTypes", "GetPaged")
        .WithName("GetDocumentTypesPaged")
        .WithSummary("Swagger.Endpoint.DocumentType.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<DocumentTypeResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/", async (
            [FromBody] CreateDocumentTypeRequest request,
            [FromServices] IDocumentTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "DocumentTypes", "Create")
        .WithName("CreateDocumentType")
        .WithSummary("Swagger.Endpoint.DocumentType.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateDocumentTypeRequest>();

        groupV1.MapPut("/{id}", async (
            [FromRoute] int id,
            [FromBody] UpdateDocumentTypeRequest request,
            [FromServices] IDocumentTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(id, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "DocumentTypes", "Update")
        .WithName("UpdateDocumentType")
        .WithSummary("Swagger.Endpoint.DocumentType.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateDocumentTypeRequest>();

        groupV1.MapPatch("/{id}/activate", async (
            [FromRoute] int id,
            [FromServices] IDocumentTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            await appService.ActivateAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "DocumentTypes", "Activate")
        .WithName("ActivateDocumentType")
        .WithSummary("Swagger.Endpoint.DocumentType.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{id}/deactivate", async (
            [FromRoute] int id,
            [FromServices] IDocumentTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            await appService.DeactivateAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "DocumentTypes", "Deactivate")
        .WithName("DeactivateDocumentType")
        .WithSummary("Swagger.Endpoint.DocumentType.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{id}", async (
            [FromRoute] int id,
            [FromServices] IDocumentTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            await appService.DeleteAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "DocumentTypes", "Delete")
        .WithName("DeleteDocumentType")
        .WithSummary("Swagger.Endpoint.DocumentType.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{id}/translations", async (
            [FromRoute] int id,
            [FromBody] CreateDocumentTypeTranslationRequest request,
            [FromServices] IDocumentTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var translationId = await appService.CreateTranslationAsync(id, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = translationId }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "DocumentTypes", "CreateTranslation")
        .WithName("CreateDocumentTypeTranslation")
        .WithSummary("Swagger.Endpoint.DocumentType.CreateTranslation.Summary")
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
            [FromBody] UpdateDocumentTypeTranslationRequest request,
            [FromServices] IDocumentTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            var updated = await appService.UpdateTranslationAsync(id, translationId, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "DocumentTypes", "UpdateTranslation")
        .WithName("UpdateDocumentTypeTranslation")
        .WithSummary("Swagger.Endpoint.DocumentType.UpdateTranslation.Summary")
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
            [FromServices] IDocumentTypeAppService appService,
            [FromServices] INotify notify,
            CancellationToken ct) =>
        {
            await appService.DeleteTranslationAsync(id, translationId, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "DocumentTypes", "DeleteTranslation")
        .WithName("DeleteDocumentTypeTranslation")
        .WithSummary("Swagger.Endpoint.DocumentType.DeleteTranslation.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
