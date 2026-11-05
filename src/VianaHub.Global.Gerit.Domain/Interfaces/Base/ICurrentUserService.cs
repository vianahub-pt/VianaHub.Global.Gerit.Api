using VianaHub.Global.Gerit.Domain.ReadModels;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Base;

/// <summary>
/// Serviço para obter informações do usuário logado a partir do token JWT ou headers da requisição.
/// Busca primeiro no token JWT (claims), depois em headers HTTP (x-user-*).
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Obtém o ID do usuário logado.
    /// Busca primeiro no token JWT (claims: 'sub', 'nameid', 'userId'), depois no header 'x-user-id'.
    /// </summary>
    /// <returns>ID do usuário ou 0 se não encontrado</returns>
    int GetUserId();

    /// <summary>
    /// Obtém o ID do Tenant do usuário logado.
    /// Busca primeiro no token JWT (claims: 'tenant_id', 'tenantId'), depois no header 'x-tenant-id'.
    /// </summary>
    /// <returns>ID do tenant ou valor default se não encontrado</returns>
    int GetTenantId();

    /// <summary>
    /// Obtém o nome do usuário logado.
    /// Busca primeiro no token JWT (claims: 'name', 'username'), depois no header 'x-user-name'.
    /// </summary>
    /// <returns>Nome do usuário ou string vazia se não encontrado</returns>
    string GetUserName();

    /// <summary>
    /// Obtém o email do usuário logado.
    /// Busca primeiro no token JWT (claim: 'email'), depois no header 'x-user-email'.
    /// </summary>
    /// <returns>Email do usuário ou string vazia se não encontrado</returns>
    string GetUserEmail();

    /// <summary>
    /// Verifica se há um usuário autenticado.
    /// </summary>
    /// <returns>True se houver usuário autenticado, false caso contrário</returns>
    bool IsAuthenticated();

    /// <summary>
    /// Verifica se o usuário autenticado pertence ao perfil informado.
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// Obtém o identificador do usuário para usar em campos de auditoria (CreatedBy/ModifiedBy).
    /// Ordem de preferência: 1) Email 2) Nome de usuário 3) ID do usuário 4) "system"
    /// </summary>
    /// <returns>Identificador do usuário ou "system" se não encontrado</returns>
    string GetUserIdentifier();

    /// <summary>
    /// Obtém o objeto CurrentUser completo com todas as informações do usuário autenticado.
    /// </summary>
    /// <returns>Objeto CurrentUser contendo UserId, UserName e Email</returns>
    CurrentUserContext GetUser();

    /// <summary>
    /// Obtém o endereço IP do usuário.
    /// </summary>
    /// <returns>Endereço IP ou "Unknown" se não encontrado</returns>
    string GetUserIpAddress();

    /// <summary>
    /// Obtém o User Agent do navegador.
    /// </summary>
    /// <returns>User Agent ou "Unknown" se não encontrado</returns>
    string GetUserAgent();
}
