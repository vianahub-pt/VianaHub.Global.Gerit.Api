namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade de tradução de um FileType por idioma (pt-PT, en-US, es-ES).
/// Tabela: dbo.FileTypeTranslations — PK surrogate (Id).
/// </summary>
public class FileTypeTranslationsEntity
{
    public int Id { get; private set; }
    public int FileTypeId { get; private set; }
    public string? LanguageCode { get; private set; }
    public string? Name { get; private set; }
    public string? Description { get; private set; }

    // Navigation Property
    public FileTypeEntity FileType { get; private set; } = null!;

    // Construtor protegido para o EF Core
    protected FileTypeTranslationsEntity() { }

    /// <summary>
    /// Construtor para criação de uma nova tradução de FileType
    /// </summary>
    public FileTypeTranslationsEntity(int fileTypeId, string? languageCode, string? name, string? description)
    {
        FileTypeId = fileTypeId;
        LanguageCode = languageCode;
        Name = name;
        Description = description;
    }

    public void Update(string? name, string? description)
    {
        Name = name;
        Description = description;
    }
}
