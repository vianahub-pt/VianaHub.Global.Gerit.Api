using VianaHub.Global.Gerit.Domain.Base;

namespace VianaHub.Global.Gerit.Domain.Entities.Identity;

/// <summary>
/// Entidade que representa uma a��o poss�vel no sistema
/// </summary>
public class ActionEntity : Entity
{
    public string? Name { get; private set; }
    public string? Description { get; set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    private readonly List<RolePermissionsEntity> _rolePermissions = new();
    public IReadOnlyCollection<RolePermissionsEntity> RolePermissions => _rolePermissions.AsReadOnly();

    // Construtor protegido para o EF Core
    protected ActionEntity() { }

    /// <summary>
    /// Construtor para cria��o de uma nova a��o
    /// </summary>
    public ActionEntity(string? name, string? description, int createdBy)
    {
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
