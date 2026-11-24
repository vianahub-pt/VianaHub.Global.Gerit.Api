namespace VianaHub.Global.Gerit.Application.Dtos.Request.Billing.TenantAddress;

public class CreateTenantAddressRequest
{
    public int AddressTypeId { get; set; }
    public string? CountryCode { get; set; } = "PT";
    public string? Street { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? PostalCode { get; set; }
    public string? StreetNumber { get; set; }
    public string? Complement { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Note { get; set; }
    public bool IsPrimary { get; set; }
    public bool? IsActive { get; set; }
}
