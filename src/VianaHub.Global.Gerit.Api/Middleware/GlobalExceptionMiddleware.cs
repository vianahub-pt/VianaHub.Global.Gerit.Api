using VianaHub.Global.Gerit.Domain.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net;
using System.Text.Json;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using AutoMapper;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Middleware;

/// <summary>
/// Middleware global para tratamento centralizado de exceções e padronização de respostas de erro.
/// Garante que nenhum detalhe técnico seja exposto aos clientes.
/// Todos os detalhes técnicos são registrados nos logs do Serilog para diagnóstico.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    /// <summary>
    /// Inicializa uma nova instância do <see cref="GlobalExceptionMiddleware"/>.
    /// </summary>
    public GlobalExceptionMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    /// <summary>
    /// Executa o middleware para tratamento global de exceções.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, INotify notify, ILocalizationService localization)
    {
        try
        {
            await _next(context);

            // Se serviços adicionaram notificações (sem lançar exceção), retornar resposta padronizada
            if (notify != null && notify.HasNotify())
            {
                // Evitar reprocessamento se a resposta já começou
                if (context.Response.HasStarted)
                {
                    Log.Warning("⚠️ [NOTIFY] Response already started; cannot modify");
                    return;
                }

                var statusCode = (int)notify.GetStatusCode();

                // Logar resumo da notificação para rastreabilidade
                Log.Information("🔔 [NOTIFY] Notifications found after pipeline execution at {Path}. Status: {StatusCode}", LogValueSanitizer.Sanitize(context.Request.Path.Value), statusCode);

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json; charset=utf-8";

                var errorResponse = new ErrorResponse(GetErrorTitle(statusCode, localization));

                foreach (var message in notify.GetErrorMessage())
                {
                    if (message.Contains(":"))
                    {
                        var parts = message.Split(':', 2);
                        // Field may be a key or literal; we keep as-is for consistency
                        var field = parts[0].Trim();
                        var payload = parts[1].Trim();

                        // If payload encodes key with args (key|arg1|arg2), resolve for response
                        var resolved = ResolvePayloadLocalization(payload, localization);
                        errorResponse.AddError(field, resolved);
                    }
                    else
                    {
                        var resolved = ResolvePayloadLocalization(message, localization);
                        errorResponse.AddError(localization.GetMessage("Api.Middleware.GlobalException.Notification.FieldLabel.Error"), resolved);
                    }
                }

                var json = JsonSerializer.Serialize(errorResponse, GetJsonSerializerOptions());
                await context.Response.WriteAsync(json);
                return;
            }
        }
        catch (BadHttpRequestException ex) when (ex.InnerException is JsonException jsonEx)
        {
            // Tratamento específico para JSON malformatado
            var errorId = Guid.NewGuid().ToString("N")[..12];
            Log.Warning("⚠️ [ERROR-{ErrorId}] Malformed JSON received at {Path} | Exception: {ExceptionDetails}", errorId, LogValueSanitizer.Sanitize(context.Request.Path.Value), LogValueSanitizer.ExceptionDetails(ex));
            await HandleJsonException(context, jsonEx, errorId, notify, localization);
        }
        catch (JsonException jsonEx)
        {
            // Tratamento específico para erros de deserialização JSON
            var errorId = Guid.NewGuid().ToString("N")[..12];
            Log.Warning("⚠️ [ERROR-{ErrorId}] Error processing JSON at {Path} | Exception: {ExceptionDetails}", errorId, LogValueSanitizer.Sanitize(context.Request.Path.Value), LogValueSanitizer.ExceptionDetails(jsonEx));
            await HandleJsonException(context, jsonEx, errorId, notify, localization);
        }
        catch (AutoMapperMappingException autoMapperEx)
        {
            // Tratamento específico para erros de mapeamento do AutoMapper
            var errorId = Guid.NewGuid().ToString("N")[..12];
            Log.Error("❌ [ERROR-{ErrorId}] AutoMapper mapping exception at {Path} | Exception: {ExceptionDetails}", errorId, LogValueSanitizer.Sanitize(context.Request.Path.Value), LogValueSanitizer.ExceptionDetails(autoMapperEx));
            await HandleAutoMapperException(context, autoMapperEx, errorId, notify, localization);
        }
        catch (Exception ex)
        {
            // Gerar ID único para rastreamento do erro
            var errorId = Guid.NewGuid().ToString("N")[..12];

            // Log completo e estruturado do erro
            Log.Error("❌ [ERROR-{ErrorId}] Unhandled exception in API\n" +
                "   📍 Path: {Path}\n" +
                "   🔧 Method: {Method}\n" +
                "   🌐 IP: {RemoteIP}\n" +
                "   🔥 Exception: {ExceptionType}\n" +
                "   💬 Message: {ExceptionMessage}\n" +
                "   👤 User: {UserId} | Exception: {ExceptionDetails}", errorId, LogValueSanitizer.Sanitize(context.Request.Path.Value), context.Request.Method, context.Connection.RemoteIpAddress?.ToString(), ex.GetType().Name, LogValueSanitizer.Sanitize(ex.Message), LogValueSanitizer.Sanitize(context.User?.Identity?.Name ?? "Anonymous"), LogValueSanitizer.ExceptionDetails(ex));

            await HandleExceptionAsync(context, ex, errorId, notify, localization);
        }
    }

    /// <summary>
    /// Trata especificamente exceções de mapeamento do AutoMapper com mensagens amigáveis.
    /// </summary>
    private async Task HandleAutoMapperException(HttpContext context, AutoMapperMappingException autoMapperEx, string errorId, INotify notify, ILocalizationService localization)
    {
        // Evitar reprocessamento se a resposta já começou
        if (context.Response.HasStarted)
        {
            Log.Warning("⚠️ [ERROR-{ErrorId}] Response already started; cannot modify", errorId);
            return;
        }

        // Log detalhado para diagnóstico técnico
        Log.Error("[ERROR-{ErrorId}] Detailed AutoMapper exception:\n" +
            "   📝 Message: {Message}\n" +
            "   🗺️ Mapping Types: {MappingTypes}\n" +
            "   🔥 Inner Exception: {InnerException} | Exception: {ExceptionDetails}", errorId, LogValueSanitizer.Sanitize(autoMapperEx.Message), $"{autoMapperEx.Types?.SourceType?.Name} -> {autoMapperEx.Types?.DestinationType?.Name}", LogValueSanitizer.Sanitize(autoMapperEx.InnerException?.Message ?? "N/A"), LogValueSanitizer.ExceptionDetails(autoMapperEx));

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json; charset=utf-8";

        // Adiciona chave de tradução ao notify
        notify.Add("Api.Middleware.GlobalException.AutoMapperException.Error.MappingError", 500);

        var errorResponse = new ErrorResponse(localization.GetMessage("Api.Middleware.GlobalException.StatusCode.Error.InternalServerError"));
        errorResponse.AddError(
            localization.GetMessage("Api.Middleware.GlobalException.AutoMapperException.Error.FieldLabel.System"),
            localization.GetMessage("Api.Middleware.GlobalException.AutoMapperException.Error.DataMappingError"));
        errorResponse.AddError(
            localization.GetMessage("Api.Middleware.GlobalException.AutoMapperException.Error.FieldLabel.ErrorId"),
            localization.GetMessage("Api.Middleware.GlobalException.AutoMapperException.Error.ContactSupport", errorId));

        var json = JsonSerializer.Serialize(errorResponse, GetJsonSerializerOptions());
        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Trata especificamente exceções de JSON malformatado com mensagens amigáveis.
    /// </summary>
    private async Task HandleJsonException(HttpContext context, JsonException jsonEx, string errorId, INotify notify, ILocalizationService localization)
    {
        // Evitar reprocessamento se a resposta já começou
        if (context.Response.HasStarted)
        {
            Log.Warning("⚠️ [ERROR-{ErrorId}] Response already started; cannot modify", errorId);
            return;
        }

        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json; charset=utf-8";

        string friendlyKey = GetFriendlyJsonErrorKey(jsonEx);
        // Adiciona chave de tradução ao notify (sem resolver aqui)
        notify.Add(friendlyKey, 400);

        var errorResponse = new ErrorResponse(localization.GetMessage("Api.Middleware.GlobalException.JsonException.Error.JsonFormatError"));
        errorResponse.AddError(localization.GetMessage("Api.Middleware.GlobalException.JsonException.Error.FieldLabel.Format"), localization.GetMessage(friendlyKey));
        errorResponse.AddError(localization.GetMessage("Api.Middleware.GlobalException.JsonException.Error.FieldLabel.ErrorId"), localization.GetMessage("Api.Middleware.GlobalException.JsonException.Error.ContactSupport", errorId));

        var lineNumber = GetLineNumber(jsonEx);
        var position = GetBytePosition(jsonEx);

        if (lineNumber.HasValue)
            errorResponse.AddError(localization.GetMessage("Api.Middleware.GlobalException.JsonException.Error.FieldLabel.Line"), lineNumber.Value.ToString());

        if (position.HasValue)
            errorResponse.AddError(localization.GetMessage("Api.Middleware.GlobalException.JsonException.Error.FieldLabel.Position"), position.Value.ToString());

        var json = JsonSerializer.Serialize(errorResponse, GetJsonSerializerOptions());
        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Trata a exceção e retorna uma resposta padronizada ao cliente.
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string errorId, INotify notify, ILocalizationService localization)
    {
        // Evitar reprocessamento se a resposta já começou
        if (context.Response.HasStarted)
        {
            Log.Warning("⚠️ [ERROR-{ErrorId}] Response already started; cannot modify", errorId);
            return;
        }

        context.Response.ContentType = "application/json; charset=utf-8";

        // Tratamento específico para SqlException
        if (exception is SqlException sqlEx)
        {
            // Logar detalhes completos internos para diagnóstico
            Log.Error("[ERROR-{ErrorId}] Detailed SqlException:\n" +
                "   🔢 Error Number: {ErrorNumber}\n" +
                "   📝 Message: {Message}\n" +
                "   🔧 Procedure: {Procedure}\n" +
                "   📍 LineNumber: {LineNumber} | Exception: {ExceptionDetails}", errorId, sqlEx.Number, LogValueSanitizer.Sanitize(sqlEx.Message), sqlEx.Procedure ?? "N/A", sqlEx.LineNumber, LogValueSanitizer.ExceptionDetails(sqlEx));

            // Adiciona a chave de mensagem ao notify
            notify.Add("Api.Middleware.GlobalException.SqlException.Error.DatabaseError", 500);

            var statusCode = (int)notify.GetStatusCode();
            context.Response.StatusCode = statusCode;

            var sqlErrorResponse = new ErrorResponse(GetErrorTitle(statusCode, localization));
            sqlErrorResponse.AddError(localization.GetMessage("Api.Middleware.GlobalException.SqlException.Error.FieldLabel.System"), localization.GetMessage("Api.Middleware.GlobalException.SqlException.Error.SystemError"));
            sqlErrorResponse.AddError(localization.GetMessage("Api.Middleware.GlobalException.SqlException.Error.FieldLabel.ErrorId"), localization.GetMessage("Api.Middleware.GlobalException.SqlException.Error.ContactSupport", errorId));

            var jsonSql = JsonSerializer.Serialize(sqlErrorResponse, GetJsonSerializerOptions());
            await context.Response.WriteAsync(jsonSql);
            return;
        }

        // Tratamento específico para exceções de atualização do EF Core
        if (exception is DbUpdateException dbEx)
        {
            // Inspecionar inner exception em busca de indicadores de violação de restrições
            var innerMessage = dbEx.InnerException?.Message ?? string.Empty;
            var innerEx = dbEx.InnerException;

            // Verificar se é SqlException com erro 2628 (string/binary truncation)
            if (innerEx is SqlException innerSqlEx && innerSqlEx.Number == 2628)
            {
                Log.Error("[ERROR-{ErrorId}] Detailed SqlException 2628 (String/Binary Truncation):\n" +
                    "   🔢 Error Number: {ErrorNumber}\n" +
                    "   📝 Message: {Message}\n" +
                    "   🔧 Procedure: {Procedure} | Exception: {ExceptionDetails}", errorId, innerSqlEx.Number, LogValueSanitizer.Sanitize(innerSqlEx.Message), innerSqlEx.Procedure ?? "N/A", LogValueSanitizer.ExceptionDetails(innerSqlEx));

                // Extrair o nome da tabela e coluna da mensagem de erro
                // Formato típico: "String or binary data would be truncated in table 'dbo.ClientIndividuals', column 'Nationality'."
                var tableName = ExtractTableNameFromTruncationMessage(innerSqlEx.Message);
                var columnName = ExtractColumnNameFromTruncationMessage(innerSqlEx.Message);

                notify.Add("Api.Middleware.GlobalException.DbUpdateException.Error.FieldLabel.System", 400);

                context.Response.StatusCode = 400;
                context.Response.ContentType = "application/json; charset=utf-8";

                var truncErrorResponse = new ErrorResponse(localization.GetMessage("Api.Middleware.GlobalException.StatusCode.Error.InvalidRequest"));
                truncErrorResponse.AddError(
                    localization.GetMessage("Api.Middleware.GlobalException.DbUpdateException.Error.FieldLabel.Error"),
                    localization.GetMessage("Api.Middleware.GlobalException.DbUpdateException.Error.FieldTruncation", columnName ?? tableName ?? "unknown"));
                truncErrorResponse.AddError(
                    localization.GetMessage("Api.Middleware.GlobalException.DbUpdateException.Error.FieldLabel.ErrorId"),
                    localization.GetMessage("Api.Middleware.GlobalException.DbUpdateException.Error.ContactSupport", errorId));

                var jsonTrunc = JsonSerializer.Serialize(truncErrorResponse, GetJsonSerializerOptions());
                await context.Response.WriteAsync(jsonTrunc);
                return;
            }

            // Logar detalhes completos internos para diagnóstico
            Log.Error("[ERROR-{ErrorId}] Detailed DbUpdateException:\n" +
                "   📝 Message: {Message}\n" +
                "   🔥 Inner Exception: {InnerException}\n" +
                "   📋 Entries: {EntriesCount} | Exception: {ExceptionDetails}", errorId, LogValueSanitizer.Sanitize(dbEx.Message), LogValueSanitizer.Sanitize(dbEx.InnerException?.Message ?? "N/A"), dbEx.Entries?.Count() ?? 0, LogValueSanitizer.ExceptionDetails(dbEx));

            if (innerMessage.IndexOf("FK_", StringComparison.OrdinalIgnoreCase) >= 0 ||
                innerMessage.IndexOf("FOREIGN KEY", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                notify.Add("Api.Middleware.GlobalException.DbUpdateException.Error.TenantNotExistOrInactive", 400);
            }
            else if (innerMessage.IndexOf("UNIQUE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     innerMessage.IndexOf("duplicate", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                notify.Add("Api.Middleware.GlobalException.DbUpdateException.Error.DuplicateApplication", 409);
            }
            else
            {
                notify.Add("Api.Middleware.GlobalException.DbUpdateException.Error.SaveDataError", 400);
            }

            // Construir resposta a partir do notify para retornar mensagens amigáveis ao consumidor
            var statusCode = (int)notify.GetStatusCode();
            context.Response.StatusCode = statusCode;

            var efErrorResponse = new ErrorResponse(GetErrorTitle(statusCode, localization));
            efErrorResponse.AddError(localization.GetMessage("Api.Middleware.GlobalException.DbUpdateException.Error.FieldLabel.ErrorId.ErrorResponse"), localization.GetMessage("Api.Middleware.GlobalException.DbUpdateException.Error.ContactSupport.ErrorResponse", errorId));

            foreach (var message in notify.GetErrorMessage())
            {
                if (message.Contains(":"))
                {
                    var parts = message.Split(':', 2);
                    efErrorResponse.AddError(parts[0].Trim(), ResolvePayloadLocalization(parts[1].Trim(), localization));
                }
                else
                {
                    efErrorResponse.AddError(localization.GetMessage("Api.Middleware.GlobalException.DbUpdateException.Error.FieldLabel.Error"), ResolvePayloadLocalization(message, localization));
                }
            }

            var jsonEf = JsonSerializer.Serialize(efErrorResponse, GetJsonSerializerOptions());
            await context.Response.WriteAsync(jsonEf);
            return;
        }

        // Tratamento genérico para erros inesperados
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // Log detalhado para diagnóstico
        Log.Error("[ERROR-{ErrorId}] Detailed generic exception:\n" +
            "   🔥 Exception Type: {ExceptionType}\n" +
            "   📝 Message: {Message}\n" +
            "   🔗 Inner Exception: {InnerException}\n" +
            "   📚 Stack Trace: {StackTrace} | Exception: {ExceptionDetails}", errorId, exception.GetType().FullName, LogValueSanitizer.Sanitize(exception.Message), LogValueSanitizer.Sanitize(exception.InnerException?.Message ?? "N/A"), exception.StackTrace ?? "N/A", LogValueSanitizer.ExceptionDetails(exception));

        // Adicionar notificação amigável (uma chave)
        notify.Add("Api.Middleware.GlobalException.DbUpdateException.Error.UnexpectedError", 500);

        // Criar resposta padronizada - APENAS informações seguras para o cliente
        var errorResponse = new ErrorResponse(localization.GetMessage("Api.Middleware.GlobalException.DbUpdateException.Error.InternalServerError"));

        // Mensagem amigável para o usuário
        errorResponse.AddError(localization.GetMessage("Api.Middleware.GlobalException.DbUpdateException.Error.FieldLabel.System"),
            localization.GetMessage("Api.Middleware.GlobalException.DbUpdateException.Error.UnexpectedError.User"));

        // Adicionar ID do erro para rastreamento
        errorResponse.AddError(localization.GetMessage("Api.Middleware.GlobalException.DbUpdateException.Error.FieldLabel.ErrorId"), localization.GetMessage("Api.Middleware.GlobalException.DbUpdateException.Error.ContactSupport", errorId));

        // Serializar e retornar resposta
        var json = JsonSerializer.Serialize(errorResponse, GetJsonSerializerOptions());
        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Retorna uma chave de mensagem amigável baseada no tipo de erro JSON.
    /// </summary>
    private static string GetFriendlyJsonErrorKey(JsonException jsonEx)
    {
        var message = jsonEx.Message.ToLower();

        if (message.Contains("trailing comma"))
            return "Api.Middleware.GlobalException.JsonException.Error.JsonTrailingComma";

        if (message.Contains("unexpected character") || message.Contains("invalid character"))
            return "Api.Middleware.GlobalException.JsonException.Error.JsonInvalidCharacter";

        if (message.Contains("unterminated string"))
            return "Api.Middleware.GlobalException.JsonException.Error.JsonUnterminatedString";

        if (message.Contains("expected") && message.Contains("got"))
            return "Api.Middleware.GlobalException.JsonException.Error.JsonMalformed";

        if (message.Contains("depth"))
            return "Api.Middleware.GlobalException.JsonException.Error.JsonTooManyLevels";

        if (message.Contains("property name"))
            return "Api.Middleware.GlobalException.JsonException.Error.JsonInvalidPropertyName";

        if (message.Contains("missing"))
            return "Api.Middleware.GlobalException.JsonException.Error.JsonIncomplete";

        return "Api.Middleware.GlobalException.JsonException.Error.JsonGeneric";
    }

    /// <summary>
    /// Resolve payload que pode ser uma chave com argumentos (key|arg1|arg2) ou texto literal.
    /// </summary>
    private static string ResolvePayloadLocalization(string payload, ILocalizationService localization)
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

    /// <summary>
    /// Extrai o número da linha do erro JSON, se disponível.
    /// </summary>
    private static int? GetLineNumber(JsonException jsonEx)
    {
        try
        {
            var lineNumberProperty = jsonEx.GetType().GetProperty("LineNumber");
            if (lineNumberProperty != null)
            {
                var value = lineNumberProperty.GetValue(jsonEx);
                if (value != null)
                {
                    return Convert.ToInt32(value);
                }
            }
        }
        catch
        {
            // Ignora erros ao tentar extrair informação de linha
        }
        return null;
    }

    /// <summary>
    /// Extrai a posição do byte na linha do erro JSON, se disponível.
    /// </summary>
    private static int? GetBytePosition(JsonException jsonEx)
    {
        try
        {
            var bytePositionProperty = jsonEx.GetType().GetProperty("BytePositionInLine");
            if (bytePositionProperty != null)
            {
                var value = bytePositionProperty.GetValue(jsonEx);
                if (value != null)
                {
                    return Convert.ToInt32(value);
                }
            }
        }
        catch
        {
            // Ignora erros ao tentar extrair informação de posição
        }
        return null;
    }

    /// <summary>
    /// Retorna as opções de serialização JSON padronizadas.
    /// </summary>
    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    /// <summary>
    /// Retorna um título amigável baseado no código de status HTTP.
    /// </summary>
    private static string GetErrorTitle(int statusCode, ILocalizationService localization)
    {
        return statusCode switch
        {
            400 => localization.GetMessage("Api.Middleware.GlobalException.StatusCode.Error.InvalidRequest"),
            401 => localization.GetMessage("Api.Middleware.GlobalException.StatusCode.Error.Unauthorized"),
            403 => localization.GetMessage("Api.Middleware.GlobalException.StatusCode.Error.Forbidden"),
            404 => localization.GetMessage("Api.Middleware.GlobalException.StatusCode.Error.NotFound"),
            409 => localization.GetMessage("Api.Middleware.GlobalException.StatusCode.Error.Conflict"),
            500 => localization.GetMessage("Api.Middleware.GlobalException.StatusCode.Error.InternalServerError"),
            _ => localization.GetMessage("Api.Middleware.GlobalException.StatusCode.Error.GenericError")
        };
    }

    /// <summary>
    /// Extrai o nome da coluna da mensagem de erro de truncamento do SQL Server (erro 2628).
    /// Formato esperado: "String or binary data would be truncated in table 'dbo.ClientIndividuals', column 'Nationality'."
    /// </summary>
    private static string? ExtractColumnNameFromTruncationMessage(string message)
    {
        try
        {
            var columnMarker = "column '";
            var columnIndex = message.IndexOf(columnMarker, StringComparison.OrdinalIgnoreCase);
            if (columnIndex >= 0)
            {
                var start = columnIndex + columnMarker.Length;
                var end = message.IndexOf("'", start);
                if (end >= 0)
                {
                    return message.Substring(start, end - start);
                }
            }
        }
        catch
        {
            // Ignora erros de parsing
        }
        return null;
    }

    /// <summary>
    /// Extrai o nome da tabela da mensagem de erro de truncamento do SQL Server (erro 2628).
    /// Formato esperado: "String or binary data would be truncated in table 'dbo.ClientIndividuals', column 'Nationality'."
    /// </summary>
    private static string? ExtractTableNameFromTruncationMessage(string message)
    {
        try
        {
            var tableMarker = "table '";
            var tableIndex = message.IndexOf(tableMarker, StringComparison.OrdinalIgnoreCase);
            if (tableIndex >= 0)
            {
                var start = tableIndex + tableMarker.Length;
                var end = message.IndexOf("'", start);
                if (end >= 0)
                {
                    // Extrair apenas o nome da tabela sem o schema (dbo.)
                    var fullName = message.Substring(start, end - start);
                    var dotIndex = fullName.IndexOf('.');
                    return dotIndex >= 0 ? fullName.Substring(dotIndex + 1) : fullName;
                }
            }
        }
        catch
        {
            // Ignora erros de parsing
        }
        return null;
    }
}
