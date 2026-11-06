using System;
using System.Linq;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using Xunit;

namespace VianaHub.Global.Gerit.Tests.Domain.Entities.Business;

public class ClientEntityTests
{
    [Fact(DisplayName = "ClientEntity - ClientContact SetPrimary/RemovePrimary funciona")]
    [Trait("Domain", "")]
    public void ClientContact_SetPrimaryAndRemovePrimary_WorksAsExpected()
    {
        var first = new ClientContactPersonsEntity(1, 0, "Contato 1", "111", "", true, "c1@test.com", false, 7);
        var second = new ClientContactPersonsEntity(1, 0, "Contato 2", "222", "", true, "c2@test.com", false, 7);

        // Set primary on first
        first.SetPrimary(7);
        Assert.True(first.IsPrimary);

        // Set primary on second
        second.SetPrimary(7);
        Assert.True(second.IsPrimary);

        // Remove primary
        second.RemovePrimary(7);
        Assert.False(second.IsPrimary);
    }

    [Fact(DisplayName = "ClientEntity - ClientAddress SetPrimary/RemovePrimary funciona")]
    [Trait("Domain", "")]
    public void ClientAddress_SetPrimaryAndRemovePrimary_WorksAsExpected()
    {
        var first = new ClientAddressesEntity(1, 0, 1, "PT", "Rua 1", "10", "", "Centro", "Porto", "Porto", "4000-001", null, null, "", false, 7);
        var second = new ClientAddressesEntity(1, 0, 2, "PT", "Rua 2", "20", "", "Centro", "Lisboa", "Lisboa", "1000-001", null, null, "", false, 7);

        first.SetPrimary(7);
        Assert.True(first.IsPrimary);

        second.SetPrimary(7);
        Assert.True(second.IsPrimary);

        second.RemovePrimary(7);
        Assert.False(second.IsPrimary);
    }

    [Fact(DisplayName = "ClientEntity - Delete é root-only e não depende de children carregados")]
    [Trait("Domain", "Lifecycle")]
    public void Delete_DoesNotTouchChildNavigations()
    {
        var client = CreateClient();

        client.Delete(7);

        Assert.True(client.IsDeleted);
        Assert.False(client.IsActive);
        Assert.Empty(client.Contacts);
        Assert.Empty(client.Addresses);
    }

    private static ClientEntity CreateClient()
    {
        return new ClientEntity(
            tenantId: 1,
            partyTypeId: 1,
            acquisitionSourceTypeId: 1,
            imageUrl: null,
            note: null,
            name: "Test Client",
            phoneNumber: null,
            cellPhoneNumber: null,
            isCellPhoneWhatsapp: false,
            email: null,
            websiteUrl: null,
            birthDate: null,
            gender: null,
            nationality: null,
            companyRegistrationNumber: null,
            economicActivityCode: null,
            numberOfEmployees: null,
            statusDefinitionId: 0,
            statusDomainId: 0,
            createdBy: 7);
    }
}
