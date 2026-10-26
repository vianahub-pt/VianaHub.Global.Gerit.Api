namespace VianaHub.Global.Gerit.Domain.Entities.Billing;

/// <summary>
/// Entidade de tradução de um SubscriptionPlan por idioma (pt-PT, en-US, es-ES).
/// Tabela: dbo.SubscriptionPlanTranslations — PK surrogate (Id).
/// </summary>
public class SubscriptionPlanTranslationsEntity
{
    public int Id { get; private set; }
    public int SubscriptionPlanId { get; private set; }
    public string? LanguageCode { get; private set; }
    public string? Name { get; private set; }
    public string? Description { get; private set; }

    // Navigation Property
    public SubscriptionPlanEntity SubscriptionPlan { get; private set; } = null!;

    // Construtor protegido para o EF Core
    protected SubscriptionPlanTranslationsEntity() { }

    /// <summary>
    /// Construtor para criação de uma nova tradução de SubscriptionPlan
    /// </summary>
    public SubscriptionPlanTranslationsEntity(int subscriptionPlanId, string? languageCode, string? name, string? description)
    {
        SubscriptionPlanId = subscriptionPlanId;
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
