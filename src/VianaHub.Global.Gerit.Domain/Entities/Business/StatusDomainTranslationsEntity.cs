namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade de tradução de um StatusDomain por idioma (pt-PT, en-US, es-ES).
/// Tabela: dbo.StatusDomainTranslations — PK surrogate (Id).
/// </summary>
public class StatusDomainTranslationsEntity
{
    public int Id { get; private set; }
    public int StatusDomainId { get; private set; }
    public string? LanguageCode { get; private set; }
    public string? Name { get; private set; }
    public string? Description { get; private set; }

    // Navigation Property
    public StatusDomainEntity StatusDomain { get; private set; } = null!;

    // Construtor protegido para o EF Core
    protected StatusDomainTranslationsEntity() { }

    /// <summary>
    /// Construtor para criação de uma nova tradução de StatusDomain
    /// </summary>
    public StatusDomainTranslationsEntity(int statusDomainId, string? languageCode, string? name, string? description)
    {
        StatusDomainId = statusDomainId;
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
