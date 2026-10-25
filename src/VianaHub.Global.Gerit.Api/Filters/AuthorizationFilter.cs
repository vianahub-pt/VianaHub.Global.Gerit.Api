using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Linq;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Filters;

/// <summary>
/// Filtro de autorização customizado que valida se o usuário tem permissão para acessar o recurso.
/// Verifica os perfis (roles) do usuário no token JWT e valida permissões de recurso e ação
/// diretamente das claims do token (stateless), sem necessidade de consultar o banco de dados.
/// </summary>
public class AuthorizationFilter : IEndpointFilter
{
    private readonly string[] _allowedRoles;
    private readonly string? _resource;
    private readonly string? _action;
    private readonly ILogger<AuthorizationFilter> _logger;
    private readonly ILocalizationService _localization;

    /// <summary>
    /// Inicializa uma nova instância do <see cref="AuthorizationFilter"/>.
    /// </summary>
    /// <param name="allowedRoles">Roles permitidas (separadas por vírgula)</param>
    /// <param name="resource">Nome do recurso (ex: "Actions", "Resources", "Permissions")</param>
    /// <param name="action">Ação permitida (ex: "Read", "Write", "Delete")</param>
    /// <param name="logger">Logger</param>
    /// <param name="localization">Serviço de localização para mensagens</param>
    public AuthorizationFilter(
        string allowedRoles,
        string? resource,
        string? action,
        ILogger<AuthorizationFilter> logger,
        ILocalizationService localization)
    {
        _allowedRoles = allowedRoles?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(r => r.Trim().ToLower())
            .ToArray() ?? Array.Empty<string>();
        _resource = resource;
        _action = action;
        _logger = logger;
        _localization = localization;
    }

    /// <summary>
    /// Executa a validação de autorização verificando:
    /// 1. Se o usuário está autenticado
    /// 2. Se possui uma das roles permitidas
    /// 3. Se tem permissão para o recurso e ação especificados (via claims do token)
    /// </summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User;

        _logger.LogDebug("AuthorizationFilter executando para Resource: {Resource}, ActionEntity: {ActionEntity}", _resource, _action);

