using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientDocument;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientDocument;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

[EndpointMapper]
public static class ClientDocumentEndpoint
{
    public static void MapClientDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/clients").WithTags("ClientDocuments").WithGroupName("v1").RequireAuthorization();
        group.MapGet("/{clientId}/documents", async (int clientId, [FromServices] IClientDocumentAppService service, [FromServices] INotify notify, CancellationToken ct) => notify.CustomResponse(await service.GetAllAsync(clientId, ct), 200)).CustomAuthorize("Admin,BackOffice,Manager", "ClientDocuments", "GetAll").Produces<IEnumerable<ClientDocumentResponse>>(200);
        group.MapGet("/{clientId}/documents/{id}", async (int clientId, int id, [FromServices] IClientDocumentAppService service, [FromServices] INotify notify, CancellationToken ct) => notify.CustomResponse(await service.GetByIdAsync(clientId, id, ct), 200)).CustomAuthorize("Admin,BackOffice,Manager", "ClientDocuments", "GetBy").Produces<ClientDocumentDetailResponse>(200).Produces<ErrorResponse>(410);
        group.MapGet("/{clientId}/documents/paged", async (int clientId, [AsParameters] PagedFilterRequest request, [FromServices] IClientDocumentAppService service, [FromServices] INotify notify, CancellationToken ct) => notify.CustomResponse(await service.GetPagedAsync(clientId, request, ct), 200)).CustomAuthorize("Admin,BackOffice,Manager", "ClientDocuments", "GetPaged").Produces<ListPageResponse<ClientDocumentResponse>>(200);
        group.MapPost("/{clientId}/documents", async (int clientId, CreateClientDocumentRequest request, [FromServices] IClientDocumentAppService service, [FromServices] INotify notify, CancellationToken ct) => notify.CustomResponse(new GenericResponse { Id = await service.CreateAsync(clientId, request, ct) }, 201)).CustomAuthorize("Admin,BackOffice,Manager", "ClientDocuments", "Create").WithValidation<CreateClientDocumentRequest>().Produces<GenericResponse>(201);
        group.MapPut("/{clientId}/documents/{id}", async (int clientId, int id, UpdateClientDocumentRequest request, [FromServices] IClientDocumentAppService service, [FromServices] INotify notify, CancellationToken ct) => notify.CustomResponse(await service.UpdateAsync(clientId, id, request, ct), 200)).CustomAuthorize("Admin,BackOffice,Manager", "ClientDocuments", "Update").WithValidation<UpdateClientDocumentRequest>().Produces<bool>(200);
        group.MapPatch("/{clientId}/documents/{id}/activate", async (int clientId, int id, [FromServices] IClientDocumentAppService service, [FromServices] INotify notify, CancellationToken ct) => { await service.ActivateAsync(clientId, id, ct); return notify.CustomResponse(); }).CustomAuthorize("Admin,BackOffice,Manager", "ClientDocuments", "Activate");
        group.MapPatch("/{clientId}/documents/{id}/deactivate", async (int clientId, int id, [FromServices] IClientDocumentAppService service, [FromServices] INotify notify, CancellationToken ct) => { await service.DeactivateAsync(clientId, id, ct); return notify.CustomResponse(); }).CustomAuthorize("Admin,BackOffice,Manager", "ClientDocuments", "Deactivate");
        group.MapDelete("/{clientId}/documents/{id}", async (int clientId, int id, [FromServices] IClientDocumentAppService service, [FromServices] INotify notify, CancellationToken ct) => { await service.DeleteAsync(clientId, id, ct); return notify.CustomResponse(); }).CustomAuthorize("Admin,BackOffice,Manager", "ClientDocuments", "Delete");
    }
}
