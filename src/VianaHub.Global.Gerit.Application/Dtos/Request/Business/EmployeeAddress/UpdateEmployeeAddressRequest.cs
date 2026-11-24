namespace VianaHub.Global.Gerit.Application.Dtos.Request.Business.EmployeeAddress;

/// <summary>
/// DTO para atualizaþÒo de endereþo de membro da equipe
/// </summary>
public class UpdateEmployeeAddressRequest
{
    public int AddressTypeId { get; set; }
    public string? CountryCode { get; set; }
    public string? Street { get; set; }
    public string? StreetNumber { get; set; }
    public string? Complement { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? District { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Note { get; set; }
    public bool? IsActive { get; set; }
}
