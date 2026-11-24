using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Entities.Job;

namespace VianaHub.Global.Gerit.Tests.IdentityMappings;

public sealed class IsActiveExtendedEntityContractTests
{
    [Fact]
    public void EmployeeSubresourcesHonorOmissionAndExplicitState()
    {
        var address = new EmployeeAddressesEntity(1, 1, 1, "PT", "Street", null, null, null, "City", null, null, null, null, null, false, 1, false);
        var addressDefault = new EmployeeAddressesEntity(1, 1, 1, "PT", "Street", null, null, null, "City", null, null, null, null, null, false, 1);
        AssertOptionalState(addressDefault, address,
            e => e.UpdateAddressInfo(1, "PT", "Changed", null, null, null, "City", null, null, null, null, null, 2),
            e => e.UpdateAddressInfo(1, "PT", "Changed", null, null, null, "City", null, null, null, null, null, 2, true));

        var contact = new EmployeeContactPersonsEntity(1, 1, "Name", null, null, null, null, null, false, false, 1, false);
        var contactDefault = new EmployeeContactPersonsEntity(1, 1, "Name", null, null, null, null, null, false, false, 1);
        AssertOptionalState(contactDefault, contact,
            e => e.UpdateContactInfo("Changed", null, null, null, null, null, false, 2),
            e => e.UpdateContactInfo("Changed", null, null, null, null, null, false, 2, true));

        var fiscal = new EmployeeFiscalDataEntity(1, 1, null, null, "PT", false, null, null, 1, false);
        var fiscalDefault = new EmployeeFiscalDataEntity(1, 1, null, null, "PT", false, null, null, 1);
        AssertOptionalState(fiscalDefault, fiscal,
            e => e.Update(null, null, "PT", false, null, null, 2),
            e => e.Update(null, null, "PT", false, null, null, 2, true));

        var team = new EmployeeTeamEntity(1, 1, 1, false, DateTime.UtcNow, 1, false);
        var teamDefault = new EmployeeTeamEntity(1, 1, 1, false, DateTime.UtcNow, 1);
        AssertOptionalState(teamDefault, team,
            e => e.Update(1, 1, false, DateTime.UtcNow, null, 2),
            e => e.Update(1, 1, false, DateTime.UtcNow, null, 2, true));
    }

    [Fact]
    public void TenantSubresourcesHonorOmissionAndExplicitState()
    {
        var address = new TenantAddressesEntity(1, 1, "PT", null, null, null, null, null, null, null, null, null, null, false, 1, false);
        var addressDefault = new TenantAddressesEntity(1, 1, "PT", null, null, null, null, null, null, null, null, null, null, false, 1);
        AssertOptionalState(addressDefault, address,
            e => e.Update(1, "PT", null, null, null, null, null, null, null, null, null, null, 2),
            e => e.Update(1, "PT", null, null, null, null, null, null, null, null, null, null, 2, true));

        var contact = new TenantContactPersonsEntity(1, "Name", null, null, null, null, null, false, false, 1, false);
        var contactDefault = new TenantContactPersonsEntity(1, "Name", null, null, null, null, null, false, false, 1);
        AssertOptionalState(contactDefault, contact,
            e => e.Update("Changed", null, null, null, null, null, false, 2),
            e => e.Update("Changed", null, null, null, null, null, false, 2, true));

        var fiscal = new TenantFiscalDataEntity(1, null, null, null, null, "PT", false, 1, false);
        var fiscalDefault = new TenantFiscalDataEntity(1, null, null, null, null, "PT", false, 1);
        AssertOptionalState(fiscalDefault, fiscal,
            e => e.UpdateFiscalData(null, null, null, null, "PT", false, 2),
            e => e.UpdateFiscalData(null, null, null, null, "PT", false, 2, true));
    }

    [Fact]
    public void VisitSubresourcesHonorOmissionAndExplicitState()
    {
        var address = new VisitAddressesEntity(1, 1, 1, "PT", null, null, null, null, null, null, null, null, null, null, false, 1, false);
        var addressDefault = new VisitAddressesEntity(1, 1, 1, "PT", null, null, null, null, null, null, null, null, null, null, false, 1);
        AssertOptionalState(addressDefault, address,
            e => e.UpdateAddressInfo(1, "PT", null, null, null, null, null, null, null, null, null, null, 2),
            e => e.UpdateAddressInfo(1, "PT", null, null, null, null, null, null, null, null, null, null, 2, true));

        var attachment = new VisitAttachmentsEntity(1, 1, 1, "s3/key", "file.pdf", 1, 0, false, 1, false);
        var attachmentDefault = new VisitAttachmentsEntity(1, 1, 1, "s3/key", "file.pdf", 1, 0, false, 1);
        AssertOptionalState(attachmentDefault, attachment,
            e => e.Update("changed.pdf", 1, false, 2),
            e => e.Update("changed.pdf", 1, false, 2, true));

        var contact = new VisitContactPersonsEntity(1, 1, "Name", null, null, null, null, null, false, false, 1, false);
        var contactDefault = new VisitContactPersonsEntity(1, 1, "Name", null, null, null, null, null, false, false, 1);
        AssertOptionalState(contactDefault, contact,
            e => e.Update("Changed", null, null, null, null, null, false, false, 2),
            e => e.Update("Changed", null, null, null, null, null, false, false, 2, true));

        var visitTeam = new VisitTeamEntity(1, 1, 1, DateTime.UtcNow, 1, false);
        var visitTeamDefault = new VisitTeamEntity(1, 1, 1, DateTime.UtcNow, 1);
        AssertOptionalState(visitTeamDefault, visitTeam,
            e => e.Update(1, 1, DateTime.UtcNow, null, 2),
            e => e.Update(1, 1, DateTime.UtcNow, null, 2, true));

        var teamEmployee = new VisitTeamEmployeeEntity(1, 1, 1, 1, false, DateTime.UtcNow, 1, false);
        var teamEmployeeDefault = new VisitTeamEmployeeEntity(1, 1, 1, 1, false, DateTime.UtcNow, 1);
        AssertOptionalState(teamEmployeeDefault, teamEmployee,
            e => e.Update(1, false, DateTime.UtcNow, null, 2),
            e => e.Update(1, false, DateTime.UtcNow, null, 2, true));

        var teamEquipment = new VisitTeamEquipmentEntity(1, 1, 1, 1, false);
        var teamEquipmentDefault = new VisitTeamEquipmentEntity(1, 1, 1, 1);
        AssertOptionalState(teamEquipmentDefault, teamEquipment,
            e => e.Update(1, 1, 2),
            e => e.Update(1, 1, 2, true));

        var teamVehicle = new VisitTeamVehicleEntity(1, 1, 1, 1, false);
        var teamVehicleDefault = new VisitTeamVehicleEntity(1, 1, 1, 1);
        AssertOptionalState(teamVehicleDefault, teamVehicle,
            e => e.Update(1, 1, 2),
            e => e.Update(1, 1, 2, true));
    }

