using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade que representa um anexo (ficheiro) relacionado a uma visita
/// Suporta integração com S3 para armazenamento de ficheiros
/// </summary>
public class VisitAttachmentsEntity : Entity
{
    public int TenantId { get; private set; }
    public int FileTypeId { get; private set; }
    public int VisitId { get; private set; }
    public Guid PublicId { get; private set; }
    public string? S3Key { get; private set; }
    public string? FileName { get; private set; }
    public long FileSizeBytes { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public TenantEntity? Tenant { get; private set; }
    public FileTypeEntity? FileType { get; private set; }
    public VisitEntity? Visit { get; private set; }

    protected VisitAttachmentsEntity() { }

    /// <summary>
    /// Construtor para criação de um novo anexo de visita
    /// </summary>
    public VisitAttachmentsEntity(int tenantId, int fileTypeId, int visitId,
        string? s3Key, string? fileName, long fileSizeBytes, int displayOrder, bool isPrimary, int createdBy)
    {
        if (fileSizeBytes <= 0)
            throw new ArgumentException("Domain.VisitAttachment.FileSizeInvalid");

        if (string.IsNullOrWhiteSpace(s3Key))
            throw new ArgumentNullException(nameof(s3Key), "Domain.VisitAttachment.S3KeyRequired");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName), "Domain.VisitAttachment.FileNameRequired");

        TenantId = tenantId;
        FileTypeId = fileTypeId;
        VisitId = visitId;
        PublicId = Guid.NewGuid();
        S3Key = s3Key;
        FileName = fileName;
        FileSizeBytes = fileSizeBytes;
        DisplayOrder = displayOrder;
        IsPrimary = isPrimary;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string? fileName, int displayOrder, bool isPrimary, int modifiedBy)
    {
        FileName = fileName;
        DisplayOrder = displayOrder;
        IsPrimary = isPrimary;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void UpdateDisplayOrder(int displayOrder, int modifiedBy)
    {
        DisplayOrder = displayOrder;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void SetAsPrimary(int modifiedBy)
    {
        IsPrimary = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void RemovePrimary(int modifiedBy)
    {
        IsPrimary = false;
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

    /// <summary>
    /// Obtém a URL de download segura usando o PublicId
    /// </summary>
    public string? GetDownloadUrl(string baseUrl) => $"{baseUrl}/attachments/{PublicId}";

    /// <summary>
    /// Formata o tamanho do ficheiro de forma legível
    /// </summary>
    public string? GetFormattedFileSize()
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = FileSizeBytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
