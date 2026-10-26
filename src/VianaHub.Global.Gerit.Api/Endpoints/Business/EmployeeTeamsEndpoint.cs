using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeTeams;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.EmployeeTeams;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

[EndpointMapper]
public static class EmployeeTeamsEndpoint
{
    public static void MapEmployeeTeamsEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/employees").WithTags("EmployeeTeams").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/{employeeId}/teams", async ([FromRoute] int employeeId, [FromServices] IEmployeeTeamsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(employeeId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeTeam", "GetAll")
        .WithName("GetEmployeeTeams")
        .WithSummary("Swagger.Endpoint.EmployeeTeams.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<EmployeeTeamResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{employeeId}/teams/{id}", async ([FromRoute] int employeeId, [FromRoute] int id, [FromServices] IEmployeeTeamsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(employeeId, id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeTeam", "GetBy")
        .WithName("GetEmployeeTeamById")
        .WithSummary("Swagger.Endpoint.EmployeeTeams.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<EmployeeTeamDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{employeeId}/teams/paged", async ([FromRoute] int employeeId, [AsParameters] PagedFilterRequest request, [FromServices] IEmployeeTeamsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(employeeId, request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeTeam", "GetPaged")
        .WithName("GetEmployeeTeamsPaged")
        .WithSummary("Swagger.Endpoint.EmployeeTeams.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<EmployeeTeamResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{employeeId}/teams", async ([FromRoute] int employeeId, [FromBody] CreateEmployeeTeamRequest request, [FromServices] IEmployeeTeamsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(employeeId, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeTeam", "Create")
        .WithName("CreateEmployeeTeam")
        .WithSummary("Swagger.Endpoint.EmployeeTeams.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateEmployeeTeamRequest>();

        groupV1.MapPut("/{employeeId}/teams/{id}", async ([FromRoute] int employeeId, [FromRoute] int id, [FromBody] UpdateEmployeeTeamRequest request, [FromServices] IEmployeeTeamsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(employeeId, id, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeTeam", "Update")
        .WithName("UpdateEmployeeTeam")
        .WithSummary("Swagger.Endpoint.EmployeeTeams.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateEmployeeTeamRequest>();

        groupV1.MapPatch("/{employeeId}/teams/{id}/activate", async ([FromRoute] int employeeId, [FromRoute] int id, [FromServices] IEmployeeTeamsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(employeeId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeTeam", "Activate")
        .WithName("ActivateEmployeeTeam")
        .WithSummary("Swagger.Endpoint.EmployeeTeams.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{employeeId}/teams/{id}/deactivate", async ([FromRoute] int employeeId, [FromRoute] int id, [FromServices] IEmployeeTeamsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(employeeId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeTeam", "Deactivate")
        .WithName("DeactivateEmployeeTeam")
        .WithSummary("Swagger.Endpoint.EmployeeTeams.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{employeeId}/teams/{id}", async ([FromRoute] int employeeId, [FromRoute] int id, [FromServices] IEmployeeTeamsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(employeeId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeTeam", "Delete")
        .WithName("DeleteEmployeeTeam")
        .WithSummary("Swagger.Endpoint.EmployeeTeams.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{employeeId}/teams/bulk-upload", async ([FromRoute] int employeeId, HttpRequest request, [FromServices] IEmployeeTeamsAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                notify.Add("Api.Upload.NoFileProvided", 400);
                return notify.CustomResponse();
            }

            var file = request.Form.Files[0];
            var success = await appService.BulkUploadAsync(employeeId, file, ct);
            return notify.CustomResponse(success);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeTeam", "BulkUpload")
        .WithName("BulkUploadEmployeeTeams")
        .WithSummary("Swagger.Endpoint.EmployeeTeams.BulkUpload.Summary")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
