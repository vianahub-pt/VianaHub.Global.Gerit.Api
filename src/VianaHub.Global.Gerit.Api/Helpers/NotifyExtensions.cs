using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Api.Helpers;

/// <summary>
/// Classe responsável por mapear os endpoints de NotifyExtensions.
/// </summary>
public static class NotifyExtensions
{
    /// <summary>
    /// Retorna uma resposta customizada para o padrão de notificação.
    /// </summary>
    public static IResult CustomResponse<T>(this INotify notify, T data, int statusCode = 200)
    {
        if (notify.HasNotify())
        {
            statusCode = (int)notify.GetStatusCode();
            var errorMessages = notify.GetErrorMessage();

            var localization = GetLocalizationService();
            var errorResponse = new ErrorResponse(GetErrorTitle(statusCode));

            foreach (var message in errorMessages)
            {
                if (message.Contains(":"))
                {
                    var parts = message.Split(':', 2);
                    var field = parts[0].Trim();
                    var payload = parts[1].Trim();
                    var resolved = ResolvePayloadLocalization(payload, localization);
                    errorResponse.AddError(field, resolved);
                }
                else
                {
                    var resolved = ResolvePayloadLocalization(message, localization);
                    errorResponse.AddError(GetLocalizedErrorLabel(), resolved);
                }
            }
            return Results.Json(errorResponse, statusCode: statusCode);
        }

        if (statusCode == 204)
        {
            return Results.NoContent();
        }

        return Results.Json(data, statusCode: statusCode);
    }

    public static IResult CustomResponse(this INotify notify, int statusCode = 200)
    {
        // Se houver notificações de erro
        if (notify.HasNotify())
        {
            statusCode = (int)notify.GetStatusCode();
            var errorMessages = notify.GetErrorMessage();

            var localization = GetLocalizationService();
            var errorResponse = new ErrorResponse(GetErrorTitle(statusCode));

            foreach (var message in errorMessages)
            {
                if (message.Contains(":"))
                {
                    var parts = message.Split(':', 2);
                    var field = parts[0].Trim();
                    var payload = parts[1].Trim();
                    var resolved = ResolvePayloadLocalization(payload, localization);
                    errorResponse.AddError(field, resolved);
                }
                else
                {
                    var resolved = ResolvePayloadLocalization(message, localization);
                    errorResponse.AddError(GetLocalizedErrorLabel(), resolved);
                }
            }

            return Results.Json(errorResponse, statusCode: statusCode);
        }

        // Sem erros → devolve o status code solicitado
        if (statusCode == 204)
        {
            return Results.NoContent();
        }

        return Results.StatusCode(statusCode);
    }

    public static IResult CustomResponse(this INotify notify)
    {
        int statusCode = 200;

        if (notify.HasNotify())
        {
            statusCode = (int)notify.GetStatusCode();
            var errorMessages = notify.GetErrorMessage();
            var localization = GetLocalizationService();
            var errorResponse = new ErrorResponse(GetErrorTitle(statusCode));
            foreach (var message in errorMessages)
            {
                if (message.Contains(":"))
                {
                    var parts = message.Split(':', 2);
                    var field = parts[0].Trim();
                    var payload = parts[1].Trim();
                    var resolved = ResolvePayloadLocalization(payload, localization);
                    errorResponse.AddError(field, resolved);
                }
                else
                {
                    var resolved = ResolvePayloadLocalization(message, localization);
                    errorResponse.AddError(GetLocalizedErrorLabel(), resolved);
                }
            }
            return Results.Json(errorResponse, statusCode: statusCode);
        }

        return Results.Ok();
    }

    /// <summary>
    /// Adiciona um erro de campo à notificação.
    /// </summary>
    public static void AddFieldError(this INotify notify, string field, string message, int statusCode = 400)
    {
        // Mantém formato "field: payload" onde payload pode ser uma chave ou texto literal.
        notify.Add($"{field}: {message}", statusCode);
    }

