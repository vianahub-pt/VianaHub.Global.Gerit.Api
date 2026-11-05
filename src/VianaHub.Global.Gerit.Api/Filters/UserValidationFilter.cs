using VianaHub.Global.Gerit.Domain.Helpers;
#nullable enable

using System.Net;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Filters;

public class UserValidationFilter : IEndpointFilter
{
    private readonly ILogger<UserValidationFilter> _logger;

    public UserValidationFilter(ILogger<UserValidationFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User;

        // Tenta resolver serviço de localização do container
        var localization = httpContext.RequestServices.GetService(typeof(ILocalizationService)) as ILocalizationService;

        // Extrair userId da rota
        var userIdParam = httpContext.Request.RouteValues["userId"]?.ToString();

        if (string.IsNullOrEmpty(userIdParam))
        {
            _logger.LogWarning("? userId não encontrado na URL da requisição");
            var title = localization?.GetMessage("Api.Filters.UserValidation.UserId.Required") ?? "Api.Filters.UserValidation.UserId.Required";
            return CreateErrorResponse(title, localization?.GetMessage("Api.Filters.UserValidation.UserId.Required.Message") ?? "Api.Filters.UserValidation.UserId.Required.Message");
        }

        if (!Guid.TryParse(userIdParam, out var requesteduserId))
        {
            _logger.LogWarning("? userId inválido na URL: {userId}", LogValueSanitizer.Sanitize(userIdParam));
            var title = localization?.GetMessage("Api.Filters.UserValidation.UserId.Invalid") ?? "Api.Filters.UserValidation.UserId.Invalid";
            return CreateErrorResponse(title, localization?.GetMessage("Api.Filters.UserValidation.UserId.Invalid.Message") ?? "Api.Filters.UserValidation.UserId.Invalid.Message");
        }

        // Verificar se usuário está autenticado
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("? Usuário não autenticado tentando acessar user {userId}", requesteduserId);
            return Results.Unauthorized();
        }

        // Extrair userId do token JWT
        // Tentar múltiplas claims para compatibilidade
        var useruserIdClaim = user.FindFirst("user_id")?.Value
            ?? user.FindFirst("tid")?.Value
            ?? user.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(useruserIdClaim))
        {
            _logger.LogWarning(
                "?? Token JWT não contém claim de userId. Usuário: {UserId}",
                LogValueSanitizer.Sanitize(user.FindFirst("sub")?.Value ?? "Unknown"));

            // TODO: Em produção, isso deveria retornar Unauthorized
            // Por enquanto, permite acesso para compatibilidade com tokens antigos
            _logger.LogWarning(
                "?? MODO DE COMPATIBILIDADE: Permitindo acesso sem validação de user. " +
                "ATENÇÃO: Isso será removido em versões futuras!");

            return await next(context);
        }

        if (!Guid.TryParse(useruserIdClaim, out var useruserId))
        {
            _logger.LogError(
                "? userId no token JWT está em formato inválido: {userId}",
                LogValueSanitizer.Sanitize(useruserIdClaim));
            return Results.Unauthorized();
        }

        // Validar se o user da URL corresponde ao user do usuário
        if (requesteduserId != useruserId)
        {
            var userId = user.FindFirst("sub")?.Value ?? "Unknown";
            var userName = user.FindFirst("name")?.Value ?? "Unknown";

            _logger.LogWarning(
                "?? TENTATIVA DE ACESSO CROSS-user DETECTADA! " +
                "Usuário: {UserId} ({UserName}), " +
                "user do Usuário: {UseruserId}, " +
                "user Requisitado: {RequesteduserId}, " +
                "Endpoint: {Endpoint}, " +
                "IP: {IP}",
                LogValueSanitizer.Sanitize(userId),
                LogValueSanitizer.Sanitize(userName),
                useruserId,
                requesteduserId,
                LogValueSanitizer.Sanitize(httpContext.Request.Path.Value),
                httpContext.Connection.RemoteIpAddress);

            var title = localization?.GetMessage("Api.Filters.UserValidation.AccessDenied.Title") ?? "Api.Filters.UserValidation.AccessDenied.Title";
            var message = localization?.GetMessage("Api.Filters.UserValidation.AccessDenied.Message") ?? "Api.Filters.UserValidation.AccessDenied.Message";

            return CreateErrorResponse(title, message, HttpStatusCode.Forbidden, localization);
        }

        _logger.LogDebug(
            "? Validação de user bem-sucedida. Usuário: {UserId}, user: {userId}",
            LogValueSanitizer.Sanitize(user.FindFirst("sub")?.Value),
            useruserId);

        return await next(context);
    }

    private IResult CreateErrorResponse(
        string title,
        string? message = null,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest,
        ILocalizationService? localization = null)
    {
        // Se localization não for fornecida, tentamos resolver a partir do provider atual
        localization ??= ServiceProviderHolder.ServiceProvider?.GetService(typeof(ILocalizationService)) as ILocalizationService;

        var localizedTitle = localization?.GetMessage(title) ?? title;

        var errorResponse = new ErrorResponse(localizedTitle);

        if (!string.IsNullOrEmpty(message))
        {
            var label = localization?.GetMessage("Api.Helpers.NotifyExtensions.Common.Error") ?? "Api.Helpers.NotifyExtensions.Common.Error";
            var localizedMessage = localization?.GetMessage(message) ?? message;
            errorResponse.AddError(label, localizedMessage);
        }

        return Results.Json(errorResponse, statusCode: (int)statusCode);
    }
}
