using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeContact;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.EmployeeContact;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

[EndpointMapper]
public static class EmployeeContactPersonEndpoint
{
    public static void MapEmployeeContactPersonEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/employees").WithTags("EmployeeContactPersons").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/{employeeId}/contacts", async ([FromRoute] int employeeId, [FromServices] IEmployeeContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(employeeId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeContactPersons", "GetAll")
        .WithName("GetEmployeeContacts")
        .WithSummary("Swagger.Endpoint.EmployeeContactPerson.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<EmployeeContactPersonResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{employeeId}/contacts/{id}", async ([FromRoute] int employeeId, [FromRoute] int id, [FromServices] IEmployeeContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(employeeId, id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeContactPersons", "GetBy")
        .WithName("GetEmployeeContactById")
        .WithSummary("Swagger.Endpoint.EmployeeContactPerson.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<EmployeeContactPersonDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{employeeId}/contacts/paged", async ([FromRoute] int employeeId, [AsParameters] PagedFilterRequest request, [FromServices] IEmployeeContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(employeeId, request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeContactPersons", "GetPaged")
        .WithName("GetEmployeeContactsPaged")
        .WithSummary("Swagger.Endpoint.EmployeeContactPerson.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<EmployeeContactPersonResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{employeeId}/contacts", async ([FromRoute] int employeeId, [FromBody] CreateEmployeeContactPersonRequest request, [FromServices] IEmployeeContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(employeeId, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeContactPersons", "Create")
        .WithName("CreateEmployeeContact")
        .WithSummary("Swagger.Endpoint.EmployeeContactPerson.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateEmployeeContactPersonRequest>();

        groupV1.MapPut("/{employeeId}/contacts/{id}", async ([FromRoute] int employeeId, [FromRoute] int id, [FromBody] UpdateEmployeeContactPersonRequest request, [FromServices] IEmployeeContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(employeeId, id, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeContactPersons", "Update")
        .WithName("UpdateEmployeeContact")
        .WithSummary("Swagger.Endpoint.EmployeeContactPerson.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateEmployeeContactPersonRequest>();

        groupV1.MapPatch("/{employeeId}/contacts/{id}/activate", async ([FromRoute] int employeeId, [FromRoute] int id, [FromServices] IEmployeeContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(employeeId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeContactPersons", "Activate")
        .WithName("ActivateEmployeeContact")
        .WithSummary("Swagger.Endpoint.EmployeeContactPerson.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{employeeId}/contacts/{id}/deactivate", async ([FromRoute] int employeeId, [FromRoute] int id, [FromServices] IEmployeeContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(employeeId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeContactPersons", "Deactivate")
        .WithName("DeactivateEmployeeContact")
        .WithSummary("Swagger.Endpoint.EmployeeContactPerson.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{employeeId}/contacts/{id}", async ([FromRoute] int employeeId, [FromRoute] int id, [FromServices] IEmployeeContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(employeeId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeContactPersons", "Delete")
        .WithName("DeleteEmployeeContact")
        .WithSummary("Swagger.Endpoint.EmployeeContactPerson.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{employeeId}/contacts/bulk-upload", async ([FromRoute] int employeeId, HttpRequest request, [FromServices] IEmployeeContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
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
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeContactPersons", "BulkUpload")
        .WithName("BulkUploadEmployeeContacts")
        .WithSummary("Swagger.Endpoint.EmployeeContactPerson.BulkUpload.Summary")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
