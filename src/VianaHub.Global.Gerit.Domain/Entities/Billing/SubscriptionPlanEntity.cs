using VianaHub.Global.Gerit.Domain.Base;

namespace VianaHub.Global.Gerit.Domain.Entities.Billing;

/// <summary>
/// Entidade que representa um plano de assinatura do sistema.
/// Tabela: dbo.SubscriptionPlans.
/// Nota: Name e Description residem exclusivamente na tabela de tradução
/// SubscriptionPlanTranslations e são acedidos via propriedade de navegação Translations.
/// </summary>
public class SubscriptionPlanEntity : Entity
{
    public string? Code { get; private set; }
    public decimal? PricePerHour { get; set; }
    public decimal? PricePerDay { get; set; }
    public decimal? PricePerMonth { get; set; }
    public decimal? PricePerYear { get; set; }
    public string? Currency { get; private set; }
    public int MaxUsers { get; private set; }
    public int MaxPhotosPerVisit { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    private readonly List<SubscriptionEntity> _subscriptions = new();
    public IReadOnlyCollection<SubscriptionEntity> Subscriptions => _subscriptions.AsReadOnly();

    private readonly List<SubscriptionPlanTranslationsEntity> _translations = new();
    public IReadOnlyCollection<SubscriptionPlanTranslationsEntity> Translations => _translations.AsReadOnly();

    // Construtor protegido para o EF Core
    protected SubscriptionPlanEntity() { }

    /// <summary>
    /// Construtor para criação de um novo plano.
    /// Name e Description são armazenados separadamente na tabela de traduções.
    /// </summary>
    public SubscriptionPlanEntity(
        string? code,
        decimal? pricePerHour,
        decimal? pricePerDay,
        decimal? pricePerMonth,
        decimal? pricePerYear,
        string currency,
        int maxUsers,
        int maxPhotosPerVisit,
        int createdBy)
    {
        Code = code;
        PricePerHour = pricePerHour;
        PricePerDay = pricePerDay;
        PricePerMonth = pricePerMonth;
        PricePerYear = pricePerYear;
        Currency = currency ?? "EUR";
        MaxUsers = maxUsers;
        MaxPhotosPerVisit = maxPhotosPerVisit;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
    }

    public void Update(
        decimal? pricePerHour,
        decimal? pricePerDay,
        decimal? pricePerMonth,
        decimal? pricePerYear,
        string currency,
        int maxUsers,
        int maxPhotosPerVisit,
        int modifiedBy)
    {

        PricePerHour = pricePerHour;
        PricePerDay = pricePerDay;
        PricePerMonth = pricePerMonth;
        PricePerYear = pricePerYear;
        Currency = currency ?? "EUR";
        MaxUsers = maxUsers;
        MaxPhotosPerVisit = maxPhotosPerVisit;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adiciona uma tradução ao plano. Usado para criar traduções iniciais (pt-PT)
    /// ou adicionar novos idiomas posteriormente.
    /// </summary>
    public void AddTranslation(SubscriptionPlanTranslationsEntity translation)
    {
        _translations.Add(translation);
    }

    public void Activate(int? modifiedBy)
    {
        IsActive = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Deactivate(int? modifiedBy)
    {
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Delete(int? modifiedBy)
    {
        IsDeleted = true;
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }
}
