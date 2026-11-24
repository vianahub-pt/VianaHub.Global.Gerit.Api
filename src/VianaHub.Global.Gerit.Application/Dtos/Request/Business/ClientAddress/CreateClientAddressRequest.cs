namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientAddress;

/// <summary>
/// DTO para criação de ClientAddress
/// </summary>
public class CreateClientAddressRequest
{
    public int AddressTypeId { get; set; }
    public string? CountryCode { get; set; }
    public string? Street { get; set; }
    public string? StreetNumber { get; set; }
    public string? Complement { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Note { get; set; }
    public bool IsPrimary { get; set; }
    public bool? IsActive { get; set; }
}
