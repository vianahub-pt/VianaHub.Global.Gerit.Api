namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade de tradução de um DocumentType por idioma (pt-PT, en-US, es-ES).
/// Tabela: dbo.DocumentTypeTranslations — PK surrogate (Id).
/// </summary>
public class DocumentTypeTranslationsEntity
{
    public int Id { get; private set; }
    public int DocumentTypeId { get; private set; }
    public string? LanguageCode { get; private set; }
    public string? Name { get; private set; }
    public string? Description { get; private set; }

    // Navigation Property
    public DocumentTypeEntity DocumentType { get; private set; } = null!;

    // Construtor protegido para o EF Core
    protected DocumentTypeTranslationsEntity() { }

    /// <summary>
    /// Construtor para criação de uma nova tradução de DocumentType
    /// </summary>
    public DocumentTypeTranslationsEntity(int documentTypeId, string? languageCode, string? name, string? description)
    {
        DocumentTypeId = documentTypeId;
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
