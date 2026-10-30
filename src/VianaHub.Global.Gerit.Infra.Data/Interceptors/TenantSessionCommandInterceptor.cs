using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Helpers;

namespace VianaHub.Global.Gerit.Infra.Data.Interceptors;

/// <summary>
/// Interceptor de comando responsável por garantir que o SESSION_CONTEXT do SQL Server
/// tenha o TenantId correto antes de cada execução de comando.
///
/// Necessário porque conexões do pool podem ser reutilizadas entre requests distintos.
/// A lógica de resolução do TenantId é idêntica à do TenantSessionConnectionInterceptor:
///   1. Usuário autenticado  ? claim 'tenant_id' do JWT
///   2. Usuário não autenticado (ex: login) ? IRequestTenantContext (populado pelo AppService a partir do body)
///   3. Nenhuma das anteriores ? não seta SESSION_CONTEXT; RLS bloqueia o acesso
///
/// IMPORTANTE: A aplicação NUNCA seta IsSuperAdmin no SESSION_CONTEXT.
/// O TenantId passado é sempre o do tenant autenticado ou do body da requisição.
/// </summary>
public class TenantSessionCommandInterceptor : DbCommandInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRequestTenantContext _requestTenantContext;
    private readonly ILogger<TenantSessionCommandInterceptor> _logger;

    public TenantSessionCommandInterceptor(
        IHttpContextAccessor httpContextAccessor,
        IRequestTenantContext requestTenantContext,
        ILogger<TenantSessionCommandInterceptor> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _requestTenantContext = requestTenantContext;
        _logger = logger;
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        await EnsureTenantSessionContextAsync(command, cancellationToken);
        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        await EnsureTenantSessionContextAsync(command, cancellationToken);
        return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await EnsureTenantSessionContextAsync(command, cancellationToken);
        return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <summary>
    /// Garante que o SESSION_CONTEXT da sessão SQL contenha o TenantId correto
    /// antes de qualquer comando ser executado.
    /// Evita recursão verificando se o próprio comando já é um sp_set_session_context.
    /// </summary>
    private async Task EnsureTenantSessionContextAsync(DbCommand command, CancellationToken cancellationToken)
    {
        // Evita recursão infinita: ignora comandos que já são sp_set_session_context
        if (command.CommandText is not null &&
            command.CommandText.IndexOf("sp_set_session_context", StringComparison.OrdinalIgnoreCase) >= 0)
            return;

        if (command.Connection is not SqlConnection sqlConnection)
            return;

        _logger.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=RlsCommandExecuting CommandType={CommandType}",
            LogValueSanitizer.Sanitize(_httpContextAccessor.HttpContext?.TraceIdentifier), command.CommandType);
        var tenantId = ResolveTenantId();
        _logger.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=RlsCommandTenantResolved TenantId={TenantId} Source={Source}",
            LogValueSanitizer.Sanitize(_httpContextAccessor.HttpContext?.TraceIdentifier), tenantId,
            tenantId is null ? "unresolved" : _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true ? "authenticated JWT claim" : "IRequestTenantContext");

        if (tenantId is null)
        {
            _logger.LogDebug("[RLS] TenantId não resolvido no interceptor de comando. SESSION_CONTEXT não será atualizado.");
            return;
        }

        await SetTenantSessionContextAsync(sqlConnection, tenantId.Value, command.Transaction, cancellationToken);
    }

    /// <summary>
    /// Resolve o TenantId para o request atual.
    /// Prioridade: claim JWT ? IRequestTenantContext (payload do body, ex: login)
    /// </summary>
    private int? ResolveTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            _logger.LogDebug("[RLS] Sem HttpContext no interceptor de comando. TenantId não pode ser resolvido.");
            return null;
        }

        var user = httpContext.User;

        // 1. Usuário autenticado: TenantId vem exclusivamente do claim do JWT
        if (user?.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = user.FindFirst("tenant_id")
                           ?? user.FindFirst("tenantId")
                           ?? user.FindFirst("tenant");

            if (tenantClaim is not null && int.TryParse(tenantClaim.Value, out var tenantFromJwt))
            {
                _logger.LogDebug("[RLS] TenantId resolvido via JWT claim no interceptor de comando: {TenantId}", tenantFromJwt);
                return tenantFromJwt;
            }

            _logger.LogWarning("[RLS] Usuário autenticado sem claim tenant_id válida no token (interceptor de comando).");
            return null;
        }

        // 2. Usuário não autenticado: TenantId vem do IRequestTenantContext (ex: login)
        if (_requestTenantContext.TenantId.HasValue)
        {
            _logger.LogDebug("[RLS] TenantId resolvido via IRequestTenantContext no interceptor de comando: {TenantId}",
                _requestTenantContext.TenantId.Value);
            return _requestTenantContext.TenantId.Value;
        }

        _logger.LogDebug("[RLS] Request não autenticado sem IRequestTenantContext definido (interceptor de comando). TenantId não resolvido.");
        return null;
    }

    /// <summary>
    /// Executa o sp_set_session_context para definir o TenantId na sessão SQL Server.
    /// Utiliza parâmetro para evitar SQL injection.
    /// </summary>
    private async Task SetTenantSessionContextAsync(SqlConnection connection, int tenantId, DbTransaction? transaction, CancellationToken cancellationToken)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "EXEC sp_set_session_context @key=N'TenantId', @value=@tenantId;";

            // Associa a transação ativa ao comando para evitar o erro
            // "BeginExecuteNonQuery requires the command to have a transaction
            //  when the connection assigned to the command is in a pending local transaction"
            if (transaction is SqlTransaction sqlTransaction)
                cmd.Transaction = sqlTransaction;

            var param = cmd.CreateParameter();
            param.ParameterName = "@tenantId";
            param.Value = tenantId;
            cmd.Parameters.Add(param);

            _logger.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=RlsCommandSessionSetting TenantId={TenantId}",
                LogValueSanitizer.Sanitize(_httpContextAccessor.HttpContext?.TraceIdentifier), tenantId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=RlsCommandSessionSet TenantId={TenantId}",
                LogValueSanitizer.Sanitize(_httpContextAccessor.HttpContext?.TraceIdentifier), tenantId);

            _logger.LogDebug("[RLS] SESSION_CONTEXT TenantId={TenantId} atualizado no interceptor de comando.", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RLS] Falha ao atualizar SESSION_CONTEXT TenantId={TenantId} no interceptor de comando.", tenantId);
            throw;
        }
    }
}
