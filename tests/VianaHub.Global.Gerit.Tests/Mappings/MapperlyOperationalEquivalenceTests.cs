using System.Globalization;
using System.Text.Json;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Equipment;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Vehicle;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitAddress;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Visit;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitAttachment;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitContactPersons;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeam;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamEmployee;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamEquipments;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamVehicle;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamVehicles;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamFunction;
using VianaHub.Global.Gerit.Application.Mappings.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using Xunit;

namespace VianaHub.Global.Gerit.Tests.Mappings;

public sealed class MapperlyOperationalEquivalenceTests
{
    [Fact]
    public void Equipment_MapsSummaryDetailAndPageContract()
    {
        var entity = new EquipmentEntity(17, 8, 9, 10, "Generator", null, null, 11);
        var summary = EquipmentMapper.ToResponse(entity);
        var detail = EquipmentMapper.ToDetail(entity, CultureInfo.CurrentCulture.Name);
        var page = EquipmentMapper.ToPaged(new ListPage<EquipmentEntity>([entity], 2, 5, 6, 2));
        Assert.Equal(entity.Id, summary.Id); Assert.Equal(entity.Name, summary.Name); Assert.True(summary.IsActive);
        Assert.Equal(entity.TenantId, detail.TenantId); Assert.Null(detail.SerialNumber); Assert.Null(detail.UrlImage);
        Assert.Equal(2, page.PageNumber); Assert.Equal(6, page.TotalItems); Assert.Single(page.Items);
        Assert.Empty(EquipmentMapper.ToResponses(Array.Empty<EquipmentEntity>()));
        AssertJsonProperty(summary, "Name", "Generator"); AssertJsonProperty(detail, "TenantId", "17");
    }

    [Fact]
    public void Vehicle_MapsSummaryDetailAndPageContract()
    {
        var entity = new VehicleEntity(17, 9, 10, "AA-00-BB", null, "Model X", 0, null, null, 11);
        var summary = VehicleMapper.ToResponse(entity); var detail = VehicleMapper.ToDetail(entity, CultureInfo.CurrentCulture.Name);
        var page = VehicleMapper.ToPaged(new ListPage<VehicleEntity>([entity], 1, 10, 1, 1));
        Assert.Equal("AA-00-BB", summary.Plate); Assert.Null(summary.Brand); Assert.Equal(0, detail.Year);
        Assert.Equal(entity.TenantId, detail.TenantId); Assert.Null(detail.Color); Assert.Single(page.Items);
        AssertJsonProperty(summary, "Plate", "AA-00-BB"); AssertJsonProperty(detail, "TenantId", "17");
    }

    [Fact]
    public void VisitAddress_MapsResponseDetailPageAndNullNavigations()
    {
        var entity = new VisitAddressesEntity(17, 23, 3, null, "Rua", null, null, null, "Porto", null, null, null, null, null, true, 11);
        var summary = VisitAddressMapper.ToResponse(entity, CultureInfo.CurrentCulture.Name); var detail = VisitAddressMapper.ToDetail(entity, CultureInfo.CurrentCulture.Name);
        var page = VisitAddressMapper.ToPaged(new ListPage<VisitAddressesEntity>([entity], 1, 10, 1, 1), CultureInfo.CurrentCulture.Name);
        Assert.Equal(17, detail.TenantId); Assert.Equal(23, detail.VisitId); Assert.Null(detail.Visit); Assert.Null(summary.AddressTypeName);
        Assert.Single(page.Items); AssertJsonProperty(summary, "Street", "Rua"); AssertJsonProperty(detail, "VisitId", "23");
    }

    [Fact]
    public void VisitAttachment_MapsInitOnlySummaryDetailAndNullFileType()
    {
        var entity = new VisitAttachmentsEntity(17, 4, 23, "key", "file.pdf", 1024, 0, true, 11);
        var summary = VisitAttachmentMapper.ToResponse(entity, CultureInfo.CurrentCulture.Name); var detail = VisitAttachmentMapper.ToDetail(entity, CultureInfo.CurrentCulture.Name);
        var page = VisitAttachmentMapper.ToPaged(new ListPage<VisitAttachmentsEntity>([entity], 1, 10, 1, 1), CultureInfo.CurrentCulture.Name);
        Assert.Equal(entity.Id, summary.Id); Assert.Equal("file.pdf", summary.FileName); Assert.Null(summary.MimeType); Assert.Null(summary.FileTypeName);
        Assert.Equal(23, detail.VisitId); Assert.Equal(17, detail.TenantId); Assert.Equal(1024, detail.FileSizeBytes);
        Assert.Single(page.Items); AssertJsonProperty(summary, "FileName", "file.pdf"); AssertJsonProperty(detail, "VisitId", "23");
    }

