using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Infra.Data.Security;
using System.Data;

namespace VianaHub.Global.Gerit.Infra.Data.Interceptors;

/// <summary>
/// Interceptor de conexão responsável por popular o SESSION_CONTEXT do SQL Server com o TenantId
/// correto antes de qualquer operação de banco de dados, garantindo o isolamento do RLS.
///
/// Regras de resolução do TenantId (em ordem de prioridade):
///   1. Usuário autenticado  → claim 'tenant_id' do JWT
///   2. Usuário não autenticado (ex: login, register) → IRequestTenantContext (populado pelo AppService a partir do body)
///   3. Nenhuma das anteriores → não seta SESSION_CONTEXT; RLS bloqueia o acesso
/// </summary>
public class TenantSessionConnectionInterceptor : DbConnectionInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantSessionConnectionInterceptor> _logger;
    private readonly IRequestTenantContext _requestTenantContext;

    public TenantSessionConnectionInterceptor(
        IHttpContextAccessor httpContextAccessor,
        ILogger<TenantSessionConnectionInterceptor> logger,
        IRequestTenantContext requestTenantContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _requestTenantContext = requestTenantContext;
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);

        if (connection is not SqlConnection)
            return;

        // Não herdar flags de uma sessão anteriormente usada pelo pool.
        await SqlRlsSessionContext.ResetAsync(connection, cancellationToken);
        var tenantId = ResolvetenantId();

        if (tenantId is null)
        {
            _logger.LogDebug("[RLS] TenantId não resolvido. SESSION_CONTEXT não será definido. RLS bloqueará o acesso.");
            return;
        }

        await SetTenantSessionContextAsync(connection, tenantId.Value, cancellationToken);
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        base.ConnectionOpened(connection, eventData);
        if (connection is not SqlConnection) return;
        SqlRlsSessionContext.Reset(connection);
        var tenantId = ResolvetenantId();
        if (!tenantId.HasValue) return;
        using var command = connection.CreateCommand();
        command.CommandText = "EXEC sp_set_session_context @key=N'TenantId', @value=@tenantId;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tenantId";
        parameter.Value = tenantId.Value;
        command.Parameters.Add(parameter);
        command.ExecuteNonQuery();
    }

    public override InterceptionResult ConnectionClosing(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        ResetBeforePooling(connection);
        return base.ConnectionClosing(connection, eventData, result);
    }

    public override async ValueTask<InterceptionResult> ConnectionClosingAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        await ResetBeforePoolingAsync(connection);
        return await base.ConnectionClosingAsync(connection, eventData, result);
    }

    public override InterceptionResult ConnectionDisposing(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        ResetBeforePooling(connection);
        return base.ConnectionDisposing(connection, eventData, result);
    }

    public override async ValueTask<InterceptionResult> ConnectionDisposingAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        await ResetBeforePoolingAsync(connection);
        return await base.ConnectionDisposingAsync(connection, eventData, result);
    }

    private void ResetBeforePooling(DbConnection connection)
    {
        if (connection is not SqlConnection sql || connection.State != ConnectionState.Open) return;
        try { SqlRlsSessionContext.Reset(connection); }
        // Intencional: qualquer falha de cleanup (SQL, estado da conexão ou provider) exige descarte do pool.
        catch (Exception ex)
        {
            // Descartar a conexão é obrigatório se não foi possível remover seu contexto.
            SqlConnection.ClearPool(sql);
            _logger.LogWarning("[RLS] Session cleanup failed; SQL connection discarded from pool. ExceptionType={ExceptionType}", ex.GetType().FullName);
        }
    }

    private async Task ResetBeforePoolingAsync(DbConnection connection)
    {
        if (connection is not SqlConnection sql || connection.State != ConnectionState.Open) return;
        try { await SqlRlsSessionContext.ResetAsync(connection, CancellationToken.None); }
        // Intencional: capturar somente DbException deixaria outras falhas devolverem contexto ao pool.
        catch (Exception ex)
        {
            SqlConnection.ClearPool(sql);
            _logger.LogWarning("[RLS] Session cleanup failed; SQL connection discarded from pool. ExceptionType={ExceptionType}", ex.GetType().FullName);
        }
    }

    /// <summary>
    /// Resolve o TenantId para o request atual.
    /// Prioridade: claim JWT → IRequestTenantContext (payload do body, ex: login)
    /// </summary>
    private int? ResolvetenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // Jobs definem um tenant explicito no escopo; JWT autenticado continua tendo prioridade.
        var user = httpContext?.User;

        // 1. Usuário autenticado: TenantId vem exclusivamente do claim do JWT
        if (user?.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = user.FindFirst("tenant_id")
                           ?? user.FindFirst("tenantId")
                           ?? user.FindFirst("tenant");

            if (tenantClaim is not null && int.TryParse(tenantClaim.Value, out var tenantFromJwt))
            {
                _logger.LogDebug("[RLS] TenantId resolvido via JWT claim: {TenantId}", tenantFromJwt);
                return tenantFromJwt;
            }

            _logger.LogWarning("[RLS] Usuário autenticado sem claim tenant_id válida no token.");
            return null;
        }

        // 2. Usuário não autenticado: TenantId vem do IRequestTenantContext (ex: login, register)
        if (_requestTenantContext.TenantId.HasValue)
        {
            _logger.LogDebug("[RLS] TenantId resolvido via IRequestTenantContext (request não autenticado): {TenantId}",
                _requestTenantContext.TenantId.Value);
            return _requestTenantContext.TenantId.Value;
        }

        _logger.LogDebug("[RLS] Request não autenticado sem IRequestTenantContext definido. TenantId não resolvido.");
        return null;
    }

    /// <summary>
    /// Executa o sp_set_session_context para definir o TenantId na sessão SQL Server.
    /// </summary>
    private async Task SetTenantSessionContextAsync(DbConnection connection, int tenantId, CancellationToken cancellationToken)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "EXEC sp_set_session_context @key=N'TenantId', @value=@tenantId;";

            var param = cmd.CreateParameter();
            param.ParameterName = "@tenantId";
            param.Value = tenantId;
            cmd.Parameters.Add(param);

            await cmd.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogDebug("[RLS] SESSION_CONTEXT TenantId={TenantId} definido com sucesso.", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RLS] Falha ao definir SESSION_CONTEXT TenantId={TenantId}.", tenantId);
            throw;
        }
    }
}
