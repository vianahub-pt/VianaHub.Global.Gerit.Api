using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Domain.Entities.Billing;

/// <summary>
/// Entidade que representa um Tenant (inquilino) no sistema multi-tenant.
/// Aggregate Root para o contexto de Tenant.
/// </summary>
public class TenantEntity : Entity, IAggregateRoot
{
    private readonly List<TenantContactPersonsEntity> _contacts = [];
    private readonly List<TenantAddressesEntity> _addresses = [];
    private readonly List<TenantFiscalDataEntity> _fiscalData = [];
    private readonly List<UserEntity> _users = [];

    public byte PartyTypeId { get; private set; }
    public PartyTypeEntity? PartyType { get; private set; }
    public int AcquisitionSourceTypeId { get; private set; }
    public AcquisitionSourceTypeEntity? AcquisitionSourceType { get; private set; }
    public string? Name { get; private set; }
    public string? Email { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? Note { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Partes internas do agregado
    public IReadOnlyCollection<TenantContactPersonsEntity> Contacts => _contacts.AsReadOnly();
    public IReadOnlyCollection<TenantAddressesEntity> Addresses => _addresses.AsReadOnly();
    public IReadOnlyCollection<TenantFiscalDataEntity> FiscalData => _fiscalData.AsReadOnly();
    public IReadOnlyCollection<UserEntity> Users => _users.AsReadOnly();

    // Construtor protegido para o EF Core
    protected TenantEntity() { }

    /// <summary>
    /// Construtor para criação de um novo Tenant
    /// </summary>
    public TenantEntity(byte partyTypeId, int acquisitionSourceTypeId, string? name, string? email, string? websiteUrl, string? imageUrl, string? note, int createdBy)
    {
        PartyTypeId = partyTypeId;
        AcquisitionSourceTypeId = acquisitionSourceTypeId;
        Name = name;
        Email = email;
        WebsiteUrl = websiteUrl;
        ImageUrl = imageUrl;
        Note = note;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(byte partyTypeId, int acquisitionSourceTypeId, string? name, string? email, string? websiteUrl, string? imageUrl, string? note, int modifiedBy)
    {
        PartyTypeId = partyTypeId;
        AcquisitionSourceTypeId = acquisitionSourceTypeId;
        Name = name;
        Email = email;
        WebsiteUrl = websiteUrl;
        ImageUrl = imageUrl;
        Note = note;
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

    public void AddAddress(TenantAddressesEntity address, int createdBy)
    {
        if (address == null)
            throw new ArgumentNullException(nameof(address));

        _addresses.Add(address);
    }

    public void AddFiscalData(TenantFiscalDataEntity fiscalData, int createdBy)
    {
        if (fiscalData == null)
            throw new ArgumentNullException(nameof(fiscalData));

        _fiscalData.Add(fiscalData);
    }
}