        // Verificar se o usuário está autenticado
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("Acesso negado: Usuário não autenticado");
            return Results.Unauthorized();
        }

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst("user_id")?.Value;

        var userName = user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst("name")?.Value
            ?? user.FindFirst("preferred_username")?.Value;

        var userRoles = user.FindAll(ClaimTypes.Role)
            .Select(c => c.Value.ToLower())
            .ToList();

        if (!userRoles.Any())
        {
            // Tentar claims alternativas para roles
            userRoles = user.FindAll("role")
                .Select(c => c.Value.ToLower())
                .ToList();
        }

        _logger.LogInformation("Usuário: {UserId} ({UserName}), Roles: [{Roles}]",
            userId, userName, string.Join(", ", userRoles));

        // Verificar se o usuário possui alguma das roles permitidas (se configurado)
        if (_allowedRoles.Any() && !_allowedRoles.Any(allowedRole => userRoles.Contains(allowedRole)))
        {
            _logger.LogWarning(
                "Acesso negado: Usuário {UserId} não possui nenhuma das roles permitidas. " +
                "Roles do usuário: [{UserRoles}], Roles permitidas: [{AllowedRoles}]",
                userId,
                string.Join(", ", userRoles),
                string.Join(", ", _allowedRoles));

            var notify = httpContext.RequestServices.GetService<INotify>();
            if (notify != null)
            {
                // Sempre adicionar chave de tradução, nunca texto literal
                notify.Add("Api.Authorization.Forbidden.Roles", (int)HttpStatusCode.Forbidden);
            }

            var titleKey = "Api.Authorization.AccessDenied";
            var detailKey = "Api.Authorization.Forbidden.Roles.Detail";

            var errorResponse = new ErrorResponse(_localization?.GetMessage(titleKey) ?? titleKey);
            errorResponse.AddError("Authorization", _localization?.GetMessage(detailKey) ?? detailKey);

            return Results.Json(errorResponse, statusCode: (int)HttpStatusCode.Forbidden);
        }


        // Verificar permissões específicas do recurso e ação (se configurado)
        // Suporta formato antigo: várias claims 'permission': 'resource:action'
        // E novo formato único 'permissions' cujo valor é um JSON com chaves por recurso
        if (!string.IsNullOrEmpty(_resource) && !string.IsNullOrEmpty(_action))
        {
            var hasPermission = ValidateResourcePermissionFromClaims(user, _resource, _action, httpContext);

            if (!hasPermission)
            {
                _logger.LogWarning(
                    "Acesso negado: Usuário {UserId} não tem permissão para {ActionEntity} em {Resource}",
                    userId, _action, _resource);

                var notify = httpContext.RequestServices.GetService<INotify>();
                if (notify != null)
                {
                    // Adicionar chave de tradução em notify
                    notify.Add("Api.Authorization.Forbidden.Permission", (int)HttpStatusCode.Forbidden);
                }

                var titleKey = "Api.Authorization.AccessDenied";
                var detailKey = "Api.Authorization.Forbidden.Permission.Detail";

                var errorResponse = new ErrorResponse(_localization?.GetMessage(titleKey) ?? titleKey);
                errorResponse.AddError("Authorization", _localization?.GetMessage(detailKey) ?? detailKey);

                return Results.Json(errorResponse, statusCode: (int)HttpStatusCode.Forbidden);
            }
        }

        _logger.LogInformation("Autorização concedida para usuário {UserId}", userId);
        return await next(context);
    }

    /// <summary>
    /// Valida se o usuário tem permissão específica para o recurso e ação
    /// verificando diretamente as claims do token JWT (stateless).
    /// 
    /// Esta abordagem segue as melhores práticas de sistemas IAM modernos como
    /// Auth0, Okta, Azure AD e Keycloak, onde as permissões são incluídas no token
    /// durante a autenticação, eliminando a necessidade de consultas ao banco de dados
    /// a cada requisição.
    /// 
    /// Benefícios:
    /// - Performance até 1000x melhor (sem I/O de banco de dados)
    /// - Escalabilidade horizontal simples
    /// - Latência mínima (~0.1ms vs ~50-200ms)
    /// - Alinhado com padrões OAuth2/OIDC
    /// </summary>
    /// <param name="user">ClaimsPrincipal do usuário autenticado</param>
    /// <param name="resource">Nome do recurso (ex: "users", "resources", "actions")</param>
    /// <param name="action">Nome da ação (ex: "read", "write", "delete")</param>
    /// <returns>True se o usuário tem a permissão, false caso contrário</returns>
    private bool ValidateResourcePermissionFromClaims(
        ClaimsPrincipal user,
        string resource,
        string action,
        HttpContext httpContext)
    {
        // Formato padrão antigo: "resource:action" (case-insensitive)
        var requiredPermission = $"{resource}:{action}".ToLower();

        // 1) Tentar formato antigo: várias claims 'permission'
        var userPermissions = user.FindAll("permission")
            .Select(c => c.Value.ToLower())
            .ToHashSet();

        if (userPermissions.Contains(requiredPermission))
        {
            _logger.LogDebug(
                "Permissão '{Permission}' encontrada nas claims do token (formato antigo)",
                requiredPermission);
            return true;
        }

        // 2) Tentar novo formato: claim único 'permissions' com JSON
        // Cachear o dicionário parseado no HttpContext.Items para este request
        const string cacheKey = "__permissions_parsed__";
        if (!httpContext.Items.TryGetValue(cacheKey, out var cached))
        {
            // Encontrar claim 'permissions'
            var permClaim = user.FindFirst("permissions")?.Value;
            if (string.IsNullOrWhiteSpace(permClaim))
            {
                _logger.LogDebug("Nenhuma claim 'permission' nem 'permissions' encontrada para o usuário");
                return false;
            }

            try
            {
                // Desserializar para Dictionary<string,List<string>>
                var dict = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(permClaim);
                if (dict == null)
                {
                    _logger.LogWarning("Claim 'permissions' presente mas não pôde ser desserializada como JSON");
                    httpContext.Items[cacheKey] = new Dictionary<string, HashSet<string>>();
                }
                else
                {
                    var normalized = dict.ToDictionary(kvp => kvp.Key.ToLower(), kvp => kvp.Value.Select(x => x.ToLower()).ToHashSet());
                    httpContext.Items[cacheKey] = normalized;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao desserializar claim 'permissions'");
                httpContext.Items[cacheKey] = new Dictionary<string, HashSet<string>>();
            }

            cached = httpContext.Items[cacheKey];
        }

        if (cached is Dictionary<string, HashSet<string>> permissionsDict)
        {
            var resourceKey = resource.ToLower();
            if (permissionsDict.TryGetValue(resourceKey, out var actions))
            {
                var actionKey = action.ToLower();
                var has = actions.Contains(actionKey);
                if (has)
                {
                    _logger.LogDebug("Permissão '{Permission}' encontrada no claim 'permissions' (formato agrupado)", requiredPermission);
                    return true;
                }
                else
                {
                    _logger.LogDebug("Permissão '{Permission}' NÃO encontrada no claim 'permissions' (ações disponíveis: {Actions})", requiredPermission, string.Join(", ", actions));
                    return false;
                }
            }
        }

        _logger.LogDebug("Permissão '{Permission}' NÃO encontrada nas claims do token (nenhum formato)", requiredPermission);
        return false;
    }
}
