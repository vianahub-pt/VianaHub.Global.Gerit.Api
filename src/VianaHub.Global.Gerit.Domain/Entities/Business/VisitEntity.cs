using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade que representa uma Intervenção
/// Aggregate Root para o contexto de Intervenção
/// </summary>
public class VisitEntity : Entity, IAggregateRoot
{
    public int TenantId { get; private set; }
    public int ClientId { get; private set; }
    public int StatusDefinitionId { get; private set; }
    public int StatusDomainId { get; private set; }
    public string CurrencyCode { get; private set; } = "EUR";
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public DateTime StartDateTime { get; private set; }
    public DateTime? EndDateTime { get; private set; }
    public decimal EstimatedValue { get; private set; }
    public decimal? RealValue { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public TenantEntity? Tenant { get; private set; }
    public ClientEntity? Client { get; private set; }
    public StatusDefinitionEntity? StatusDefinition { get; private set; }

    private readonly List<VisitContactPersonsEntity> _contacts = new();
    public IReadOnlyCollection<VisitContactPersonsEntity> Contacts => _contacts.AsReadOnly();

    private readonly List<VisitAddressesEntity> _addresses = new();
    public IReadOnlyCollection<VisitAddressesEntity> Addresses => _addresses.AsReadOnly();

    // Construtor protegido para o EF Core
    protected VisitEntity() { }

    /// <summary>
    /// Construtor para criação de uma nova Intervenção
    /// </summary>
    public VisitEntity(int tenantId, int clientId, int statusDefinitionId, int statusDomainId, string currencyCode, string? title, string? description, DateTime startDateTime,
        decimal estimatedValue, int modifiedBy)
    {
        TenantId = tenantId;
        ClientId = clientId;
        StatusDefinitionId = statusDefinitionId;
        StatusDomainId = statusDomainId;
        CurrencyCode = currencyCode;
        Title = title;
        Description = description;
        StartDateTime = startDateTime;
        EstimatedValue = estimatedValue;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = modifiedBy;
        CreatedAt = DateTime.UtcNow;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(int clientId, int statusDefinitionId, int statusDomainId, string currencyCode, string? title, string? description, DateTime startDateTime, DateTime? endDateTime,
        decimal estimatedValue, decimal? realValue, int modifiedBy)
    {
        ClientId = clientId;
        StatusDefinitionId = statusDefinitionId;
        StatusDomainId = statusDomainId;
        CurrencyCode = currencyCode;
        Title = title;
        Description = description;
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
        EstimatedValue = estimatedValue;
        RealValue = realValue;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Activate(int modifiedBy)
    {
        IsActive = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Deactivate(int modifiedBy)
    {
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Delete(int modifiedBy)
    {
        IsDeleted = true;
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void AddContact(VisitContactPersonsEntity contact)
    {
        if (contact == null)
            throw new ArgumentNullException(nameof(contact));

        _contacts.Add(contact);
    }

    public void AddAddress(VisitAddressesEntity address)
    {
        if (address == null)
            throw new ArgumentNullException(nameof(address));

        _addresses.Add(address);
    }
}
