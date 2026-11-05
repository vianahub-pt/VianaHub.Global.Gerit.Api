using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Domain.Entities.Billing;

/// <summary>
/// Entidade que representa uma assinatura de um tenant a um plano
/// </summary>
public class SubscriptionEntity : Entity
{
    public int TenantId { get; private set; }
    public int SubscriptionPlanId { get; private set; }
    public int StatusDefinitionId { get; private set; }
    public int StatusDomainId { get; private set; }
    public decimal AgreedAmount { get; private set; }
    public string? BillingInterval { get; private set; }
    public string? CurrencyCode { get; private set; }
    public string? StripeId { get; private set; }
    public DateTime CurrentPeriodStart { get; private set; }
    public DateTime CurrentPeriodEnd { get; private set; }
    public DateTime? TrialStart { get; private set; }
    public DateTime? TrialEnd { get; private set; }
    public bool CancelAtPeriodEnd { get; private set; }
    public DateTime? CanceledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? StripeCustomerId { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public TenantEntity Tenant { get; private set; } = null!;
    public SubscriptionPlanEntity SubscriptionPlan { get; private set; } = null!;
    public StatusDefinitionEntity StatusDefinition { get; private set; } = null!;

    // Construtor protegido para o EF Core
    protected SubscriptionEntity() { }

    /// <summary>
    /// Construtor para criação de uma nova assinatura
    /// </summary>
    public SubscriptionEntity(
        int tenantId,
        int subscriptionPlanId,
        int statusDefinitionId,
        int statusDomainId,
        decimal agreedAmount,
        string? billingInterval,
        string? currencyCode,
        DateTime currentPeriodStart,
        DateTime currentPeriodEnd,
        DateTime? trialStart,
        DateTime? trialEnd,
        string? stripeCustomerId,
        int createdBy)
    {
        TenantId = tenantId;
        SubscriptionPlanId = subscriptionPlanId;
        StatusDefinitionId = statusDefinitionId;
        StatusDomainId = statusDomainId;
        AgreedAmount = agreedAmount;
        BillingInterval = billingInterval;
        CurrencyCode = currencyCode;
        CurrentPeriodStart = currentPeriodStart;
        CurrentPeriodEnd = currentPeriodEnd;
        TrialStart = trialStart;
        TrialEnd = trialEnd;
        StripeCustomerId = stripeCustomerId;
        CancelAtPeriodEnd = false;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
    }

    public void Update(
        int subscriptionPlanId,
        int statusDefinitionId,
        int statusDomainId,
        decimal agreedAmount,
        string? billingInterval,
        string? currencyCode,
        DateTime currentPeriodStart,
        DateTime currentPeriodEnd,
        DateTime? trialStart,
        DateTime? trialEnd,
        int modifiedBy)
    {
        SubscriptionPlanId = subscriptionPlanId;
        StatusDefinitionId = statusDefinitionId;
        StatusDomainId = statusDomainId;
        AgreedAmount = agreedAmount;
        BillingInterval = billingInterval;
        CurrencyCode = currencyCode;
        CurrentPeriodStart = currentPeriodStart;
        CurrentPeriodEnd = currentPeriodEnd;
        TrialStart = trialStart;
        TrialEnd = trialEnd;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
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

    public void Cancel(string? cancellationReason, bool cancelAtPeriodEnd, int modifiedBy)
    {
        CancelAtPeriodEnd = cancelAtPeriodEnd;
        CanceledAt = DateTime.UtcNow;
        CancellationReason = cancellationReason;

        if (!cancelAtPeriodEnd)
        {
            IsActive = false;
        }

        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Renew(DateTime newPeriodEnd, int modifiedBy)
    {
        CurrentPeriodStart = CurrentPeriodEnd;
        CurrentPeriodEnd = newPeriodEnd;
        CancelAtPeriodEnd = false;
        CanceledAt = null;
        CancellationReason = null;
        IsActive = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }
}
