using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Identity;

/// <summary>
/// Entidade que representa uma Role (papel) no sistema RBAC
/// </summary>
public class RoleEntity : Entity
{
    public int TenantId { get; private set; }
    public string? Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public TenantEntity? Tenant { get; private set; }

    private readonly List<RolePermissionsEntity> _permissions = new();
    public IReadOnlyCollection<RolePermissionsEntity> Permissions => _permissions.AsReadOnly();

    private readonly List<UserRolesEntity> _userRoles = new();
    public IReadOnlyCollection<UserRolesEntity> UserRoles => _userRoles.AsReadOnly();

    // Construtor protegido para o EF Core
    protected RoleEntity() { }

    /// <summary>
    /// Construtor para cria��o de uma nova Role
    /// </summary>
    public RoleEntity(int tenantId, string? name, string? description, int createdBy)
    {
        TenantId = tenantId;
        Name = name;
        Description = description;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
    }

    public void Update(string? description, int modifiedBy)
    {
        Description = description;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Activate(int? modifiedBy)
    {
        IsActive = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Deactivate(int? modifiedBy)
    {
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Delete(int? modifiedBy)
    {
        IsDeleted = true;
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

}
