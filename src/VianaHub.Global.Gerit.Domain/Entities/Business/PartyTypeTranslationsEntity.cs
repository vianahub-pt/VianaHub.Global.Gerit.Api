namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade de tradução de um PartyType por idioma (pt-PT, en-US, es-ES).
/// Tabela: dbo.PartyTypeTranslations — PK surrogate (Id).
/// </summary>
public class PartyTypeTranslationsEntity
{
    public int Id { get; private set; }
    public byte PartyTypeId { get; private set; }
    public string? LanguageCode { get; private set; }
    public string? Name { get; private set; }
    public string? Description { get; private set; }

    // Navigation Property
    public PartyTypeEntity PartyType { get; private set; } = null!;

    // Construtor protegido para o EF Core
    protected PartyTypeTranslationsEntity() { }

    /// <summary>
    /// Construtor para criação de uma nova tradução de PartyType
    /// </summary>
    public PartyTypeTranslationsEntity(byte partyTypeId, string? languageCode, string? name, string? description)
    {
        PartyTypeId = partyTypeId;
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
