namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade de tradução de um StatusDefinition por idioma (pt-PT, en-US, es-ES).
/// FK composta (StatusDefinitionId, TenantId, StatusDomainId) referenciando StatusDefinitions.
/// </summary>
public class StatusDefinitionTranslationsEntity
{
    public int Id { get; private set; }
    public int TenantId { get; private set; }
    public int StatusDomainId { get; private set; }
    public int StatusDefinitionId { get; private set; }
    public string? LanguageCode { get; private set; }
    public string? Name { get; private set; }
    public string? Description { get; private set; }

    // Navigation Property
    public StatusDefinitionEntity StatusDefinition { get; private set; } = null!;

    // Construtor protegido para o EF Core
    protected StatusDefinitionTranslationsEntity() { }

    /// <summary>
    /// Construtor para criação de uma nova tradução de StatusDefinition
    /// </summary>
    public StatusDefinitionTranslationsEntity(int tenantId, int statusDomainId, int statusDefinitionId, string? languageCode, string? name, string? description)
    {
        TenantId = tenantId;
        StatusDomainId = statusDomainId;
        StatusDefinitionId = statusDefinitionId;
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
