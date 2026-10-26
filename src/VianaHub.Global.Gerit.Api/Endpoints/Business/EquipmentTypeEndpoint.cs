using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EquipmentType;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using VianaHub.Global.Gerit.Api.Endpoints.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.EquipmentType;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Endpoints.Business;

[EndpointMapper]
public static class EquipmentTypeEndpoint
{
    public static void MapEquipmentTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var groupV1 = app.MapGroup("/v1/equipment-types").WithTags("EquipmentTypes").WithGroupName("v1").RequireAuthorization();

        groupV1.MapGet("/", async ([FromServices] IEquipmentTypeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetAllAsync(ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager,Operator", "EquipmentTypes", "GetAll")
        .WithName("GetAllEquipmentTypes")
        .WithSummary("Swagger.Endpoint.EquipmentType.GetAll.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<IEnumerable<EquipmentTypeResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/{id}", async ([FromRoute] int id, [FromServices] IEquipmentTypeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetByIdAsync(id, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager,Operator", "EquipmentTypes", "GetBy")
        .WithName("GetEquipmentTypeById")
        .WithSummary("Swagger.Endpoint.EquipmentType.GetById.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<EquipmentTypeDetailResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapGet("/paged", async ([AsParameters] PagedFilterRequest request, [FromServices] IEquipmentTypeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var response = await appService.GetPagedAsync(request, ct);
            return notify.CustomResponse(response, 200);
        })
        .CustomAuthorize("Admin,BackOffice,Manager,Operator", "EquipmentTypes", "GetPaged")
        .WithName("GetPagedEquipmentTypes")
        .WithSummary("Swagger.Endpoint.EquipmentType.GetPaged.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ListPageResponse<EquipmentTypeResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPost("/", async ([FromBody] CreateEquipmentTypeRequest request, [FromServices] IEquipmentTypeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var id = await appService.CreateAsync(request, ct);
            return notify.CustomResponse(new GenericResponse { Id = id }, 201);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EquipmentTypes", "Create")
        .WithName("CreateEquipmentType")
        .WithSummary("Swagger.Endpoint.EquipmentType.Create.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<GenericResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<CreateEquipmentTypeRequest>();

        groupV1.MapPut("/{id}", async ([FromRoute] int id, [FromBody] UpdateEquipmentTypeRequest request, [FromServices] IEquipmentTypeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var updated = await appService.UpdateAsync(id, request, ct);
            return notify.CustomResponse(updated ? 204 : 400);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EquipmentTypes", "Update")
        .WithName("UpdateEquipmentType")
        .WithSummary("Swagger.Endpoint.EquipmentType.Update.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
        .WithValidation<UpdateEquipmentTypeRequest>();

        groupV1.MapPatch("/{id}/activate", async ([FromRoute] int id, [FromServices] IEquipmentTypeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.ActivateAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EquipmentTypes", "Activate")
        .WithName("ActivateEquipmentType")
        .WithSummary("Swagger.Endpoint.EquipmentType.Activate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapPatch("/{id}/deactivate", async ([FromRoute] int id, [FromServices] IEquipmentTypeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeactivateAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EquipmentTypes", "Deactivate")
        .WithName("DeactivateEquipmentType")
        .WithSummary("Swagger.Endpoint.EquipmentType.Deactivate.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        groupV1.MapDelete("/{id}", async ([FromRoute] int id, [FromServices] IEquipmentTypeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            var ok = await appService.DeleteAsync(id, ct);
            return notify.CustomResponse();
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EquipmentTypes", "Delete")
        .WithName("DeleteEquipmentType")
        .WithSummary("Swagger.Endpoint.EquipmentType.Delete.Summary")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        // Upload massivo de EquipmentTypes via CSV
        groupV1.MapPost("/bulk-upload", async (HttpRequest request, [FromServices] IEquipmentTypeAppService appService, [FromServices] INotify notify, CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
            {
                notify.Add("Api.Upload.NoFileProvided", 400);
                return notify.CustomResponse();
            }

            var form = await request.ReadFormAsync(ct);
            if (form.Files.Count == 0)
            {
                notify.Add("Api.Upload.NoFileProvided", 400);
                return notify.CustomResponse();
            }

            var file = form.Files[0];
            var success = await appService.BulkUploadAsync(file, ct);
            return notify.CustomResponse(success);
        })
        .CustomAuthorize("Admin,BackOffice,Manager", "EquipmentTypes", "BulkUpload")
        .WithName("BulkUploadEquipmentTypes")
        .WithSummary("Swagger.Endpoint.EquipmentType.BulkUpload.Summary")
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
