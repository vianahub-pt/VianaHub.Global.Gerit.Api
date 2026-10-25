using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.TenantContactPerson;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.TenantContactPerson;
using VianaHub.Global.Gerit.Application.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Billing;

[EndpointMapper]
public static class TenantContactPersonEndpoint
{
    public static void MapTenantContactPersonEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/tenants").WithTags("TenantContactPersons").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/{tenantId}/contacts", async ([FromRoute] int tenantId, [FromServices] ITenantContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByTenantIdAsync(tenantId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantContactPersons", "GetAll")
        .WithName("GetTenantContacts")
        .WithSummary("Swagger.Endpoint.TenantContactPerson.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<TenantContactPersonResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{tenantId}/contacts/{id}", async ([FromRoute] int tenantId, [FromRoute] int id, [FromServices] ITenantContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantContactPersons", "GetBy")
        .WithName("GetTenantContactById")
        .WithSummary("Swagger.Endpoint.TenantContactPerson.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<TenantContactPersonDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{tenantId}/contacts/primary", async ([FromRoute] int tenantId, [FromServices] ITenantContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPrimaryByTenantIdAsync(tenantId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantContactPersons", "GetPrimary")
        .WithName("GetPrimaryTenantContact")
        .WithSummary("Swagger.Endpoint.TenantContactPerson.GetPrimary.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<TenantContactPersonResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{tenantId}/contacts/paged", async ([FromRoute] int tenantId, [AsParameters] PagedFilterRequest request, [FromServices] ITenantContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantContactPersons", "GetPaged")
        .WithName("GetTenantContactsPaged")
        .WithSummary("Swagger.Endpoint.TenantContactPerson.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<TenantContactPersonResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{tenantId}/contacts", async ([FromRoute] int tenantId, [FromBody] CreateTenantContactPersonRequest request, [FromServices] ITenantContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(tenantId, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantContactPersons", "Create")
        .WithName("CreateTenantContact")
        .WithSummary("Swagger.Endpoint.TenantContactPerson.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateTenantContactPersonRequest>();

        groupV1.MapPut("/{tenantId}/contacts/{id}", async ([FromRoute] int tenantId, [FromRoute] int id, [FromBody] UpdateTenantContactPersonRequest request, [FromServices] ITenantContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(tenantId, id, request, ct);
            return notify.CustomResponse(updated, 204);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantContactPersons", "Update")
        .WithName("UpdateTenantContact")
        .WithSummary("Swagger.Endpoint.TenantContactPerson.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateTenantContactPersonRequest>();

        groupV1.MapPatch("/{tenantId}/contacts/{id}/set-primary", async ([FromRoute] int tenantId, [FromRoute] int id, [FromServices] ITenantContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.SetAsPrimaryAsync(tenantId, id, ct);
            return notify.CustomResponse(ok, 204);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantContactPersons", "SetAsPrimary")
        .WithName("SetPrimaryTenantContact")
        .WithSummary("Swagger.Endpoint.TenantContactPerson.SetAsPrimary.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{tenantId}/contacts/{id}/activate", async ([FromRoute] int tenantId, [FromRoute] int id, [FromServices] ITenantContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(tenantId, id, ct);
            return notify.CustomResponse(ok, 204);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantContactPersons", "Activate")
        .WithName("ActivateTenantContact")
        .WithSummary("Swagger.Endpoint.TenantContactPerson.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{tenantId}/contacts/{id}/deactivate", async ([FromRoute] int tenantId, [FromRoute] int id, [FromServices] ITenantContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(tenantId, id, ct);
            return notify.CustomResponse(ok, 204);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantContactPersons", "Deactivate")
        .WithName("DeactivateTenantContact")
        .WithSummary("Swagger.Endpoint.TenantContactPerson.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{tenantId}/contacts/{id}", async ([FromRoute] int tenantId, [FromRoute] int id, [FromServices] ITenantContactPersonAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(tenantId, id, ct);
            return notify.CustomResponse(ok, 204);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantContactPersons", "Delete")
        .WithName("DeleteTenantContact")
        .WithSummary("Swagger.Endpoint.TenantContactPerson.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
