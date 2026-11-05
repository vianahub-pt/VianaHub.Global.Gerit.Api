using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.Subscription;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.Subscription;
using VianaHub.Global.Gerit.Application.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Billing;

[EndpointMapper]
public static class SubscriptionEndpoint
{
    public static void MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/subscriptions").WithTags("Subscriptions").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/", async ([FromServices] ISubscriptionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var subscriptions = await appService.GetAllAsync(ct);
            return notify.CustomResponse(subscriptions, 200);
        })
        .CustomAuthorize("Admin,BackOffice", "Subscriptions", "GetAll")
        .WithName("GetAllSubscriptions")
        .WithSummary("Swagger.Endpoint.Subscription.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<SubscriptionResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{id}", async ([FromRoute] int id, [FromServices] ISubscriptionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var subscription = await appService.GetByIdAsync(id, ct);
            return notify.CustomResponse(subscription, 200);
        })
        .CustomAuthorize("Admin,BackOffice", "Subscriptions", "GetBy")
        .WithName("GetSubscriptionById")
        .WithSummary("Swagger.Endpoint.Subscription.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<SubscriptionDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/tenant/{tenantId}", async (int tenantId, [FromServices] ISubscriptionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var subscription = await appService.GetByTenantIdAsync(tenantId, ct);
            return notify.CustomResponse(subscription, 200);
        })
        .CustomAuthorize("Admin,BackOffice", "Subscriptions", "GetBy")
        .WithName("GetSubscriptionByTenantId")
        .WithSummary("Swagger.Endpoint.Subscription.GetByTenantId.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<SubscriptionDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/active", async ([FromServices] ISubscriptionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var subscriptions = await appService.GetActiveSubscriptionsAsync(ct);
            return notify.CustomResponse(subscriptions, 200);
        })
        .CustomAuthorize("Admin,BackOffice", "Subscriptions", "GetActivate")
        .WithName("GetActiveSubscriptions")
        .WithSummary("Swagger.Endpoint.Subscription.GetActive.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<SubscriptionResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/expiring/{days}", async (int days, [FromServices] ISubscriptionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var subscriptions = await appService.GetExpiringSubscriptionsAsync(days, ct);
            return notify.CustomResponse(subscriptions, 200);
        })
        .CustomAuthorize("Admin,BackOffice", "Subscriptions", "GetExpiring")
        .WithName("GetExpiringSubscriptions")
        .WithSummary("Swagger.Endpoint.Subscription.GetExpiring.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<SubscriptionResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/paged", async ([AsParameters] PagedFilterRequest request, [FromServices] ISubscriptionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var subscriptions = await appService.GetPagedAsync(request, ct);
            return notify.CustomResponse(subscriptions, 200);
        })
        .CustomAuthorize("Admin,BackOffice", "Subscriptions", "GetPaged")
        .WithName("GetPagedSubscriptions")
        .WithSummary("Swagger.Endpoint.Subscription.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<SubscriptionResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/", async ([FromBody] CreateSubscriptionRequest request, [FromServices] ISubscriptionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice", "Subscriptions", "Create")
        .WithName("CreateSubscription")
        .WithSummary("Swagger.Endpoint.Subscription.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPut("/{id}", async ([FromRoute] int id, [FromBody] UpdateSubscriptionRequest request, [FromServices] ISubscriptionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.UpdateAsync(id, request, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice", "Subscriptions", "Update")
        .WithName("UpdateSubscription")
        .WithSummary("Swagger.Endpoint.Subscription.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{id}/activate", async ([FromRoute] int id, [FromServices] ISubscriptionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice", "Subscriptions", "Activate")
        .WithSummary("Swagger.Endpoint.Subscription.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{id}/deactivate", async ([FromRoute] int id, [FromServices] ISubscriptionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice", "Subscriptions", "Deactivate")
        .WithName("DeactivateSubscription")
        .WithSummary("Swagger.Endpoint.Subscription.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{id}/cancel", async ([FromRoute] int id, [FromBody] CancelSubscriptionRequest request, [FromServices] ISubscriptionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.CancelAsync(id, request, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice", "Subscriptions", "Cancel")
        .WithName("CancelSubscription")
        .WithSummary("Swagger.Endpoint.Subscription.Cancel.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{id}/renew", async ([FromRoute] int id, [FromBody] RenewSubscriptionRequest request, [FromServices] ISubscriptionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.RenewAsync(id, request, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice", "Subscriptions", "Renew")
        .WithName("RenewSubscription")
        .WithSummary("Swagger.Endpoint.Subscription.Renew.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{id}", async ([FromRoute] int id, [FromServices] ISubscriptionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice", "Subscriptions", "Delete")
        .WithName("DeleteSubscription")
        .WithSummary("Swagger.Endpoint.Subscription.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        // Upload massivo de Functions via CSV
        groupV1.MapPost("/bulk-upload", async (HttpRequest request, [FromServices] ISubscriptionAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                // Utiliza chave de tradu��o para mensagem
                notify.Add("Api.Upload.NoFileProvided", 400);
                return notify.CustomResponse();
            }

            var file = request.Form.Files[0];
            var success = await appService.BulkUploadAsync(file, ct);
            return notify.CustomResponse(success);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "Subscriptions", "BulkUpload")
        .WithName("BulkUploadSubscriptions")
        .WithSummary("Swagger.Endpoint.Subscription.BulkUpload.Summary")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