    [Fact]
    public void VisitContactPerson_MapsResponseDetailPageAndOptionalNulls()
    {
        var entity = new VisitContactPersonsEntity(17, 23, "Contact", null, null, null, null, null, false, true, 11);
        var summary = VisitContactPersonMapper.ToResponse(entity); var detail = VisitContactPersonMapper.ToDetail(entity);
        var page = VisitContactPersonMapper.ToPaged(new ListPage<VisitContactPersonsEntity>([entity], 1, 10, 1, 1));
        Assert.Equal("Contact", summary.Name); Assert.Null(summary.CellPhoneNumber); Assert.Equal(11, detail.CreatedBy); Assert.Null(detail.CellPhoneNumber);
        Assert.Single(page.Items); AssertJsonProperty(summary, "Name", "Contact"); AssertJsonProperty(detail, "CreatedBy", "11");
    }

    [Fact]
    public void VisitTeam_MapsResponseDetailPageAndMissingNavigations()
    {
        var entity = new VisitTeamEntity(17, 23, 31, new DateTime(2026, 1, 2, 3, 4, 0, DateTimeKind.Utc), 11);
        var summary = VisitTeamMapper.ToResponse(entity); var detail = VisitTeamMapper.ToDetail(entity);
        var page = VisitTeamMapper.ToPaged(new ListPage<VisitTeamEntity>([entity], 1, 10, 1, 1));
        Assert.Null(summary.Team); Assert.Null(summary.Visit); Assert.Equal(23, detail.VisitId); Assert.Equal(17, detail.TenantId);
        Assert.Single(page.Items); AssertJsonProperty(summary, "Visit", ""); AssertJsonProperty(detail, "TenantId", "17");
    }

    [Fact]
    public void VisitTeamEmployee_MapsInitOnlyRelationsDetailPageAndNulls()
    {
        var entity = new VisitTeamEmployeeEntity(17, 23, 31, 7, false, DateTime.UnixEpoch, 11);
        var summary = VisitTeamEmployeeMapper.ToResponse(entity); var detail = VisitTeamEmployeeMapper.ToDetail(entity);
        var page = VisitTeamEmployeeMapper.ToPaged(new ListPage<VisitTeamEmployeeEntity>([entity], 1, 10, 1, 1));
        Assert.Null(summary.EmployeeName); Assert.Null(summary.VisitTeamFunctionName); Assert.Equal(31, detail.EmployeeId); Assert.Equal(17, detail.TenantId);
        Assert.Single(page.Items); AssertJsonProperty(summary, "EmployeeName", ""); AssertJsonProperty(detail, "TenantId", "17");
    }

