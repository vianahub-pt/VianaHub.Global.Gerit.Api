using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Identity;

/// <summary>
/// Entidade que representa a permiss�o de uma Role sobre um Resource com uma ActionEntity
/// </summary>
public class RolePermissionsEntity
{
    public int Id { get; private set; }
    public int TenantId { get; private set; }
    public int RoleId { get; private set; }
    public int ResourceId { get; private set; }
    public int ActionId { get; private set; }

    // Navigation Properties
    public TenantEntity? Tenant { get; private set; }
    public RoleEntity? Role { get; private set; }
    public ResourceEntity? Resource { get; private set; }
    public ActionEntity? Action { get; private set; }

    // Construtor protegido para o EF Core
    protected RolePermissionsEntity() { }

    /// <summary>
    /// Construtor para cria��o de uma nova permiss�o de Role
    /// </summary>
    public RolePermissionsEntity(int tenantId, int roleId, int resourceId, int actionId)
    {
        TenantId = tenantId;
        RoleId = roleId;
        ResourceId = resourceId;
        ActionId = actionId;
    }
}
