using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientFiscalData;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientFiscalData;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

[EndpointMapper]
public static class ClientFiscalDataEndpoint
{
    public static void MapClientFiscalDataEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/clients").WithTags("ClientFiscalData").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/{clientId}/fiscal-data", async ([FromRoute] int clientId, [FromServices] IClientFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(clientId, ct);
            return notify.CustomResponse(response);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientFiscalData", "GetAll")
        .WithName("GetClientFiscalData")
        .WithSummary("Swagger.Endpoint.ClientFiscalData.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<ClientFiscalDataResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{clientId}/fiscal-data/{id}", async ([FromRoute] int clientId, [FromRoute] int id, [FromServices] IClientFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(clientId, id, ct);
            return notify.CustomResponse(response);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientFiscalData", "GetBy")
        .WithName("GetClientFiscalDataById")
        .WithSummary("Swagger.Endpoint.ClientFiscalData.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ClientFiscalDataDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{clientId}/fiscal-data/paged", async ([FromRoute] int clientId, [AsParameters] PagedFilterRequest request, [FromServices] IClientFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(clientId, request, ct);
            return notify.CustomResponse(response);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientFiscalData", "GetPaged")
        .WithName("GetClientFiscalDataPaged")
        .WithSummary("Swagger.Endpoint.ClientFiscalData.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<ClientFiscalDataResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{clientId}/fiscal-data", async ([FromRoute] int clientId, [FromBody] CreateClientFiscalDataRequest request, [FromServices] IClientFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(clientId, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientFiscalData", "Create")
        .WithName("CreateClientFiscalData")
        .WithSummary("Swagger.Endpoint.ClientFiscalData.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateClientFiscalDataRequest>();

        groupV1.MapPut("/{clientId}/fiscal-data/{id}", async ([FromRoute] int clientId, [FromRoute] int id, [FromBody] UpdateClientFiscalDataRequest request, [FromServices] IClientFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(clientId, id, request, ct);
            return notify.CustomResponse(updated);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientFiscalData", "Update")
        .WithName("UpdateClientFiscalData")
        .WithSummary("Swagger.Endpoint.ClientFiscalData.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateClientFiscalDataRequest>();

        groupV1.MapPatch("/{clientId}/fiscal-data/{id}/activate", async ([FromRoute] int clientId, [FromRoute] int id, [FromServices] IClientFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(clientId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientFiscalData", "Activate")
        .WithName("ActivateClientFiscalData")
        .WithSummary("Swagger.Endpoint.ClientFiscalData.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{clientId}/fiscal-data/{id}/deactivate", async ([FromRoute] int clientId, [FromRoute] int id, [FromServices] IClientFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(clientId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientFiscalData", "Deactivate")
        .WithName("DeactivateClientFiscalData")
        .WithSummary("Swagger.Endpoint.ClientFiscalData.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{clientId}/fiscal-data/{id}", async ([FromRoute] int clientId, [FromRoute] int id, [FromServices] IClientFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(clientId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientFiscalData", "Delete")
        .WithName("DeleteClientFiscalData")
        .WithSummary("Swagger.Endpoint.ClientFiscalData.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        // Upload massivo de client fiscal data via CSV
        groupV1.MapPost("/{clientId}/fiscal-data/bulk-upload", async ([FromRoute] int clientId, HttpRequest request, [FromServices] IClientFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                notify.Add("Api.Upload.NoFileProvided", 400);
                return notify.CustomResponse();
            }

            var file = request.Form.Files[0];
            var success = await appService.BulkUploadAsync(clientId, file, ct);
            return notify.CustomResponse(success);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientFiscalData", "BulkUpload")
        .WithName("BulkUploadClientFiscalData")
        .WithSummary("Swagger.Endpoint.ClientFiscalData.BulkUpload.Summary")
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
