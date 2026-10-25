namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade de tradução de um AcquisitionSourceType por idioma (pt-PT, en-US, es-ES).
/// Tabela: dbo.AcquisitionSourceTypeTranslations — PK surrogate (Id).
/// </summary>
public class AcquisitionSourceTypeTranslationsEntity
{
    public int Id { get; private set; }
    public int AcquisitionSourceTypeId { get; private set; }
    public string? LanguageCode { get; private set; }
    public string? Name { get; private set; }
    public string? Description { get; private set; }

    // Navigation Property
    public AcquisitionSourceTypeEntity AcquisitionSourceType { get; private set; } = null!;

    // Construtor protegido para o EF Core
    protected AcquisitionSourceTypeTranslationsEntity() { }

    /// <summary>
    /// Construtor para criação de uma nova tradução de AcquisitionSourceType
    /// </summary>
    public AcquisitionSourceTypeTranslationsEntity(int acquisitionSourceTypeId, string? languageCode, string? name, string? description)
    {
        AcquisitionSourceTypeId = acquisitionSourceTypeId;
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
