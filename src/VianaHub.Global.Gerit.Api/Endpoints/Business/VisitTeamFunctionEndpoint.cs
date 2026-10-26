using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamFunction;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Function;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

[EndpointMapper]
public static class VisitTeamFunctionEndpoint
{
    public static void MapVisitTeamFunctionEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/visit-teams").WithTags("VisitTeamFunctions").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/{visitTeamId}/functions", async ([FromRoute] int visitTeamId, [FromServices] IVisitTeamFunctionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(visitTeamId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamFunctions", "GetAll")
        .WithName("GetVisitTeamFunctions")
        .WithSummary("Swagger.Endpoint.VisitTeamFunction.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<VisitTeamFunctionResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{visitTeamId}/functions/{id}", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromServices] IVisitTeamFunctionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(visitTeamId, id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamFunctions", "GetBy")
        .WithName("GetVisitTeamFunctionById")
        .WithSummary("Swagger.Endpoint.VisitTeamFunction.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<VisitTeamFunctionDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{visitTeamId}/functions/paged", async ([FromRoute] int visitTeamId, [AsParameters] PagedFilterRequest request, [FromServices] IVisitTeamFunctionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(visitTeamId, request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamFunctions", "GetPaged")
        .WithName("GetVisitTeamFunctionsPaged")
        .WithSummary("Swagger.Endpoint.VisitTeamFunction.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<VisitTeamFunctionResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{visitTeamId}/functions", async ([FromRoute] int visitTeamId, [FromBody] CreateVisitTeamFunctionRequest request, [FromServices] IVisitTeamFunctionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(visitTeamId, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamFunctions", "Create")
        .WithName("CreateVisitTeamFunction")
        .WithSummary("Swagger.Endpoint.VisitTeamFunction.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateVisitTeamFunctionRequest>();

        groupV1.MapPut("/{visitTeamId}/functions/{id}", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromBody] UpdateVisitTeamFunctionRequest request, [FromServices] IVisitTeamFunctionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(visitTeamId, id, request, ct);
            return notify.CustomResponse(updated ? 204 : 400);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamFunctions", "Update")
        .WithName("UpdateVisitTeamFunction")
        .WithSummary("Swagger.Endpoint.VisitTeamFunction.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateVisitTeamFunctionRequest>();

        groupV1.MapPatch("/{visitTeamId}/functions/{id}/activate", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromServices] IVisitTeamFunctionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(visitTeamId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamFunctions", "Activate")
        .WithName("ActivateVisitTeamFunction")
        .WithSummary("Swagger.Endpoint.VisitTeamFunction.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{visitTeamId}/functions/{id}/deactivate", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromServices] IVisitTeamFunctionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(visitTeamId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamFunctions", "Deactivate")
        .WithName("DeactivateVisitTeamFunction")
        .WithSummary("Swagger.Endpoint.VisitTeamFunction.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{visitTeamId}/functions/{id}", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromServices] IVisitTeamFunctionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(visitTeamId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamFunctions", "Delete")
        .WithName("DeleteVisitTeamFunction")
        .WithSummary("Swagger.Endpoint.VisitTeamFunction.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        // Upload massivo de Functions via CSV
        groupV1.MapPost("/{visitTeamId}/functions/bulk-upload", async ([FromRoute] int visitTeamId, HttpRequest request, [FromServices] IVisitTeamFunctionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                // Utiliza chave de tradução para mensagem
                notify.Add("Api.Upload.NoFileProvided", 400);
                return notify.CustomResponse();
            }

            var file = request.Form.Files[0];
            var success = await appService.BulkUploadAsync(visitTeamId, file, ct);
            return notify.CustomResponse(success);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamFunctions", "BulkUpload")
        .WithName("BulkUploadVisitTeamFunctions")
        .WithSummary("Swagger.Endpoint.VisitTeamFunction.BulkUpload.Summary")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
