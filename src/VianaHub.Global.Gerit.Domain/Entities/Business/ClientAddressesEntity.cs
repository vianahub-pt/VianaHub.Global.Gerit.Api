using VianaHub.Global.Gerit.Domain.Base;

namespace VianaHub.Global.Gerit.Domain.Entities.Business
{
    public class ClientAddressesEntity : Entity
    {
        public int TenantId { get; private set; }
        public int ClientId { get; private set; }
        public int AddressTypeId { get; private set; }

        public string? CountryCode { get; private set; } = null!;
        public string? Street { get; private set; } = null!;
        public string? StreetNumber { get; private set; }
        public string? Complement { get; private set; }
        public string? Neighborhood { get; private set; } = null!;
        public string? City { get; private set; } = null!;
        public string? District { get; private set; }
        public string? PostalCode { get; private set; } = null!;
        public decimal? Latitude { get; private set; }
        public decimal? Longitude { get; private set; }
        public string? Note { get; private set; }

        public bool IsPrimary { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsDeleted { get; private set; }

        // Navigation Properties
        public ClientEntity Client { get; private set; } = null!;
        public AddressTypeEntity AddressType { get; private set; } = null!;

        // Construtor protegido para EF Core
        protected ClientAddressesEntity() { }

        public ClientAddressesEntity(int tenantId, int clientId, int addressTypeId, string? countryCode, string? street, string? streetNumber, string? complement, string? neighborhood, string? city, string? district, string? postalCode, decimal? latitude, decimal? longitude, string? note, bool isPrimary, int createdBy)
        {
            TenantId = tenantId;
            ClientId = clientId;
            AddressTypeId = addressTypeId;

            CountryCode = countryCode;
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

        public void Update(int addressTypeId, string? countryCode, string? street, string? streetNumber, string? complement, string? neighborhood, string? city, string? district, string? postalCode, decimal? latitude, decimal? longitude, string? note, bool isPrimary, int modifiedBy)
        {
            AddressTypeId = addressTypeId;
            CountryCode = countryCode;
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
            ModifiedBy = modifiedBy;
            ModifiedAt = DateTime.UtcNow;
        }

        public void SetPrimary(int modifiedBy)
        {
            IsPrimary = true;
            ModifiedBy = modifiedBy;
            ModifiedAt = DateTime.UtcNow;
        }

        public void RemovePrimary(int modifiedBy)
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
            IsPrimary = false;
            IsActive = false;
            IsDeleted = true;
            ModifiedBy = modifiedBy;
            ModifiedAt = DateTime.UtcNow;
        }
    }
}
