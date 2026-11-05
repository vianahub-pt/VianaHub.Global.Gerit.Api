using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.TenantAddress;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.TenantAddress;
using VianaHub.Global.Gerit.Application.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Billing;

[EndpointMapper]
public static class TenantAddressEndpoint
{
    public static void MapTenantAddressEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/tenants").WithTags("TenantAddress").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/{tenantId}/addresses", async ([FromRoute] int tenantId, [FromServices] ITenantAddressesAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByTenantIdAsync(tenantId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantAddress", "GetAll")
        .WithName("GetTenantAddresses")
        .WithSummary("Swagger.Endpoint.TenantAddress.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<TenantAddressResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{tenantId}/addresses/{id}", async ([FromRoute] int tenantId, [FromRoute] int id, [FromServices] ITenantAddressesAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(tenantId, id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantAddress", "GetBy")
        .WithName("GetTenantAddressById")
        .WithSummary("Swagger.Endpoint.TenantAddress.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<TenantAddressDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{tenantId}/addresses/paged", async ([FromRoute] int tenantId, [AsParameters] PagedFilterRequest request, [FromServices] ITenantAddressesAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(tenantId, request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantAddress", "GetPaged")
        .WithName("GetTenantAddressesPaged")
        .WithSummary("Swagger.Endpoint.TenantAddress.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<TenantAddressResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{tenantId}/addresses", async ([FromRoute] int tenantId, [FromBody] CreateTenantAddressRequest request, [FromServices] ITenantAddressesAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(tenantId, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantAddress", "Create")
        .WithName("CreateTenantAddress")
        .WithSummary("Swagger.Endpoint.TenantAddress.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateTenantAddressRequest>();

        groupV1.MapPut("/{tenantId}/addresses/{id}", async ([FromRoute] int tenantId, [FromRoute] int id, [FromBody] UpdateTenantAddressRequest request, [FromServices] ITenantAddressesAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(tenantId, id, request, ct);
            return notify.CustomResponse(updated, 204);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantAddress", "Update")
        .WithName("UpdateTenantAddress")
        .WithSummary("Swagger.Endpoint.TenantAddress.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateTenantAddressRequest>();

        groupV1.MapPatch("/{tenantId}/addresses/{id}/activate", async ([FromRoute] int tenantId, [FromRoute] int id, [FromServices] ITenantAddressesAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(tenantId, id, ct);
            return notify.CustomResponse(ok, 204);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantAddress", "Activate")
        .WithName("ActivateTenantAddress")
        .WithSummary("Swagger.Endpoint.TenantAddress.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{tenantId}/addresses/{id}/deactivate", async ([FromRoute] int tenantId, [FromRoute] int id, [FromServices] ITenantAddressesAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(tenantId, id, ct);
            return notify.CustomResponse(ok, 204);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantAddress", "Deactivate")
        .WithName("DeactivateTenantAddress")
        .WithSummary("Swagger.Endpoint.TenantAddress.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{tenantId}/addresses/{id}", async ([FromRoute] int tenantId, [FromRoute] int id, [FromServices] ITenantAddressesAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(tenantId, id, ct);
            return notify.CustomResponse(ok, 204);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantAddress", "Delete")
        .WithName("DeleteTenantAddress")
        .WithSummary("Swagger.Endpoint.TenantAddress.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