    [Fact]
    public void VisitTeamEquipment_MapsCorrectContractIdsRelationsDetailAndPage()
    {
        var entity = new VisitTeamEquipmentEntity(17, 23, 31, 11);
        var summary = VisitTeamEquipmentMapper.ToResponse(entity); var detail = VisitTeamEquipmentMapper.ToDetail(entity);
        var page = VisitTeamEquipmentMapper.ToPaged(new ListPage<VisitTeamEquipmentEntity>([entity], 1, 10, 1, 1));
        Assert.Equal(23, detail.VisitTeamId); Assert.Equal(31, detail.EquipmentId); Assert.Equal(17, detail.TenantId);
        Assert.Null(summary.Equipment); Assert.Null(summary.VisitTeam); Assert.Single(page.Items);
        AssertJsonProperty(summary, "Id", entity.Id.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void VisitTeamVehicle_MapsSummaryDetailAndPage()
    {
        var entity = new VisitTeamVehicleEntity(17, 23, 31, 11);
        var summary = VisitTeamVehicleMapper.ToResponse(entity); var detail = VisitTeamVehicleMapper.ToDetail(entity);
        var page = VisitTeamVehicleMapper.ToPaged(new ListPage<VisitTeamVehicleEntity>([entity], 1, 10, 1, 1));
        Assert.Equal(23, detail.VisitTeamId); Assert.Equal(31, detail.VehicleId); Assert.Equal(17, detail.TenantId);
        Assert.Null(summary.Vehicle); Assert.Null(detail.VisitTeam); Assert.Single(page.Items);
        AssertJsonProperty(detail, "VehicleId", "31");
    }

    [Fact]
    public void VisitTeamFunction_MapsResponseDetailPageAndOptionalDescription()
    {
        var entity = new VisitTeamFunctionsEntity(17, "Operator", null, 11);
        var summary = VisitTeamFunctionMapper.ToResponse(entity); var detail = VisitTeamFunctionMapper.ToDetail(entity);
        var page = VisitTeamFunctionMapper.ToPaged(new ListPage<VisitTeamFunctionsEntity>([entity], 1, 10, 1, 1));
        Assert.Equal("Operator", summary.Name); Assert.Null(summary.VisitTeam); Assert.Equal(17, detail.TenantId); Assert.Null(detail.Description);
        Assert.Single(page.Items); AssertJsonProperty(summary, "Name", "Operator"); AssertJsonProperty(detail, "TenantId", "17");
    }

    [Fact]
    public void LoadedNavigations_MapDerivedNamesWithoutLosingIdsOrTenant()
    {
        var visit = new VisitEntity(17, 2, 3, 4, "EUR", "Inspection", null, DateTime.UnixEpoch, 0m, 11);
        var team = new TeamEntity(17, "Ops", null, 11);
        var assignment = new VisitTeamEntity(17, 23, 31, DateTime.UnixEpoch, 11);
        SetNavigation(assignment, nameof(VisitTeamEntity.Visit), visit);
        SetNavigation(assignment, nameof(VisitTeamEntity.Team), team);
        var assignedTeam = VisitTeamMapper.ToDetail(assignment);
        Assert.Equal("Inspection", assignedTeam.Visit);
        Assert.Equal("Ops", assignedTeam.Team);
        Assert.Equal(17, assignedTeam.TenantId);

        var equipment = new EquipmentEntity(17, 8, 9, 10, "Pump", null, null, 11);
        var equipmentType = new EquipmentTypeEntity(17, "Generator", null, 11);
        SetNavigation(equipment, nameof(EquipmentEntity.EquipmentType), equipmentType);
        Assert.Equal("Generator", EquipmentMapper.ToResponse(equipment).EquipmentType);

        var vehicle = new VehicleEntity(17, 9, 10, "AA-00-BB", null, null, 0, null, null, 11);
        var status = new StatusDefinitionEntity(17, 4, "ACTIVE", 1, false, 11);
        status.Translations.Add(new StatusDefinitionTranslationsEntity(17, 4, 9, "en-US", "Active", null));
        SetNavigation(vehicle, nameof(VehicleEntity.StatusDefinition), status);
        Assert.Equal("Active", VehicleMapper.ToDetail(vehicle, "en-US").StatusDefinitionName);

        var employee = new EmployeeEntity(17, 4, 5, "Taylor", null, "555", true, null, null, 11);
        var function = new VisitTeamFunctionsEntity(17, "Technician", null, 11);
        var assignedEmployee = new VisitTeamEmployeeEntity(17, 23, 31, 7, false, DateTime.UnixEpoch, 11);
        SetNavigation(assignedEmployee, nameof(VisitTeamEmployeeEntity.Employee), employee);
        SetNavigation(assignedEmployee, nameof(VisitTeamEmployeeEntity.Function), function);
        Assert.Equal("Taylor", VisitTeamEmployeeMapper.ToResponse(assignedEmployee).EmployeeName);
        Assert.Equal("Technician", VisitTeamEmployeeMapper.ToResponse(assignedEmployee).VisitTeamFunctionName);

        var teamEquipment = new VisitTeamEquipmentEntity(17, 23, equipment.Id, 11);
        SetNavigation(teamEquipment, nameof(VisitTeamEquipmentEntity.VisitTeam), assignment);
        SetNavigation(teamEquipment, nameof(VisitTeamEquipmentEntity.Equipment), equipment);
        Assert.Equal("Inspection", VisitTeamEquipmentMapper.ToDetail(teamEquipment).VisitTeam);
        Assert.Equal("Pump", VisitTeamEquipmentMapper.ToDetail(teamEquipment).Equipment);

        var teamVehicle = new VisitTeamVehicleEntity(17, 23, vehicle.Id, 11);
        SetNavigation(teamVehicle, nameof(VisitTeamVehicleEntity.VisitTeam), assignment);
        SetNavigation(teamVehicle, nameof(VisitTeamVehicleEntity.Vehicle), vehicle);
        Assert.Equal("Inspection", VisitTeamVehicleMapper.ToDetail(teamVehicle).VisitTeam);
        Assert.Equal("AA-00-BB", VisitTeamVehicleMapper.ToDetail(teamVehicle).Vehicle);
    }

    [Theory]
    [InlineData("pt-PT", "Pendente", "Estado visita")]
    [InlineData("pt-BR", "Pendente BR", "Estado visita BR")]
    [InlineData("en-US", "Pending", "Visit status")]
    [InlineData("es-ES", "Pendiente", "Estado visita")]
    public void Visit_MapsSummaryDetailAndPageForEachSupportedCulture(string culture, string statusName, string domainName)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var entity = CreateVisit();
            var summary = VisitMapper.ToResponse(entity);
            var detail = VisitMapper.ToDetail(entity, culture);
            var page = VisitMapper.ToPaged(new ListPage<VisitEntity>([entity], 2, 5, 6, 2));

            Assert.Equal("Client A", summary.ClientName);
            Assert.Equal("Inspection", summary.Title);
            Assert.Equal(17, detail.TenantId);
            Assert.Equal(2, detail.ClientId);
            Assert.Equal("Client A", detail.Client);
            Assert.Equal(statusName, detail.StatusDefinitionName);
            Assert.Equal(statusName, detail.StatusDefinition);
            Assert.Equal(domainName, detail.StatusDomainName);
            Assert.Equal(125.50m, detail.EstimatedValue);
            Assert.Null(detail.RealValue);
            Assert.Equal(2, page.PageNumber);
            Assert.Equal(6, page.TotalItems);
            Assert.Single(page.Items);
            Assert.Empty(VisitMapper.ToResponses(Array.Empty<VisitEntity>()));
            AssertJsonProperty(summary, "ClientName", "Client A");
            AssertJsonProperty(detail, "StatusDefinitionName", statusName);
            AssertJsonProperty(detail, "EstimatedValue", "125.50");
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }
    }

