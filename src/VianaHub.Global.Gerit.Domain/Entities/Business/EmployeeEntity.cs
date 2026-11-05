using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade que representa um Funcionário do Tenant
/// Aggregate Root para o contexto de Funcionário
/// </summary>
public class EmployeeEntity : Entity, IAggregateRoot
{
    public int TenantId { get; private set; }
    public int StatusDefinitionId { get; private set; }
    public int StatusDomainId { get; private set; }
    public string? Name { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? CellPhoneNumber { get; private set; }
    public bool IsCellPhoneWhatsapp { get; private set; }
    public string? Email { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public TenantEntity? Tenant { get; private set; }
    public StatusDefinitionEntity? StatusDefinition { get; private set; }

    private readonly List<EmployeeContactPersonsEntity> _contacts = [];
    public IReadOnlyCollection<EmployeeContactPersonsEntity> Contacts => _contacts.AsReadOnly();

    private readonly List<EmployeeAddressesEntity> _addresses = [];
    public IReadOnlyCollection<EmployeeAddressesEntity> Addresses => _addresses.AsReadOnly();

    // Construtor protegido para o EF Core
    protected EmployeeEntity() { }

    /// <summary>
    /// Construtor para criação de um novo Funcionário
    /// </summary>
    public EmployeeEntity(int tenantId, int statusDefinitionId, int statusDomainId, string? name,
        string? phoneNumber, string? cellPhoneNumber, bool isCellPhoneWhatsapp,
        string? email, string? imageUrl, int createdBy)
    {
        TenantId = tenantId;
        StatusDefinitionId = statusDefinitionId;
        StatusDomainId = statusDomainId;
        Name = name;
        PhoneNumber = phoneNumber;
        CellPhoneNumber = cellPhoneNumber;
        IsCellPhoneWhatsapp = isCellPhoneWhatsapp;
        Email = email;
        ImageUrl = imageUrl;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(int statusDefinitionId, int statusDomainId, string? name,
        string? phoneNumber, string? cellPhoneNumber, bool isCellPhoneWhatsapp,
        string? email, string? imageUrl, int modifiedBy)
    {
        StatusDefinitionId = statusDefinitionId;
        StatusDomainId = statusDomainId;
        Name = name;
        PhoneNumber = phoneNumber;
        CellPhoneNumber = cellPhoneNumber;
        IsCellPhoneWhatsapp = isCellPhoneWhatsapp;
        Email = email;
        ImageUrl = imageUrl;
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

    public void AddContact(EmployeeContactPersonsEntity contact)
    {
        if (contact == null)
            throw new ArgumentNullException(nameof(contact));

        _contacts.Add(contact);
    }

    public void AddAddress(EmployeeAddressesEntity address)
    {
        if (address == null)
            throw new ArgumentNullException(nameof(address));

        _addresses.Add(address);
    }
}
