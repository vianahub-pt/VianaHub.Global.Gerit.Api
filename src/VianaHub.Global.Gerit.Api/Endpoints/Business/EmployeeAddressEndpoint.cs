using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeAddress;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.EmployeeAddress;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

[EndpointMapper]
public static class EmployeeAddressEndpoint
{
    public static void MapEmployeeAddressEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/employees").WithTags("EmployeeAddresses").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/{employeeId}/addresses", async ([FromRoute] int employeeId, [FromServices] IEmployeeAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(employeeId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeAddresses", "GetAll")
        .WithName("GetEmployeeAddresses")
        .WithSummary("Swagger.Endpoint.EmployeeAddress.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<EmployeeAddressResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{employeeId}/addresses/{id}", async ([FromRoute] int employeeId, [FromRoute] int id, [FromServices] IEmployeeAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(employeeId, id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeAddresses", "GetBy")
        .WithName("GetEmployeeAddressById")
        .WithSummary("Swagger.Endpoint.EmployeeAddress.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<EmployeeAddressDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{employeeId}/addresses/paged", async ([FromRoute] int employeeId, [AsParameters] PagedFilterRequest request, [FromServices] IEmployeeAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(employeeId, request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeAddresses", "GetPaged")
        .WithName("GetEmployeeAddressesPaged")
        .WithSummary("Swagger.Endpoint.EmployeeAddress.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<EmployeeAddressResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{employeeId}/addresses", async ([FromRoute] int employeeId, [FromBody] CreateEmployeeAddressRequest request, [FromServices] IEmployeeAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(employeeId, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeAddresses", "Create")
        .WithName("CreateEmployeeAddress")
        .WithSummary("Swagger.Endpoint.EmployeeAddress.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateEmployeeAddressRequest>();

        groupV1.MapPut("/{employeeId}/addresses/{id}", async ([FromRoute] int employeeId, [FromRoute] int id, [FromBody] UpdateEmployeeAddressRequest request, [FromServices] IEmployeeAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(employeeId, id, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeAddresses", "Update")
        .WithName("UpdateEmployeeAddress")
        .WithSummary("Swagger.Endpoint.EmployeeAddress.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateEmployeeAddressRequest>();

        groupV1.MapPatch("/{employeeId}/addresses/{id}/activate", async ([FromRoute] int employeeId, [FromRoute] int id, [FromServices] IEmployeeAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(employeeId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeAddresses", "Activate")
        .WithName("ActivateEmployeeAddress")
        .WithSummary("Swagger.Endpoint.EmployeeAddress.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{employeeId}/addresses/{id}/deactivate", async ([FromRoute] int employeeId, [FromRoute] int id, [FromServices] IEmployeeAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(employeeId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeAddresses", "Deactivate")
        .WithName("DeactivateEmployeeAddress")
        .WithSummary("Swagger.Endpoint.EmployeeAddress.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{employeeId}/addresses/{id}", async ([FromRoute] int employeeId, [FromRoute] int id, [FromServices] IEmployeeAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(employeeId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeAddresses", "Delete")
        .WithName("DeleteEmployeeAddress")
        .WithSummary("Swagger.Endpoint.EmployeeAddress.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{employeeId}/addresses/bulk-upload", async ([FromRoute] int employeeId, HttpRequest request, [FromServices] IEmployeeAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
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
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeAddresses", "BulkUpload")
        .WithName("BulkUploadEmployeeAddresses")
        .WithSummary("Swagger.Endpoint.EmployeeAddress.BulkUpload.Summary")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
