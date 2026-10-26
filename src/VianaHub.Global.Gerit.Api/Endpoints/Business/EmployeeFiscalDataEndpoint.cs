using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeFiscalData;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.EmployeeFiscalData;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

[EndpointMapper]
public static class EmployeeFiscalDataEndpoint
{
    public static void MapEmployeeFiscalDataEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/employees").WithTags("EmployeeFiscalData").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/{employeeId}/fiscal-data", async ([FromRoute] int employeeId, [FromServices] IEmployeeFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(employeeId, ct);
            return notify.CustomResponse(response);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeFiscalData", "GetAll")
        .WithName("GetEmployeeFiscalData")
        .WithSummary("Swagger.Endpoint.EmployeeFiscalData.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<EmployeeFiscalDataResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{employeeId}/fiscal-data/{id}", async ([FromRoute] int employeeId, [FromRoute] int id, [FromServices] IEmployeeFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(employeeId, id, ct);
            return notify.CustomResponse(response);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeFiscalData", "GetBy")
        .WithName("GetEmployeeFiscalDataById")
        .WithSummary("Swagger.Endpoint.EmployeeFiscalData.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<EmployeeFiscalDataDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{employeeId}/fiscal-data/paged", async ([FromRoute] int employeeId, [AsParameters] PagedFilterRequest request, [FromServices] IEmployeeFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(employeeId, request, ct);
            return notify.CustomResponse(response);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeFiscalData", "GetPaged")
        .WithName("GetEmployeeFiscalDataPaged")
        .WithSummary("Swagger.Endpoint.EmployeeFiscalData.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<EmployeeFiscalDataResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{employeeId}/fiscal-data/", async ([FromRoute] int employeeId, [FromBody] CreateEmployeeFiscalDataRequest request, [FromServices] IEmployeeFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(employeeId, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeFiscalData", "Create")
        .WithName("CreateEmployeeFiscalData")
        .WithSummary("Swagger.Endpoint.EmployeeFiscalData.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateEmployeeFiscalDataRequest>();

        groupV1.MapPut("/{employeeId}/fiscal-data/{id}", async ([FromRoute] int employeeId, [FromRoute] int id, [FromBody] UpdateEmployeeFiscalDataRequest request, [FromServices] IEmployeeFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(employeeId, id, request, ct);
            return notify.CustomResponse(updated);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeFiscalData", "Update")
        .WithName("UpdateEmployeeFiscalData")
        .WithSummary("Swagger.Endpoint.EmployeeFiscalData.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateEmployeeFiscalDataRequest>();

        groupV1.MapPatch("/{employeeId}/fiscal-data/{id}/activate", async ([FromRoute] int employeeId, [FromRoute] int id, [FromServices] IEmployeeFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(employeeId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeFiscalData", "Activate")
        .WithName("ActivateEmployeeFiscalData")
        .WithSummary("Swagger.Endpoint.EmployeeFiscalData.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{employeeId}/fiscal-data/{id}/deactivate", async ([FromRoute] int employeeId, [FromRoute] int id, [FromServices] IEmployeeFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(employeeId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeFiscalData", "Deactivate")
        .WithName("DeactivateEmployeeFiscalData")
        .WithSummary("Swagger.Endpoint.EmployeeFiscalData.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{employeeId}/fiscal-data/{id}", async ([FromRoute] int employeeId, [FromRoute] int id, [FromServices] IEmployeeFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(employeeId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeFiscalData", "Delete")
        .WithName("DeleteEmployeeFiscalData")
        .WithSummary("Swagger.Endpoint.EmployeeFiscalData.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        // Upload massivo de employee fiscal data via CSV
        groupV1.MapPost("/{employeeId}/fiscal-data/bulk-upload", async ([FromRoute] int employeeId, HttpRequest request, [FromServices] IEmployeeFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
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
        .CustomAuthorize("Admin,BackOffice,Manager", "EmployeeFiscalData", "BulkUpload")
        .WithName("BulkUploadEmployeeFiscalData")
        .WithSummary("Swagger.Endpoint.EmployeeFiscalData.BulkUpload.Summary")
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
