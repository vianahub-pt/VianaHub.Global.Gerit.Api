using System.Globalization;
using System.Reflection;
using System.Text.Json;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Employee;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeAddress;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeContact;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeFiscalData;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeTeams;
using VianaHub.Global.Gerit.Application.Mappings.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using Xunit;

namespace VianaHub.Global.Gerit.Tests.Employees;

public class EmployeeMappingEquivalenceTests
{
    [Theory]
    [InlineData("pt-PT")]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    [InlineData("es-ES")]
    public void Employee_SummaryDetailAndPage_PreserveFieldsAndJson(string culture)
    {
        WithCulture(culture, () =>
        {
            var employee = CreateEmployee();
            var summary = EmployeeMapper.ToResponse(employee);
            var detail = EmployeeMapper.ToDetail(employee, culture);
            var page = EmployeeMapper.ToPaged(new ListPage<EmployeeEntity>([employee], 2, 5, 6, 2));
            Assert.Equal(employee.Id, summary.Id); Assert.Equal("Employee A", summary.Name);
            Assert.Equal(17, detail.TenantId); Assert.Equal("Ativo", detail.StatusDefinitionName);
            Assert.Equal("Funcionário", detail.StatusDomainName); Assert.Equal(2, page.PageNumber); Assert.Equal(6, page.TotalItems);
            Assert.Equal("Employee A", JsonDocument.Parse(JsonSerializer.Serialize(summary)).RootElement.GetProperty("Name").GetString());
        });
    }

    [Theory]
    [InlineData("pt-PT", "Casa")]
    [InlineData("pt-BR", "Casa")]
    [InlineData("en-US", "Home")]
    [InlineData("es-ES", "Casa")]
    public void EmployeeAddress_MapsLocalizedNamesAndOptionalFields(string culture, string expectedName)
    {
        var addressType = new AddressTypeEntity("HOME", 1);
        addressType.AddTranslation(new AddressTypeTranslationsEntity(1, "pt-PT", "Casa", "Residencial"));
        addressType.AddTranslation(new AddressTypeTranslationsEntity(1, "en-US", "Home", "Residential"));
        var address = new EmployeeAddressesEntity(17, 23, 3, "PT", "Rua", null, null, null, "Porto", null, null, null, null, null, true, 7);
        Set(address, nameof(EmployeeAddressesEntity.AddressType), addressType);
        Set(address, nameof(EmployeeAddressesEntity.Employee), CreateEmployee());
        WithCulture(culture, () =>
        {
            var summary = EmployeeAddressMapper.ToResponse(address, culture);
            var detail = EmployeeAddressMapper.ToDetail(address, culture);
            var page = EmployeeAddressMapper.ToPaged(new ListPage<EmployeeAddressesEntity>([address], 1, 10, 1, 1), culture);
            Assert.Equal(expectedName, summary.AddressTypeName); Assert.Equal(expectedName, detail.AddressType);
            Assert.Equal("Employee A", detail.Employee); Assert.Null(detail.Neighborhood); Assert.Single(page.Items);
            Assert.Equal(expectedName, JsonDocument.Parse(JsonSerializer.Serialize(summary)).RootElement.GetProperty("AddressTypeName").GetString());
        });
    }

    [Fact]
    public void EmployeeContactFiscalAndTeam_MapNullsRelationsAuditAndPagination()
    {
        var contact = new EmployeeContactPersonsEntity(17, 23, "Contact", null, null, null, null, null, false, true, 7);
        var fiscal = new EmployeeFiscalDataEntity(17, 23, "TAX", null, "PT", false, null, null, 7);
        var team = new EmployeeTeamEntity(17, 31, 23, true, new DateTime(2026, 1, 2, 3, 4, 0, DateTimeKind.Utc), 7);
        Set(team, nameof(EmployeeTeamEntity.Team), new TeamEntity(17, "Team A", null, 7));
        Set(team, nameof(EmployeeTeamEntity.Employee), CreateEmployee());
        Assert.Null(EmployeeContactPersonMapper.ToResponse(contact).CellPhoneNumber);
        Assert.Equal(7, EmployeeContactPersonMapper.ToDetail(contact).CreatedBy);
        Assert.Equal("TAX", EmployeeFiscalDataMapper.ToDetail(fiscal).TaxNumber);
        Assert.Equal("Team A", EmployeeTeamMapper.ToResponse(team).Team);
        Assert.Equal("Employee A", EmployeeTeamMapper.ToDetail(team).Employee);
        Assert.Equal(31, EmployeeTeamMapper.ToDetail(team).TeamId);
        var page = EmployeeTeamMapper.ToPaged(new ListPage<EmployeeTeamEntity>([team], 1, 10, 1, 1));
        Assert.Equal(team.Id, page.Items.Single().Id);
        Assert.Contains("Team A", JsonSerializer.Serialize(page));
    }

    private static EmployeeEntity CreateEmployee()
    {
        var employee = new EmployeeEntity(17, 4, 5, "Employee A", null, "555", true, null, null, 7);
        var status = new StatusDefinitionEntity(17, 5, "ACTIVE", 1, false, 7);
        status.Translations.Add(new StatusDefinitionTranslationsEntity(17, 5, 4, "pt-PT", "Ativo", null));
        var statusDomain = new StatusDomainEntity("EMPLOYEE", 7);
        statusDomain.Translations.Add(new StatusDomainTranslationsEntity(5, "pt-PT", "Funcionário", null));
        Set(status, nameof(StatusDefinitionEntity.StatusDomain), statusDomain);
        Set(employee, nameof(EmployeeEntity.StatusDefinition), status);
        return employee;
    }

    private static void Set(object target, string propertyName, object? value) =>
        target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!.SetValue(target, value);

    private static void WithCulture(string culture, Action action)
    {
        var originalCulture = CultureInfo.CurrentCulture; var originalUiCulture = CultureInfo.CurrentUICulture;
        try { CultureInfo.CurrentCulture = new CultureInfo(culture); CultureInfo.CurrentUICulture = new CultureInfo(culture); action(); }
        finally { CultureInfo.CurrentCulture = originalCulture; CultureInfo.CurrentUICulture = originalUiCulture; }
    }
}