    [Fact]
    public void Visit_DetailPreservesNullClientAndStatusNavigations()
    {
        var entity = CreateVisit();
        SetNavigation<VisitEntity, ClientEntity?>(entity, nameof(VisitEntity.Client), null);
        SetNavigation<VisitEntity, StatusDefinitionEntity?>(entity, nameof(VisitEntity.StatusDefinition), null);
        var detail = VisitMapper.ToDetail(entity, "pt-PT");
        Assert.Null(detail.Client);
        Assert.Null(detail.StatusDefinitionName);
        Assert.Null(detail.StatusDomainName);
        Assert.Null(detail.StatusDefinition);
    }

    private static VisitEntity CreateVisit()
    {
        var visit = new VisitEntity(17, 2, 9, 4, "EUR", "Inspection", null, new DateTime(2026, 3, 4, 5, 6, 0, DateTimeKind.Utc), 125.50m, 11);
        var client = new ClientEntity(17, 1, 2, null, null, "Client A", null, null, false, null, null, null, null, null, null, null, null, 9, 4, 11);
        SetNavigation(visit, nameof(VisitEntity.Client), client);
        var status = new StatusDefinitionEntity(17, 4, "PENDING", 1, false, 11);
        status.Translations.Add(new StatusDefinitionTranslationsEntity(17, 4, 9, "pt-PT", "Pendente", null));
        status.Translations.Add(new StatusDefinitionTranslationsEntity(17, 4, 9, "pt-BR", "Pendente BR", null));
        status.Translations.Add(new StatusDefinitionTranslationsEntity(17, 4, 9, "en-US", "Pending", null));
        status.Translations.Add(new StatusDefinitionTranslationsEntity(17, 4, 9, "es-ES", "Pendiente", null));
        var domain = new StatusDomainEntity("VISIT", 11);
        domain.Translations.Add(new StatusDomainTranslationsEntity(4, "pt-PT", "Estado visita", null));
        domain.Translations.Add(new StatusDomainTranslationsEntity(4, "pt-BR", "Estado visita BR", null));
        domain.Translations.Add(new StatusDomainTranslationsEntity(4, "en-US", "Visit status", null));
        domain.Translations.Add(new StatusDomainTranslationsEntity(4, "es-ES", "Estado visita", null));
        SetNavigation(status, nameof(StatusDefinitionEntity.StatusDomain), domain);
        SetNavigation(visit, nameof(VisitEntity.StatusDefinition), status);
        return visit;
    }

    private static void SetNavigation<TEntity, TNavigation>(TEntity entity, string property, TNavigation value) =>
        typeof(TEntity).GetProperty(property)!.SetValue(entity, value);

    private static void AssertJsonProperty<T>(T dto, string name, string expected)
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto));
        Assert.Equal(expected, json.RootElement.GetProperty(name).ToString());
    }
}