    [Fact]
    public void EmployeeHonorsDefaultAndOptionalState()
    {
        var defaults = new EmployeeEntity(1, 1, 1, "Name", null, null, false, null, null, 1);
        var entity = new EmployeeEntity(1, 1, 1, "Name", null, null, false, null, null, 1, false);
        Assert.True(defaults.IsActive);
        Assert.False(entity.IsActive);
        entity.Update(1, 1, "Changed", null, null, false, null, null, 2);
        Assert.False(entity.IsActive);
        entity.Update(1, 1, "Changed", null, null, false, null, null, 2, true);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void TenantHonorsDefaultAndOptionalState()
    {
        var defaults = new TenantEntity(1, 1, "Tenant", null, null, null, null, 1);
        var entity = new TenantEntity(1, 1, "Tenant", null, null, null, null, 1, false);
        Assert.True(defaults.IsActive);
        Assert.False(entity.IsActive);
        entity.Update(1, 1, "Changed", null, null, null, null, 2);
        Assert.False(entity.IsActive);
        entity.Update(1, 1, "Changed", null, null, null, null, 2, true);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void VisitHonorsDefaultAndOptionalState()
    {
        var now = DateTime.UtcNow;
        var defaults = new VisitEntity(1, 1, 1, 1, "EUR", "Visit", null, now, 10m, 1);
        var entity = new VisitEntity(1, 1, 1, 1, "EUR", "Visit", null, now, 10m, 1, false);
        Assert.True(defaults.IsActive);
        Assert.False(entity.IsActive);
        entity.UpdateDetails(1, 1, 1, "EUR", "Changed", null, now, null, 10m, null, 2);
        Assert.False(entity.IsActive);
        entity.UpdateDetails(1, 1, 1, "EUR", "Changed", null, now, null, 10m, null, 2, true);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void PlanHonorsDefaultAndOptionalState()
    {
        var defaults = new SubscriptionPlanEntity("PLAN", null, null, null, null, "EUR", 5, 5, 1);
        var entity = new SubscriptionPlanEntity("PLAN", null, null, null, null, "EUR", 5, 5, 1, false);
        Assert.True(defaults.IsActive);
        Assert.False(entity.IsActive);
        entity.Update(null, null, null, null, "EUR", 5, 5, 2);
        Assert.False(entity.IsActive);
        entity.Update(null, null, null, null, "EUR", 5, 5, 2, true);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void SubscriptionHonorsDefaultAndOptionalState()
    {
        var now = DateTime.UtcNow;
        var defaults = new SubscriptionEntity(1, 1, 1, 1, 10m, "monthly", "EUR", now, now.AddMonths(1), null, null, null, 1);
        var entity = new SubscriptionEntity(1, 1, 1, 1, 10m, "monthly", "EUR", now, now.AddMonths(1), null, null, null, 1, false);
        Assert.True(defaults.IsActive);
        Assert.False(entity.IsActive);
        entity.Update(1, 1, 1, 10m, "monthly", "EUR", now, now.AddMonths(1), null, null, 2);
        Assert.False(entity.IsActive);
        entity.Update(1, 1, 1, 10m, "monthly", "EUR", now, now.AddMonths(1), null, null, 2, true);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void JobHonorsDefaultAndOptionalState()
    {
        var defaults = new JobDefinitionsEntity("category", "job", "type", 1);
        var entity = new JobDefinitionsEntity("category", "job", "type", 1, isActive: false);
        Assert.True(defaults.IsActive);
        Assert.False(entity.IsActive);
        entity.Update(null, null, null, null, 5, 5, "default", 3, null, null, 2);
        Assert.False(entity.IsActive);
        entity.Update(null, null, null, null, 5, 5, "default", 3, null, true, 2);
        Assert.True(entity.IsActive);
    }

    private static void AssertOptionalState<TEntity>(TEntity defaults, TEntity entity, Action<TEntity> omittedUpdate, Action<TEntity> activate)
    {
        Assert.True((bool)typeof(TEntity).GetProperty("IsActive")!.GetValue(defaults)!);
        Assert.False((bool)typeof(TEntity).GetProperty("IsActive")!.GetValue(entity)!);
        omittedUpdate(entity);
        Assert.False((bool)typeof(TEntity).GetProperty("IsActive")!.GetValue(entity)!);
        activate(entity);
        Assert.True((bool)typeof(TEntity).GetProperty("IsActive")!.GetValue(entity)!);
    }
}
