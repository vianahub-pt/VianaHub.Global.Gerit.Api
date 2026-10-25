using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using VianaHub.Global.Gerit.Infra.Data.Telemetry;

namespace VianaHub.Global.Gerit.Infra.Data.Interceptors;

/// <summary>
/// Interceptor para adicionar telemetria e logging a todas as operações do Entity Framework
/// </summary>
public class TelemetryInterceptor : DbCommandInterceptor
{
    private readonly ILogger<TelemetryInterceptor> _logger;

    public TelemetryInterceptor(ILogger<TelemetryInterceptor> logger)
    {
        _logger = logger;
    }

    // ========== COMMAND EXECUTING ==========

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        LogCommandExecution(command, eventData, "ReaderExecuting");
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        LogCommandExecution(command, eventData, "ReaderExecutingAsync");
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        LogCommandExecution(command, eventData, "NonQueryExecuting");
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        LogCommandExecution(command, eventData, "NonQueryExecutingAsync");
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        LogCommandExecution(command, eventData, "ScalarExecuting");
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        LogCommandExecution(command, eventData, "ScalarExecutingAsync");
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    // ========== COMMAND EXECUTED ==========

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        LogCommandExecuted(command, eventData, "ReaderExecuted");
        RecordMetrics(command, eventData);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogCommandExecuted(command, eventData, "ReaderExecutedAsync");
        RecordMetrics(command, eventData);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        LogCommandExecuted(command, eventData, "NonQueryExecuted");
        RecordMetrics(command, eventData);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        LogCommandExecuted(command, eventData, "NonQueryExecutedAsync");
        RecordMetrics(command, eventData);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        LogCommandExecuted(command, eventData, "ScalarExecuted");
        RecordMetrics(command, eventData);
        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        LogCommandExecuted(command, eventData, "ScalarExecutedAsync");
        RecordMetrics(command, eventData);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    // ========== COMMAND FAILED ==========

    public override void CommandFailed(
        DbCommand command,
        CommandErrorEventData eventData)
    {
        LogCommandFailed(command, eventData);
        RecordErrorMetrics(eventData);
        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        LogCommandFailed(command, eventData);
        RecordErrorMetrics(eventData);
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    // ========== HELPER METHODS ==========

    private void LogCommandExecution(DbCommand command, CommandEventData eventData, string operation)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "?? DB {Operation} | CommandId: {CommandId} | SQL: {CommandText}",
                operation,
                eventData.CommandId,
                command.CommandText.Length > 200 ? command.CommandText[..200] + "..." : command.CommandText
            );
        }
    }

    private void LogCommandExecuted(DbCommand command, CommandExecutedEventData eventData, string operation)
    {
        var duration = eventData.Duration.TotalMilliseconds;
        var logLevel = duration > 1000 ? LogLevel.Warning : LogLevel.Debug;

        if (_logger.IsEnabled(logLevel))
        {
            _logger.Log(
                logLevel,
                "? DB {Operation} | CommandId: {CommandId} | Duration: {Duration}ms",
                operation,
                eventData.CommandId,
                duration
            );
        }
    }

    private void LogCommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        _logger.LogError(
            eventData.Exception,
            "? DB Command Failed | CommandId: {CommandId} | SQL: {CommandText} | Error: {ErrorMessage}",
            eventData.CommandId,
            command.CommandText.Length > 200 ? command.CommandText[..200] + "..." : command.CommandText,
            eventData.Exception.Message
        );
    }

    private void RecordMetrics(DbCommand command, CommandExecutedEventData eventData)
    {
        // Incrementa contador de queries executadas
        DatabaseTelemetry.QueriesExecuted.Add(1,
            new KeyValuePair<string, object?>("command.type", command.CommandType.ToString()));

        // Registra duração da query
        var duration = eventData.Duration.TotalMilliseconds;
        DatabaseTelemetry.QueryDuration.Record(duration,
            new KeyValuePair<string, object?>("command.type", command.CommandType.ToString()));

        // Log de alerta para queries lentas (> 1 segundo)
        if (duration > 1000)
        {
            _logger.LogWarning(
                "??  Query lenta detectada | Duration: {Duration}ms | SQL: {CommandText}",
                duration,
                command.CommandText.Length > 200 ? command.CommandText[..200] + "..." : command.CommandText
            );
        }
    }

    private void RecordErrorMetrics(CommandErrorEventData eventData)
    {
        DatabaseTelemetry.QueriesWithError.Add(1,
            new KeyValuePair<string, object?>("error.type", eventData.Exception.GetType().Name));
    }
}
