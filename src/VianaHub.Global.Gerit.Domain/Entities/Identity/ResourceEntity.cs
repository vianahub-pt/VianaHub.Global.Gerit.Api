using VianaHub.Global.Gerit.Domain.Base;

namespace VianaHub.Global.Gerit.Domain.Entities.Identity;

/// <summary>
/// Entidade que representa um recurso do sistema
/// </summary>
public class ResourceEntity : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    private readonly List<RolePermissionsEntity> _rolePermissions = new();
    public IReadOnlyCollection<RolePermissionsEntity> RolePermissions => _rolePermissions.AsReadOnly();

    // Construtor protegido para o EF Core
    protected ResourceEntity() { }

    /// <summary>
    /// Construtor para cria��o de um novo recurso
    /// </summary>
    public ResourceEntity(string? name, string? description, int createdBy)
    {
        Name = name?.Trim() ?? string.Empty;
        Description = description?.Trim() ?? string.Empty;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
    }

    public void Update(string? description, int modifiedBy)
    {
        Description = description?.Trim() ?? string.Empty;
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
