using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientAddress;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientAddress;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

/// <summary>
/// Endpoints para ClientAddress
/// </summary>
[EndpointMapper]
public static class ClientAddressEndpoint
{
    public static void MapClientAddressEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/clients").WithTags("ClientAddresses").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/{clientId}/addresses", async ([FromRoute] int clientId, [FromServices] IClientAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(clientId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientAddresses", "GetAll")
        .WithName("GetClientAddresses")
        .WithSummary("Swagger.Endpoint.ClientAddress.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<ClientAddressResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{clientId}/addresses/{id}", async ([FromRoute] int clientId, [FromRoute] int id, [FromServices] IClientAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(clientId, id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientAddresses", "GetBy")
        .WithName("GetClientAddressById")
        .WithSummary("Swagger.Endpoint.ClientAddress.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ClientAddressDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{clientId}/addresses/paged", async ([FromRoute] int clientId, [AsParameters] PagedFilterRequest request, [FromServices] IClientAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(clientId, request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientAddresses", "GetPaged")
        .WithName("GetClientAddressesPaged")
        .WithSummary("Swagger.Endpoint.ClientAddress.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<ClientAddressResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{clientId}/addresses", async ([FromRoute] int clientId, [FromBody] CreateClientAddressRequest request, [FromServices] IClientAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(clientId, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientAddresses", "Create")
        .WithName("CreateClientAddress")
        .WithSummary("Swagger.Endpoint.ClientAddress.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateClientAddressRequest>();

        groupV1.MapPut("/{clientId}/addresses/{id}", async ([FromRoute] int clientId, [FromRoute] int id, [FromBody] UpdateClientAddressRequest request, [FromServices] IClientAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(clientId, id, request, ct);
            return notify.CustomResponse(updated, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientAddresses", "Update")
        .WithName("UpdateClientAddress")
        .WithSummary("Swagger.Endpoint.ClientAddress.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateClientAddressRequest>();

        groupV1.MapPatch("/{clientId}/addresses/{id}/activate", async ([FromRoute] int clientId, [FromRoute] int id, [FromServices] IClientAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(clientId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientAddresses", "Activate")
        .WithName("ActivateClientAddress")
        .WithSummary("Swagger.Endpoint.ClientAddress.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{clientId}/addresses/{id}/deactivate", async ([FromRoute] int clientId, [FromRoute] int id, [FromServices] IClientAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(clientId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientAddresses", "Deactivate")
        .WithName("DeactivateClientAddress")
        .WithSummary("Swagger.Endpoint.ClientAddress.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{clientId}/addresses/{id}", async ([FromRoute] int clientId, [FromRoute] int id, [FromServices] IClientAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(clientId, id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientAddresses", "Delete")
        .WithName("DeleteClientAddress")
        .WithSummary("Swagger.Endpoint.ClientAddress.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{clientId}/addresses/bulk-upload", async ([FromRoute] int clientId, HttpRequest request, [FromServices] IClientAddressAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
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
        .CustomAuthorize("Admin,BackOffice,Manager", "ClientAddresses", "BulkUpload")
        .WithName("BulkUploadClientAddresses")
        .WithSummary("Swagger.Endpoint.ClientAddress.BulkUpload.Summary")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
