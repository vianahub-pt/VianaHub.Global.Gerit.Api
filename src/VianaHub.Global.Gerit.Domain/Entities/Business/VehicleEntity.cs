using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Domain.Entities.Business;

/// <summary>
/// Entidade que representa um Veículo
/// </summary>
public class VehicleEntity : Entity
{
    public int TenantId { get; private set; }
    public int StatusDefinitionId { get; private set; }
    public int StatusDomainId { get; private set; }
    public string? Plate { get; private set; }
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public int Year { get; private set; }
    public string? Color { get; private set; }
    public string? FuelType { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation Properties
    public TenantEntity? Tenant { get; private set; }
    public StatusDefinitionEntity? StatusDefinition { get; private set; }

    // Construtor protegido para o EF Core
    protected VehicleEntity() { }

    /// <summary>
    /// Construtor para criação de um novo Veículo
    /// </summary>
    public VehicleEntity(int tenantId, int statusDefinitionId, int statusDomainId, string? plate, string? brand, string? model, int year, string? color, string? fuelType, int createdBy)
    {
        TenantId = tenantId;
        StatusDefinitionId = statusDefinitionId;
        StatusDomainId = statusDomainId;
        Plate = plate;
        Brand = brand;
        Model = model;
        Year = year;
        Color = color;
        FuelType = fuelType;
        IsActive = true;
        IsDeleted = false;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(int statusDefinitionId, int statusDomainId, string? plate, string? brand, string? model, int year, string? color, string? fuelType, int modifiedBy)
    {
        StatusDefinitionId = statusDefinitionId;
        StatusDomainId = statusDomainId;
        Plate = plate;
        Brand = brand;
        Model = model;
        Year = year;
        Color = color;
        FuelType = fuelType;
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
