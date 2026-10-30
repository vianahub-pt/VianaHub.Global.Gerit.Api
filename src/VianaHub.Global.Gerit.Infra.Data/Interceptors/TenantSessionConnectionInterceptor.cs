using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Helpers;

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
        _logger.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=RlsConnectionOpened", LogValueSanitizer.Sanitize(_httpContextAccessor.HttpContext?.TraceIdentifier));
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);

        if (connection is not SqlConnection)
            return;

        var tenantId = ResolvetenantId();
        _logger.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=RlsConnectionTenantResolved TenantId={TenantId} Source={Source}",
            LogValueSanitizer.Sanitize(_httpContextAccessor.HttpContext?.TraceIdentifier), tenantId,
            tenantId is null ? "unresolved" : _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true ? "authenticated JWT claim" : "IRequestTenantContext");

        if (tenantId is null)
        {
            _logger.LogDebug("[RLS] TenantId não resolvido. SESSION_CONTEXT não será definido. RLS bloqueará o acesso.");
            return;
        }

        await SetTenantSessionContextAsync(connection, tenantId.Value, cancellationToken);
    }

    /// <summary>
    /// Resolve o TenantId para o request atual.
    /// Prioridade: claim JWT → IRequestTenantContext (payload do body, ex: login)
    /// </summary>
    private int? ResolvetenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            _logger.LogDebug("[RLS] Sem HttpContext. TenantId não pode ser resolvido.");
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

            _logger.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=RlsConnectionSessionSetting TenantId={TenantId}",
                LogValueSanitizer.Sanitize(_httpContextAccessor.HttpContext?.TraceIdentifier), tenantId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=RlsConnectionSessionSet TenantId={TenantId}",
                LogValueSanitizer.Sanitize(_httpContextAccessor.HttpContext?.TraceIdentifier), tenantId);

            _logger.LogDebug("[RLS] SESSION_CONTEXT TenantId={TenantId} definido com sucesso.", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RLS] Falha ao definir SESSION_CONTEXT TenantId={TenantId}.", tenantId);
            throw;
        }
    }
}
