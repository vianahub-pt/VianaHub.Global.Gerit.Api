using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Identity;

/// <summary>
/// Entidade que representa a rela��o entre User e Role
/// </summary>
public class UserRolesEntity
{
    public int Id { get; private set; }
    public int TenantId { get; private set; }
    public int UserId { get; private set; }
    public int RoleId { get; private set; }

    // Navigation Properties
    public TenantEntity? Tenant { get; private set; }
    public UserEntity? User { get; private set; }
    public RoleEntity? Role { get; private set; }

    // Construtor protegido para o EF Core
    protected UserRolesEntity() { }

    /// <summary>
    /// Construtor para cria��o de uma nova rela��o User-Role
    /// </summary>
    public UserRolesEntity(int tenantId, int userId, int roleId)
    {
        TenantId = tenantId;
        UserId = userId;
        RoleId = roleId;
    }
}
