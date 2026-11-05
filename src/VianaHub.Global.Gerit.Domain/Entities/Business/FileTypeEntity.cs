using VianaHub.Global.Gerit.Domain.Base;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

public class FileTypeEntity : Entity
{
    public string? MimeType { get; private set; }
    public string? Extension { get; private set; }
    public string? Code { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public ICollection<FileTypeTranslationsEntity> Translations { get; private set; } = new List<FileTypeTranslationsEntity>();

    // Construtor protegido para o EF Core
    protected FileTypeEntity() { }

    public FileTypeEntity(string? mimeType, string? extension, int createdBy)
        : this(mimeType, extension, code: null, createdBy)
    {
    }

    /// <summary>
    /// Construtor completo para criação de um novo FileType, incluindo o Code de identificação.
    /// </summary>
    public FileTypeEntity(string? mimeType, string? extension, string? code, int createdBy)
    {
        MimeType = mimeType;
        Extension = extension;
        Code = code;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        Translations = new List<FileTypeTranslationsEntity>();
    }

    public void Update(string? mimeType, string? extension, int modifiedBy)
    {
        MimeType = mimeType;
        Extension = extension;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
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
