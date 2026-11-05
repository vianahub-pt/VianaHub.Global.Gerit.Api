using VianaHub.Global.Gerit.Domain.Base;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade que representa um Tipo de Documento (BI, Passaporte, NIF, etc.).
/// Não é Aggregate Root.
/// </summary>
public class DocumentTypeEntity : Entity
{
    public string? Code { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public ICollection<DocumentTypeTranslationsEntity> Translations { get; private set; } = new List<DocumentTypeTranslationsEntity>();

    // Construtor protegido para o EF Core
    protected DocumentTypeEntity() { }

    /// <summary>
    /// Construtor para criação de um novo Tipo de Documento
    /// </summary>
    public DocumentTypeEntity(string? code, int createdBy)
    {
        Code = code;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        Translations = new List<DocumentTypeTranslationsEntity>();
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
