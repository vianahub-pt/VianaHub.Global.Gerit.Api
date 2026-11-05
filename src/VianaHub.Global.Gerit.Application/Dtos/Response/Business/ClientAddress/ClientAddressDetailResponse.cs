namespace VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientAddress;

/// <summary>
/// DTO de resposta detalhada para ClientAddress — inclui campos de auditoria.
/// Classe independente — não herda de ClientAddressResponse.
/// </summary>
public class ClientAddressDetailResponse
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string? TenantName { get; set; }
    public int ClientId { get; set; }
    public string? ClientName { get; set; }
    public int AddressTypeId { get; set; }
    public string? AddressTypeName { get; set; }

    public string? CountryCode { get; set; } = null!;
    public string? Street { get; set; } = null!;
    public string? StreetNumber { get; set; }
    public string? Complement { get; set; }
    public string? Neighborhood { get; set; } = null!;
    public string? City { get; set; } = null!;
    public string? District { get; set; }
    public string? PostalCode { get; set; } = null!;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Note { get; set; }

    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
