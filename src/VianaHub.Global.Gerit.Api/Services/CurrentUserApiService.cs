using System.Security.Claims;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.ReadModels;

namespace VianaHub.Global.Gerit.Api.Services;

/// <summary>
/// Serviço para obter informações do usuário atual a partir do contexto HTTP.
/// Implementa ICurrentUserService para fornecer acesso às informações do usuário autenticado.
/// </summary>
public class CurrentUserApiService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância do <see cref="CurrentUserApiService"/>.
    /// </summary>
    /// <param name="httpContextAccessor">Acessor do contexto HTTP.</param>
    public CurrentUserApiService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Obtém o ID do usuário logado
    /// Ordem de busca: 1) Token JWT (claims: sub, nameid, userId) 2) Header x-user-id
    /// </summary>
    public int GetUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
            throw new Exception("HTTP context is not available.");

        // Tenta buscar no token JWT
        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            // Busca nas claims padrões do JWT
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value
                      ?? user.FindFirst("userId")?.Value
                      ?? user.FindFirst("uid")?.Value;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                return int.Parse(userId);
            }
        }

        // Se não encontrou no token, busca no header
        if (httpContext.Request.Headers.TryGetValue("x-user-id", out var headerUserId))
        {
            return int.Parse(headerUserId.ToString());
        }
        return 0;
    }

    /// <summary>
    /// Obtém o TenantId do usuário logado
    /// Ordem de busca: 1) Token JWT (claims: tenant_id, tenantId) 2) Header x-tenant-id
    /// </summary>
    public int GetTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
            throw new Exception("HTTP context is not available.");

        // Tenta buscar no token JWT
        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            // Busca nas claims padrões do JWT
            var tenantId = user.FindFirst("tenant_id")?.Value
                        ?? user.FindFirst("tenantId")?.Value
                        ?? user.FindFirst("TenantId")?.Value;

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return int.Parse(tenantId);
            }
        }

        // Se não encontrou no token, busca no header
        if (httpContext.Request.Headers.TryGetValue("x-tenant-id", out var headerTenantId))
        {
            return int.Parse(headerTenantId.ToString());
        }

        // TODO: Quando a emissão de tokens estiver implementada, este valor default não será mais necessário
        // Por enquanto, retorna 1 para desenvolvimento
        return 1;
    }

    /// <summary>
    /// Obtém o nome do usuário logado
    /// Ordem de busca: 1) Token JWT (claims: name, username) 2) Header x-user-name
    /// </summary>
    public string GetUserName()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return string.Empty;

        // Tenta buscar no token JWT
        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var userName = user.FindFirst(ClaimTypes.Name)?.Value
                        ?? user.FindFirst("name")?.Value
                        ?? user.FindFirst("username")?.Value
                        ?? user.FindFirst("preferred_username")?.Value;

            if (!string.IsNullOrWhiteSpace(userName))
                return userName;
        }

        // Se não encontrou no token, busca no header
        if (httpContext.Request.Headers.TryGetValue("x-user-name", out var headerUserName))
        {
            return headerUserName.ToString();
        }

        return string.Empty;
    }

    /// <summary>
    /// Obtém o email do usuário logado
    /// Ordem de busca: 1) Token JWT (claim: email) 2) Header x-user-email
    /// </summary>
    public string GetUserEmail()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return string.Empty;

        // Tenta buscar no token JWT
        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var email = user.FindFirst(ClaimTypes.Email)?.Value
                     ?? user.FindFirst("email")?.Value;

            if (!string.IsNullOrWhiteSpace(email))
                return email;
        }

        // Se não encontrou no token, busca no header
        if (httpContext.Request.Headers.TryGetValue("x-user-email", out var headerEmail))
        {
            return headerEmail.ToString();
        }

        return string.Empty;
    }

    /// <summary>
    /// Verifica se há um usuário autenticado
    /// </summary>
    public bool IsAuthenticated()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return false;

        return httpContext.User?.Identity?.IsAuthenticated == true;
    }

    /// <summary>
    /// Obtém o identificador do usuário para usar em CreatedBy/ModifiedBy
    /// Ordem de preferência: 1) Email 2) Nome de usuário 3) ID do usuário 4) "system"
    /// </summary>
    public string GetUserIdentifier()
    {
        var email = GetUserEmail();
        if (!string.IsNullOrWhiteSpace(email))
            return email;

        var userName = GetUserName();
        if (!string.IsNullOrWhiteSpace(userName))
            return userName;

        var userId = GetUserId();
        if (userId != 0)
            return userId.ToString();

        return "system";
    }

    /// <summary>
    /// Obtém o objeto CurrentUser completo
    /// </summary>
    public CurrentUserContext GetUser()
    {
        return new CurrentUserContext
        {
            UserId = GetUserId(),
            UserName = GetUserName(),
            Email = GetUserEmail()
        };
    }

    /// <summary>
    /// Obtém o endereço IP do usuário
    /// </summary>
    public string GetUserIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return "Unknown";

        // Verifica se está atrás de um proxy
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Obtém o User Agent do navegador
    /// </summary>
    public string GetUserAgent()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return "Unknown";

        return httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
    }
}
