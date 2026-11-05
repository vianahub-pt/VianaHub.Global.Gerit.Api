using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Equipment;
using VianaHub.Global.Gerit.Application.Mappings.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Tests;

public class NullablePersistenceContractTests
{
    [Fact]
    public void UnloadedNavigations_MapWithoutLosingForeignKeys()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<EquipmentMappingProfile>(), NullLoggerFactory.Instance);
        var equipment = new EquipmentEntity(7, 11, 13, 17, "Meter", null, null, 1);

        var response = configuration.CreateMapper().Map<EquipmentDetailResponse>(equipment);

        Assert.Equal(7, response.TenantId);
        Assert.Equal(11, response.EquipmentTypeId);
        Assert.Equal(13, response.StatusDefinitionId);
        Assert.Equal(17, response.StatusDomainId);
        Assert.Null(response.StatusDefinitionName);
        Assert.Null(response.StatusDomainName);
    }

    [Fact]
    public void NullableUnloadedNavigations_PreserveRequiredDatabaseRelationships()
    {
        using var context = new GeritDbContext(new DbContextOptionsBuilder<GeritDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var entityType = context.Model.FindEntityType(typeof(EmployeeAddressesEntity));

        Assert.NotNull(entityType);
        var addressType = entityType.FindNavigation(nameof(EmployeeAddressesEntity.AddressType));
        var employee = entityType.FindNavigation(nameof(EmployeeAddressesEntity.Employee));
        Assert.NotNull(addressType);
        Assert.NotNull(employee);
        Assert.True(addressType.ForeignKey.IsRequired);
        Assert.True(employee.ForeignKey.IsRequired);
    }
}
