namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade de tradução de um AddressType por idioma (pt-PT, en-US, es-ES).
/// Tabela: dbo.AddressTypeTranslations — PK surrogate (Id).
/// </summary>
public class AddressTypeTranslationsEntity
{
    public int Id { get; private set; }
    public int AddressTypeId { get; private set; }
    public string? LanguageCode { get; private set; }
    public string? Name { get; private set; }
    public string? Description { get; private set; }

    // Navigation Property
    public AddressTypeEntity AddressType { get; private set; } = null!;

    // Construtor protegido para o EF Core
    protected AddressTypeTranslationsEntity() { }

    /// <summary>
    /// Construtor para criação de uma nova tradução de AddressType
    /// </summary>
    public AddressTypeTranslationsEntity(int addressTypeId, string? languageCode, string? name, string? description)
    {
        AddressTypeId = addressTypeId;
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
