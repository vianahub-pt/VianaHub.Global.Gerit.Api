using VianaHub.Global.Gerit.Domain.Base;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade que representa um Tipo de Party (Pessoa Física / Jurídica).
/// Referenciada como value object / owned entity por agregados do domínio Business.
/// </summary>
public class PartyTypeEntity : Entity
{
    public new byte Id { get; private set; }
    public string? Code { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public ICollection<PartyTypeTranslationsEntity> Translations { get; private set; } = new List<PartyTypeTranslationsEntity>();

    // Construtor protegido para o EF Core
    protected PartyTypeEntity() { }

    /// <summary>
    /// Construtor para criação de um novo PartyType
    /// </summary>
    public PartyTypeEntity(string? code, int createdBy)
    {
        Code = code;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        Translations = new List<PartyTypeTranslationsEntity>();
    }

    public void MarkModified(int modifiedBy)
    {
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
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
