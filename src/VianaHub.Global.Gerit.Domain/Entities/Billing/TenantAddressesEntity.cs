using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Domain.Entities.Billing;

/// <summary>
/// Entidade que representa um endere�o do Tenant
/// </summary>
public class TenantAddressesEntity : Entity
{
    public int TenantId { get; private set; }
    public int AddressTypeId { get; private set; }
    public string? CountryCode { get; private set; }
    public string? Street { get; private set; }
    public string? Neighborhood { get; private set; }
    public string? City { get; private set; }
    public string? District { get; private set; }
    public string? PostalCode { get; private set; }
    public string? StreetNumber { get; private set; }
    public string? Complement { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public string? Note { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public TenantEntity? Tenant { get; private set; }
    public AddressTypeEntity? AddressType { get; private set; }

    // Construtor protegido para o EF Core
    protected TenantAddressesEntity() { }

    /// <summary>
    /// Construtor para cria��o de um novo endere�o do Tenant
    /// </summary>
    public TenantAddressesEntity(
        int tenantId,
        int addressTypeId,
        string countryCode,
        string? street,
        string? neighborhood,
        string? city,
        string? district,
        string? postalCode,
        string? streetNumber,
        string? complement,
        decimal? latitude,
        decimal? longitude,
        string? note,
        bool isPrimary,
        int createdBy)
    {
        TenantId = tenantId;
        AddressTypeId = addressTypeId;
        CountryCode = countryCode ?? "PT";
        Street = street;
        Neighborhood = neighborhood;
        City = city;
        District = district;
        PostalCode = postalCode;
        StreetNumber = streetNumber;
        Complement = complement;
        Latitude = latitude;
        Longitude = longitude;
        Note = note;
        IsPrimary = isPrimary;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        int addressTypeId,
        string countryCode,
        string? street,
        string? neighborhood,
        string? city,
        string? district,
        string? postalCode,
        string? streetNumber,
        string? complement,
        decimal? latitude,
        decimal? longitude,
        string? note,
        int modifiedBy)
    {
        AddressTypeId = addressTypeId;
        CountryCode = countryCode ?? "PT";
        Street = street;
        Neighborhood = neighborhood;
        City = city;
        District = district;
        PostalCode = postalCode;
        StreetNumber = streetNumber;
        Complement = complement;
        Latitude = latitude;
        Longitude = longitude;
        Note = note;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    public void RemoveAsPrimary()
    {
        IsPrimary = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Delete()
    {
        IsDeleted = true;
        IsActive = false;
    }
}
