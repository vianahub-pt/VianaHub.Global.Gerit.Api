using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamEmployee;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeamEmployee;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

[EndpointMapper]
public static class VisitTeamEmployeeEndpoint
{
    public static void MapVisitTeamEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/visit-teams").WithTags("VisitTeamEmployees").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/{visitTeamId}/employees", async ([FromRoute] int visitTeamId, [FromServices] IVisitTeamEmployeeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(visitTeamId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEmployees", "GetAll")
        .WithName("GetVisitTeamEmployees")
        .WithSummary("Swagger.Endpoint.VisitTeamEmployee.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<VisitTeamEmployeeResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{visitTeamId}/employees/{id}", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromServices] IVisitTeamEmployeeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(visitTeamId, id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEmployees", "GetBy")
        .WithName("GetVisitTeamEmployeeById")
        .WithSummary("Swagger.Endpoint.VisitTeamEmployee.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<VisitTeamEmployeeDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{visitTeamId}/employees/active", async ([FromRoute] int visitTeamId, [FromServices] IVisitTeamEmployeeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetActiveAsync(visitTeamId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEmployees", "GetActive")
        .WithName("GetActiveVisitTeamEmployees")
        .WithSummary("Swagger.Endpoint.VisitTeamEmployee.GetActiveByVisitTeamId.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<VisitTeamEmployeeResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{visitTeamId}/employees/paged", async ([FromRoute] int visitTeamId, [AsParameters] PagedFilterRequest request, [FromServices] IVisitTeamEmployeeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(visitTeamId, request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEmployees", "GetPaged")
        .WithName("GetVisitTeamEmployeesPaged")
        .WithSummary("Swagger.Endpoint.VisitTeamEmployee.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<VisitTeamEmployeeResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{visitTeamId}/employees", async ([FromRoute] int visitTeamId, [FromBody] CreateVisitTeamEmployeeRequest request, [FromServices] IVisitTeamEmployeeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(visitTeamId, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEmployees", "Create")
        .WithName("CreateVisitTeamEmployee")
        .WithSummary("Swagger.Endpoint.VisitTeamEmployee.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateVisitTeamEmployeeRequest>();

        groupV1.MapPut("/{visitTeamId}/employees/{id}", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromBody] UpdateVisitTeamEmployeeRequest request, [FromServices] IVisitTeamEmployeeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(visitTeamId, id, request, ct);
            return notify.CustomResponse(updated ? 204 : 400);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEmployees", "Update")
        .WithName("UpdateVisitTeamEmployee")
        .WithSummary("Swagger.Endpoint.VisitTeamEmployee.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateVisitTeamEmployeeRequest>();

        groupV1.MapPatch("/{visitTeamId}/employees/{id}/activate", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromServices] IVisitTeamEmployeeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(visitTeamId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEmployees", "Activate")
        .WithName("ActivateVisitTeamEmployee")
        .WithSummary("Swagger.Endpoint.VisitTeamEmployee.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{visitTeamId}/employees/{id}/deactivate", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromServices] IVisitTeamEmployeeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(visitTeamId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEmployees", "Deactivate")
        .WithName("DeactivateVisitTeamEmployee")
        .WithSummary("Swagger.Endpoint.VisitTeamEmployee.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{visitTeamId}/employees/{id}", async ([FromRoute] int visitTeamId, [FromRoute] int id, [FromServices] IVisitTeamEmployeeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(visitTeamId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEmployees", "Delete")
        .WithName("DeleteVisitTeamEmployee")
        .WithSummary("Swagger.Endpoint.VisitTeamEmployee.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        // Upload massivo de visit team employees via CSV
        groupV1.MapPost("/{visitTeamId}/employees/bulk-upload", async ([FromRoute] int visitTeamId, HttpRequest request, [FromServices] IVisitTeamEmployeeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                notify.Add("Api.Upload.NoFileProvided", 400);
                return notify.CustomResponse();
            }

            var file = request.Form.Files[0];
            var success = await appService.BulkUploadAsync(visitTeamId, file, ct);
            return notify.CustomResponse(success);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "VisitTeamEmployees", "BulkUpload")
        .WithName("BulkUploadVisitTeamEmployees")
        .WithSummary("Swagger.Endpoint.VisitTeamEmployee.BulkUpload.Summary")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Produces<ErrorResponse>(StatusCodes.Status501NotImplemented)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
