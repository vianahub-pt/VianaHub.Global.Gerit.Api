using VianaHub.Global.Gerit.Api.Filters;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Helpers;

/// <summary>
/// Métodos de extensão para adicionar autorização customizada aos endpoints.
/// Valida perfis (roles), recursos e ações baseado no token JWT.
/// </summary>
public static class EndpointAuthorizationExtensions
{
    /// <summary>
    /// Adiciona autorização customizada ao endpoint validando perfis, recurso e ação.
    /// </summary>
    /// <param name="builder">Builder do endpoint</param>
    /// <param name="allowedRoles">Perfis permitidos separados por vírgula (ex: "admin,manager,operator")</param>
    /// <param name="resource">Nome do recurso (ex: "Actions", "Resources", "Permissions")</param>
    /// <param name="action">Ação permitida (ex: "Read", "Write", "Delete")</param>
    /// <returns>Builder do endpoint com autorização configurada</returns>
    /// <example>
    /// <code>
    /// .CustomAuthorize("admin,manager,operator", "Actions", "Read")
    /// .CustomAuthorize("admin", "Users", "Delete")
    /// </code>
    /// </example>
    public static RouteHandlerBuilder CustomAuthorize(
        this RouteHandlerBuilder builder,
        string allowedRoles,
        string? resource,
        string? action)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var services = context.HttpContext.RequestServices;
            var filter = new AuthorizationFilter(
                allowedRoles,
                resource,
                action,
                services.GetRequiredService<ILogger<AuthorizationFilter>>(),
                services.GetRequiredService<ILocalizationService>());

            return await filter.InvokeAsync(context, next);
        });
    }

    /// <summary>
    /// Adiciona autorização customizada ao endpoint validando apenas perfis.
    /// </summary>
    /// <param name="builder">Builder do endpoint</param>
    /// <param name="allowedRoles">Perfis permitidos separados por vírgula (ex: "admin,manager")</param>
    /// <returns>Builder do endpoint com autorização configurada</returns>
    /// <example>
    /// <code>
    /// .CustomAuthorize("admin,manager")
    /// </code>
    /// </example>
    public static RouteHandlerBuilder CustomAuthorize(
        this RouteHandlerBuilder builder,
        string allowedRoles)
    {
        return builder.CustomAuthorize(allowedRoles, null, null);
    }

    /// <summary>
    /// Adiciona autorização customizada ao endpoint validando perfis e recurso.
    /// </summary>
    /// <param name="builder">Builder do endpoint</param>
    /// <param name="allowedRoles">Perfis permitidos separados por vírgula</param>
    /// <param name="resource">Nome do recurso</param>
    /// <returns>Builder do endpoint com autorização configurada</returns>
    public static RouteHandlerBuilder CustomAuthorize(
        this RouteHandlerBuilder builder,
        string allowedRoles,
        string resource)
    {
        return builder.CustomAuthorize(allowedRoles, resource, null);
    }

    /// <summary>
    /// Adiciona validação de tenant ao endpoint.
    /// Valida se o TenantId na URL corresponde ao TenantId do usuário autenticado.
    /// IMPORTANTE: Deve ser chamado ANTES de CustomAuthorize para garantir isolamento multi-tenant.
    /// </summary>
    /// <param name="builder">Builder do endpoint</param>
    /// <returns>Builder do endpoint com validação de tenant configurada</returns>
    /// <example>
    /// <code>
    /// .ValidateTenant()
    /// .CustomAuthorize("admin,manager", "Actions", "Create")
    /// </code>
    /// </example>
    public static RouteHandlerBuilder ValidateTenant(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter<UserValidationFilter>();
    }
}
