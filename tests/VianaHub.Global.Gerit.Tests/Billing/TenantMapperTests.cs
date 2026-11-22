using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using VianaHub.Global.Gerit.Application.Configuration;
using VianaHub.Global.Gerit.Application.Mappings.Billing;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.Tenant;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.TenantAddress;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.TenantContactPerson;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.TenantFiscalData;
using VianaHub.Global.Gerit.Application.Services.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using VianaHub.Global.Gerit.Infra.Data.Context;
using Xunit;

namespace VianaHub.Global.Gerit.Tests.Billing;

public class TenantMapperTests
{
    [Fact]
    public void TenantMapper_PreservesLoginAndResponseContracts()
    {
        var tenant = new TenantEntity(1, 2, "Tenant A", "a@example.test", null, null, null, 9);
        var login = TenantMapper.ToLoginResponse(tenant);
        var response = TenantMapper.ToResponse(tenant);
        var detail = TenantMapper.ToDetail(tenant);
        Assert.Equal("Tenant A", login.Name); Assert.Equal(tenant.Id, login.Id);
        Assert.Equal("Tenant A", response.Name); Assert.Null(response.AcquisitionSourceTypeName);
        Assert.Equal(tenant.CreatedBy, detail!.CreatedBy); Assert.Equal(tenant.CreatedAt, detail.CreatedAt);
        Assert.Equal("{\"Id\":0,\"Name\":\"Tenant A\"}", JsonSerializer.Serialize(login));
    }

    [Fact]
    public async Task AuthGetLoginAsync_ReturnsTheSameIdAndNameContract()
    {
        var tenant = new TenantEntity(1, 2, "Tenant Login", "login@example.test", null, null, null, 9);
        typeof(TenantEntity).GetProperty(nameof(TenantEntity.Id))!.SetValue(tenant, 73);
        var tenants = new Mock<ITenantDataRepository>();
        tenants.Setup(repo => repo.GetLoginAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { tenant });
        await using var database = new GeritDbContext(new DbContextOptionsBuilder<GeritDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var auth = new AuthAppService(new Notify(), Mock.Of<IRolePermissionDataRepository>(), Mock.Of<IRefreshTokenDataRepository>(),
            Mock.Of<IUserDataRepository>(), Mock.Of<IUserRoleDataRepository>(), tenants.Object, Mock.Of<IEmailSender>(),
            Mock.Of<ILocalizationService>(), Mock.Of<ICurrentUserService>(), Options.Create(new JwtSettings()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthAppService>.Instance, new ConfigurationBuilder().Build(), Mock.Of<IJwtKeyDataRepository>(), database,
            Mock.Of<ISecretProvider>(), Mock.Of<ISubscriptionDomainService>(), Mock.Of<IRequestTenantContext>());
        var response = Assert.Single(await auth.GetLoginAsync(CancellationToken.None));
        Assert.Equal(73, response.Id); Assert.Equal("Tenant Login", response.Name);
        Assert.Equal("{\"Id\":73,\"Name\":\"Tenant Login\"}", JsonSerializer.Serialize(response));
    }

    [Fact]
    public void TenantMapper_PagedPreservesPaginationEnvelope()
    {
        var page = TenantMapper.ToPaged(new ListPage<TenantEntity>([new TenantEntity(1, 2, "Tenant A", null, null, null, null, 9)], 3, 4, 10, 3));
        Assert.Equal((3, 4, 10, 3), (page.PageNumber, page.PageSize, page.TotalItems, page.TotalPages));
        Assert.Equal("Tenant A", Assert.Single(page.Items).Name);
    }

    [Fact]
    public void TenantAddressMapper_ResolvesLocalizedNameAndPreservesOptionalFields()
    {
        var addressType = new AddressTypeEntity("HOME", 9);
        addressType.AddTranslation(new AddressTypeTranslationsEntity(0, "pt-PT", "Casa", "Residencial"));
        addressType.AddTranslation(new AddressTypeTranslationsEntity(0, "en-US", "Home", "Residential"));
        var address = new TenantAddressesEntity(12, 3, "PT", "Rua A", null, "Lisboa", null, null, null, null, null, null, null, true, 9);
        SetNavigation(address, nameof(TenantAddressesEntity.AddressType), addressType);
        var summary = TenantAddressMapper.ToResponse(address, "en-US");
        var detail = TenantAddressMapper.ToDetail(address, "pt-BR");
        Assert.Equal("Home", summary.AddressTypeName); Assert.Equal("Casa", detail.AddressTypeName);
        Assert.Null(detail.Neighborhood); Assert.Equal("PT", detail.CountryCode);
        var page = TenantAddressMapper.ToPaged(new ListPage<TenantAddressesEntity>([address], 1, 10, 1, 1), "pt-BR");
        Assert.Equal("Casa", Assert.Single(page.Items).AddressTypeName);
        Assert.Equal("Rua A", JsonDocument.Parse(JsonSerializer.Serialize(detail)).RootElement.GetProperty("Street").GetString());
    }

    [Fact]
    public void TenantContactAndFiscalMappersPreserveOptionalAndSensitiveDetailFields()
    {
        var contact = new TenantContactPersonsEntity(12, "Ana", null, null, "CFO", null, null, false, true, 9);
        var fiscal = new TenantFiscalDataEntity(12, "123", "VAT-123", "PT50000000000000000000000", null, "PT", true, 9);
        var contactDetail = TenantContactPersonMapper.ToDetail(contact); var fiscalDetail = TenantFiscalDataMapper.ToDetail(fiscal);
        Assert.Null(contactDetail.Email); Assert.Equal("CFO", contactDetail.JobTitle);
        Assert.Equal("PT50000000000000000000000", fiscalDetail.IBAN); Assert.Equal("PT", fiscalDetail.FiscalCountry);
        Assert.True(fiscalDetail.IsVatRegistered); Assert.Equal(contact.TenantId, contactDetail.TenantId); Assert.Equal(fiscal.TenantId, fiscalDetail.TenantId);
        Assert.Single(TenantContactPersonMapper.ToPaged(new ListPage<TenantContactPersonsEntity>([contact], 1, 10, 1, 1)).Items);
        Assert.Single(TenantFiscalDataMapper.ToPaged(new ListPage<TenantFiscalDataEntity>([fiscal], 1, 10, 1, 1)).Items);
    }

    private static void SetNavigation<T>(object entity, string propertyName, T value) =>
        entity.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!.SetValue(entity, value);
}