    private static string GetErrorTitle(int statusCode)
    {
        var localization = GetLocalizationService();

        // Em vez de retornar mensagens hardcoded, retornamos a chave correspondente.
        // Isso garante que todas as mensagens passem por tradução conforme diretriz de arquitetura.
        if (localization == null)
        {
            return statusCode switch
            {
                400 => "Api.Helpers.NotifyExtensions.Error.ValidationError",
                401 => "Api.Helpers.NotifyExtensions.Error.Unauthorized",
                403 => "Api.Helpers.NotifyExtensions.Error.Forbidden",
                404 => "Api.Helpers.NotifyExtensions.Error.NotFound",
                409 => "Api.Helpers.NotifyExtensions.Error.Conflict",
                410 => "Api.Helpers.NotifyExtensions.Error.Gone",
                422 => "Api.Helpers.NotifyExtensions.Error.UnprocessableEntity",
                429 => "Api.Helpers.NotifyExtensions.Error.TooManyRequests",
                500 => "Api.Helpers.NotifyExtensions.Error.InternalServerError",
                503 => "Api.Helpers.NotifyExtensions.Error.ServiceUnavailable",
                _ => "Api.Helpers.NotifyExtensions.Error.Generic"
            };
        }

        return statusCode switch
        {
            400 => localization.GetMessage("Api.Helpers.NotifyExtensions.Error.ValidationError"),
            401 => localization.GetMessage("Api.Helpers.NotifyExtensions.Error.Unauthorized"),
            403 => localization.GetMessage("Api.Helpers.NotifyExtensions.Error.Forbidden"),
            404 => localization.GetMessage("Api.Helpers.NotifyExtensions.Error.NotFound"),
            409 => localization.GetMessage("Api.Helpers.NotifyExtensions.Error.Conflict"),
            410 => localization.GetMessage("Api.Helpers.NotifyExtensions.Error.Gone"),
            422 => localization.GetMessage("Api.Helpers.NotifyExtensions.Error.UnprocessableEntity"),
            429 => localization.GetMessage("Api.Helpers.NotifyExtensions.Error.TooManyRequests"),
            500 => localization.GetMessage("Api.Helpers.NotifyExtensions.Error.InternalServerError"),
            503 => localization.GetMessage("Api.Helpers.NotifyExtensions.Error.ServiceUnavailable"),
            _ => localization.GetMessage("Api.Helpers.NotifyExtensions.Error.Generic")
        };
    }

    private static string GetLocalizedErrorLabel()
    {
        var localization = GetLocalizationService();
        // Sempre retornar chave quando localization não estiver disponível
        return localization?.GetMessage("Api.Helpers.NotifyExtensions.Common.Error") ?? "Api.Helpers.NotifyExtensions.Common.Error";
    }

    private static ILocalizationService? GetLocalizationService()
    {
        // Tenta obter o serviço do contexto HTTP atual via IHttpContextAccessor
        var httpContextAccessor = ServiceProviderHolder.ServiceProvider?.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
        return httpContextAccessor?.HttpContext?.RequestServices.GetService(typeof(ILocalizationService)) as ILocalizationService;
    }

    /// <summary>
    /// Resolve payload que pode ser uma chave com argumentos (key|arg1|arg2) ou texto literal.
    /// </summary>
    private static string ResolvePayloadLocalization(string payload, ILocalizationService? localization)
    {
        if (localization == null)
            return payload;

        if (string.IsNullOrWhiteSpace(payload))
            return string.Empty;

        if (payload.Contains('|'))
        {
            var parts = payload.Split('|');
            var key = parts[0];
            var args = parts.Skip(1).ToArray();
            return localization.GetMessage(key, args);
        }

        // Heurística: se parece com uma chave (contém ponto e sem espaços), trata como chave
        if (payload.Contains('.') && !payload.Contains(' '))
        {
            return localization.GetMessage(payload);
        }

        // Caso contrário, assume que é texto já localizado
        return payload;
    }
}
