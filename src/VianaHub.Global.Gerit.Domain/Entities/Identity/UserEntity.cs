using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Identity;

/// <summary>
/// Entidade que representa um usu�rio do sistema
/// </summary>
public class UserEntity : Entity
{
    public int TenantId { get; private set; }
    public string? Name { get; private set; }
    public string? Email { get; private set; }
    public string? NormalizedEmail { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool PhoneNumberConfirmed { get; private set; }
    public DateTime? LastAccessAt { get; private set; }
    public string? PasswordHash { get; private set; }
    public string? UrlImage { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public TenantEntity? Tenant { get; private set; }

    private readonly List<UserRolesEntity> _userRoles = new();
    public IReadOnlyCollection<UserRolesEntity> UserRoles => _userRoles.AsReadOnly();

    // Construtor protegido para o EF Core
    protected UserEntity() { }

    /// <summary>
    /// Construtor para cria��o de um novo usu�rio
    /// </summary>
    public UserEntity(int tenantId, string? name, string? email, string? passwordHash, string? phoneNumber, int createdBy, string? urlImage = null)
    {
        TenantId = tenantId;
        Name = name;
        Email = email;
        NormalizedEmail = email?.ToUpperInvariant();
        PasswordHash = passwordHash;
        PhoneNumber = phoneNumber;
        UrlImage = urlImage;
        EmailConfirmed = false;
        PhoneNumberConfirmed = false;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
    }

    public void Update(string? name, string? phoneNumber, int modifiedBy)
        => Update(name, Email, phoneNumber, UrlImage, modifiedBy);

    public void Update(string? name, string? email, string? phoneNumber, string? urlImage, int modifiedBy)
    {
        Name = name;
        var normalizedEmail = email?.ToUpperInvariant();
        if (!string.Equals(NormalizedEmail, normalizedEmail, StringComparison.Ordinal))
            EmailConfirmed = false;
        Email = email;
        NormalizedEmail = normalizedEmail;
        if (!string.Equals(PhoneNumber, phoneNumber, StringComparison.Ordinal))
            PhoneNumberConfirmed = false;
        PhoneNumber = phoneNumber;
        UrlImage = urlImage;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void UpdatePassword(string? passwordHash, int modifiedBy)
    {
        PasswordHash = passwordHash;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void ConfirmEmail(int modifiedBy)
    {
        EmailConfirmed = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void ConfirmPhoneNumber(int modifiedBy)
    {
        PhoneNumberConfirmed = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void UpdateLastAccess()
    {
        LastAccessAt = DateTime.UtcNow;
    }

    public void Activate(int modifiedBy)
    {
        IsActive = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Deactivate(int modifiedBy)
    {
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Delete(int modifiedBy)
    {
        IsDeleted = true;
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

}
