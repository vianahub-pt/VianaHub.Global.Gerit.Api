using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

public class ClientEntity : Entity, IAggregateRoot
{
    private readonly List<ClientAddressesEntity> _addresses = new();
    private readonly List<ClientContactPersonsEntity> _contacts = new();

    public int TenantId { get; private set; }
    public byte PartyTypeId { get; private set; }
    public int AcquisitionSourceTypeId { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? Note { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Campos unificados (pessoa singular + jurídica)
    public string Name { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string? CellPhoneNumber { get; private set; }
    public bool IsCellPhoneWhatsapp { get; private set; }
    public string? Email { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public DateTime? BirthDate { get; private set; }
    public string? Gender { get; private set; }
    public string? Nationality { get; private set; }
    public string? CompanyRegistrationNumber { get; private set; }
    public string? EconomicActivityCode { get; private set; }
    public int? NumberOfEmployees { get; private set; }
    public int StatusDefinitionId { get; private set; }
    public int StatusDomainId { get; private set; }

    // Navegação para FiscalData (mantida)
    public ClientFiscalDataEntity? FiscalData { get; private set; }

    // Navigation Properties
    public TenantEntity Tenant { get; private set; } = null!;
    public PartyTypeEntity PartyType { get; private set; } = null!;
    public AcquisitionSourceTypeEntity AcquisitionSourceType { get; private set; } = null!;
    public StatusDefinitionEntity? StatusDefinition { get; private set; }
    public StatusDomainEntity? StatusDomain { get; private set; }

    public IReadOnlyCollection<ClientAddressesEntity> Addresses => _addresses.AsReadOnly();
    public IReadOnlyCollection<ClientContactPersonsEntity> Contacts => _contacts.AsReadOnly();

    // Construtor protegido para o EF Core
    protected ClientEntity() { }

    public ClientEntity(
        int tenantId,
        byte partyTypeId,
        int acquisitionSourceTypeId,
        string? imageUrl,
        string? note,
        string name,
        string? phoneNumber,
        string? cellPhoneNumber,
        bool isCellPhoneWhatsapp,
        string? email,
        string? websiteUrl,
        DateTime? birthDate,
        string? gender,
        string? nationality,
        string? companyRegistrationNumber,
        string? economicActivityCode,
        int? numberOfEmployees,
        int statusDefinitionId,
        int statusDomainId,
        int createdBy)
    {
        TenantId = tenantId;
        PartyTypeId = partyTypeId;
        AcquisitionSourceTypeId = acquisitionSourceTypeId;
        ImageUrl = imageUrl;
        Note = note;
        Name = name;
        PhoneNumber = phoneNumber;
        CellPhoneNumber = cellPhoneNumber;
        IsCellPhoneWhatsapp = isCellPhoneWhatsapp;
        Email = email;
        WebsiteUrl = websiteUrl;
        BirthDate = birthDate;
        Gender = gender;
        Nationality = nationality;
        CompanyRegistrationNumber = companyRegistrationNumber;
        EconomicActivityCode = economicActivityCode;
        NumberOfEmployees = numberOfEmployees;
        StatusDefinitionId = statusDefinitionId;
        StatusDomainId = statusDomainId;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        byte partyTypeId,
        int acquisitionSourceTypeId,
        string? imageUrl,
        string? note,
        string name,
        string? phoneNumber,
        string? cellPhoneNumber,
        bool isCellPhoneWhatsapp,
        string? email,
        string? websiteUrl,
        DateTime? birthDate,
        string? gender,
        string? nationality,
        string? companyRegistrationNumber,
        string? economicActivityCode,
        int? numberOfEmployees,
        int statusDefinitionId,
        int statusDomainId,
        int modifiedBy)
    {
        PartyTypeId = partyTypeId;
        AcquisitionSourceTypeId = acquisitionSourceTypeId;
        ImageUrl = imageUrl;
        Note = note;
        Name = name;
        PhoneNumber = phoneNumber;
        CellPhoneNumber = cellPhoneNumber;
        IsCellPhoneWhatsapp = isCellPhoneWhatsapp;
        Email = email;
        WebsiteUrl = websiteUrl;
        BirthDate = birthDate;
        Gender = gender;
        Nationality = nationality;
        CompanyRegistrationNumber = companyRegistrationNumber;
        EconomicActivityCode = economicActivityCode;
        NumberOfEmployees = numberOfEmployees;
        StatusDefinitionId = statusDefinitionId;
        StatusDomainId = statusDomainId;
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
        IsActive = false;
        IsDeleted = true;

        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }
}
