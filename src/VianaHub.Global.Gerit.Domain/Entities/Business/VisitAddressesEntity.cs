using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade que representa um endere�o da Interven��o
/// </summary>
public class VisitAddressesEntity : Entity
{
    public int TenantId { get; private set; }
    public int VisitId { get; private set; }
    public int AddressTypeId { get; private set; }
    public string? CountryCode { get; private set; }
    public string? Street { get; private set; }
    public string? StreetNumber { get; private set; }
    public string? Complement { get; private set; }
    public string? Neighborhood { get; private set; }
    public string? City { get; private set; }
    public string? District { get; private set; }
    public string? PostalCode { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public string? Note { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public TenantEntity? Tenant { get; private set; }
    public VisitEntity? Visit { get; private set; }
    public AddressTypeEntity? AddressType { get; private set; }

    // Construtor protegido para o EF Core
    protected VisitAddressesEntity() { }

    /// <summary>
    /// Construtor para cria��o de um novo endere�o da Interven��o
    /// </summary>
    public VisitAddressesEntity(
        int tenantId,
        int interventionId,
        int addressTypeId,
        string? countryCode,
        string? street,
        string? streetNumber,
        string? complement,
        string? neighborhood,
        string? city,
        string? district,
        string? postalCode,
        decimal? latitude,
        decimal? longitude,
        string? note,
        bool isPrimary,
        int createdBy)
    {
        TenantId = tenantId;
        VisitId = interventionId;
        AddressTypeId = addressTypeId;
        CountryCode = countryCode ?? "PT";
        Street = street;
        StreetNumber = streetNumber;
        Neighborhood = neighborhood;
        Complement = complement;
        City = city;
        District = district;
        PostalCode = postalCode;
        Latitude = latitude;
        Longitude = longitude;
        Note = note;
        IsPrimary = isPrimary;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateAddressInfo(
        int addressTypeId,
        string? countryCode,
        string? street,
        string? streetNumber,
        string? complement,
        string? neighborhood,
        string? city,
        string? district,
        string? postalCode,
        decimal? latitude,
        decimal? longitude,
        string? note,
        int modifiedBy)
    {
        AddressTypeId = addressTypeId;
        CountryCode = countryCode ?? "PT";
        Street = street;
        StreetNumber = streetNumber;
        Complement = complement;
        Neighborhood = neighborhood;
        City = city;
        District = district;
        PostalCode = postalCode;
        Latitude = latitude;
        Longitude = longitude;
        Note = note;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void SetAsPrimary(int modifiedBy)
    {
        IsPrimary = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void RemoveAsPrimary(int modifiedBy)
    {
        IsPrimary = false;
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
}
