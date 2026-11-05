using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.TenantFiscalData;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.TenantFiscalData;
using VianaHub.Global.Gerit.Application.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Billing;

[EndpointMapper]
public static class TenantFiscalDataEndpoint
{
    public static void MapTenantFiscalDataEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/tenants").WithTags("TenantFiscalData").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/{tenantId}/fiscal-data", async ([FromRoute] int tenantId, [FromServices] ITenantFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByTenantIdAsync(tenantId, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantFiscalData", "GetAll")
        .WithName("GetTenantFiscalData")
        .WithSummary("Swagger.Endpoint.TenantFiscalData.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<TenantFiscalDataResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{tenantId}/fiscal-data/{id}", async ([FromRoute] int tenantId, [FromRoute] int id, [FromServices] ITenantFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(tenantId, id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantFiscalData", "GetBy")
        .WithName("GetTenantFiscalDataById")
        .WithSummary("Swagger.Endpoint.TenantFiscalData.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<TenantFiscalDataDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{tenantId}/fiscal-data/paged", async ([FromRoute] int tenantId, [AsParameters] PagedFilterRequest request, [FromServices] ITenantFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(tenantId, request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantFiscalData", "GetPaged")
        .WithName("GetTenantFiscalDataPaged")
        .WithSummary("Swagger.Endpoint.TenantFiscalData.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<TenantFiscalDataResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/{tenantId}/fiscal-data", async ([FromRoute] int tenantId, [FromBody] CreateTenantFiscalDataRequest request, [FromServices] ITenantFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(tenantId, request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantFiscalData", "Create")
        .WithName("CreateTenantFiscalData")
        .WithSummary("Swagger.Endpoint.TenantFiscalData.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateTenantFiscalDataRequest>();

        groupV1.MapPut("/{tenantId}/fiscal-data/{id}", async ([FromRoute] int tenantId, [FromRoute] int id, [FromBody] UpdateTenantFiscalDataRequest request, [FromServices] ITenantFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(tenantId, id, request, ct);
            return notify.CustomResponse(updated, 204);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantFiscalData", "Update")
        .WithName("UpdateTenantFiscalData")
        .WithSummary("Swagger.Endpoint.TenantFiscalData.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateTenantFiscalDataRequest>();

        groupV1.MapPatch("/{tenantId}/fiscal-data/{id}/activate", async ([FromRoute] int tenantId, [FromRoute] int id, [FromServices] ITenantFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(tenantId, id, ct);
            return notify.CustomResponse(ok, 204);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantFiscalData", "Activate")
        .WithName("ActivateTenantFiscalData")
        .WithSummary("Swagger.Endpoint.TenantFiscalData.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{tenantId}/fiscal-data/{id}/deactivate", async ([FromRoute] int tenantId, [FromRoute] int id, [FromServices] ITenantFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(tenantId, id, ct);
            return notify.CustomResponse(ok, 204);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantFiscalData", "Deactivate")
        .WithName("DeactivateTenantFiscalData")
        .WithSummary("Swagger.Endpoint.TenantFiscalData.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{tenantId}/fiscal-data/{id}", async ([FromRoute] int tenantId, [FromRoute] int id, [FromServices] ITenantFiscalDataAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(tenantId, id, ct);
            return notify.CustomResponse(ok, 204);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "TenantFiscalData", "Delete")
        .WithName("DeleteTenantFiscalData")
        .WithSummary("Swagger.Endpoint.TenantFiscalData.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
