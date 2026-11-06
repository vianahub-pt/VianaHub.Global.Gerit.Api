using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using VianaHub.Global.Gerit.Api.Configuration;
using VianaHub.Global.Gerit.Api.Endpoints.Business;
using VianaHub.Global.Gerit.Api.Helpers;
using VianaHub.Global.Gerit.Api.Middleware;
using VianaHub.Global.Gerit.Api.Services;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Client;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientAddress;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientFiscalData;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientContact;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientDocument;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.IoC;

namespace VianaHub.Global.Gerit.Tests.Clients;

[CollectionDefinition("Client aggregate HTTP", DisableParallelization = true)]
public sealed class ClientAggregateHttpCollection;

[Collection("Client aggregate HTTP")]
public sealed class ClientAggregateHttpFixtureTests
{
    [Fact]
    public async Task StatefulClientLifecycleUsesOneIdAndPersistsAcrossHttpRequests()
    {
        await using var fixture = await ClientAggregateHttpFixture.StartAsync();
        using var client = fixture.CreateAuthorizedClient();
        var create = await client.PostAsJsonAsync("/v1/clients", new
        {
            partyTypeId = fixture.PartyTypeId,
            acquisitionSourceTypeId = fixture.AcquisitionSourceTypeId,
            statusDomainId = fixture.StatusDomainId,
            statusDefinitionId = fixture.StatusDefinitionId,
            name = "Stateful aggregate client",
            email = "stateful@example.test",
            birthDate = "1990-01-01",
            gender = "M",
            nationality = "PT"
        });
        var createBody = await create.Content.ReadAsStringAsync();
        Assert.True(create.StatusCode == HttpStatusCode.Created, $"HTTP {(int)create.StatusCode}: {createBody}");
        var created = JsonSerializer.Deserialize<JsonElement>(createBody);
        var id = created.GetProperty("id").GetInt32();
        Assert.True(id > 0);

        var detail = await client.GetFromJsonAsync<ClientDetailResponse>($"/v1/clients/{id}");
        Assert.NotNull(detail);
        Assert.Equal(id, detail!.Id);
        Assert.Equal(fixture.PartyTypeId, detail.PartyTypeId);
        Assert.Equal(fixture.AcquisitionSourceTypeId, detail.AcquisitionSourceTypeId);
        Assert.Equal(fixture.StatusDomainId, detail.StatusDomainId);
        Assert.Equal(fixture.StatusDefinitionId, detail.StatusDefinitionId);

        var update = new
        {
            partyTypeId = detail.PartyTypeId,
            acquisitionSourceTypeId = detail.AcquisitionSourceTypeId,
            urlImage = detail.UrlImage,
            note = detail.Note,
            name = "Stateful aggregate client updated",
            phoneNumber = detail.PhoneNumber,
            cellPhoneNumber = detail.CellPhoneNumber,
            isCellPhoneWhatsapp = detail.IsCellPhoneWhatsapp,
            email = detail.Email,
            websiteUrl = detail.WebsiteUrl,
            birthDate = detail.BirthDate,
            gender = detail.Gender,
            nationality = detail.Nationality,
            companyRegistrationNumber = detail.CompanyRegistrationNumber,
            economicActivityCode = detail.EconomicActivityCode,
            numberOfEmployees = detail.NumberOfEmployees,
            statusDefinitionId = detail.StatusDefinitionId,
            statusDomainId = detail.StatusDomainId
        };
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/v1/clients/{id}", update)).StatusCode);
        var updated = await client.GetFromJsonAsync<ClientDetailResponse>($"/v1/clients/{id}");
        Assert.NotNull(updated);
        Assert.Equal(id, updated!.Id);
        Assert.Equal("Stateful aggregate client updated", updated.Name);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/clients/{id}/deactivate", null)).StatusCode);
        Assert.False((await client.GetFromJsonAsync<ClientDetailResponse>($"/v1/clients/{id}"))!.IsActive);
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/clients/{id}/activate", null)).StatusCode);
        Assert.True((await client.GetFromJsonAsync<ClientDetailResponse>($"/v1/clients/{id}"))!.IsActive);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/v1/clients/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/clients/{id}")).StatusCode);
    }

    [Fact]
    public async Task ClientContactPersonUsesRealHttpLifecycleAndHonorsParentStates()
    {
        await using var fixture = await ClientAggregateHttpFixture.StartAsync();
        using var client = fixture.CreateAuthorizedClient();

        var activeClientId = await fixture.CreateClientAsync(client);
        var contactId = await fixture.CreateContactAsync(client, activeClientId, "active-contact@example.test");
        var detail = await client.GetFromJsonAsync<ClientContactPersonDetailResponse>($"/v1/clients/{activeClientId}/contacts/{contactId}");
        Assert.NotNull(detail);
        Assert.Equal(activeClientId, detail!.ClientId);
        Assert.Equal(1, detail.TenantId);

        var unauthorized = fixture.CreateAuthorizedClient(contactPermissions: ["Create", "Update", "Activate", "Deactivate", "Delete"]);
        Assert.Equal(HttpStatusCode.Forbidden, (await unauthorized.GetAsync($"/v1/clients/{activeClientId}/contacts/{contactId}")).StatusCode);
        unauthorized.Dispose();

        var update = new
        {
            name = "Updated contact",
            email = detail.Email,
            phoneNumber = detail.PhoneNumber,
            cellPhoneNumber = detail.CellPhoneNumber,
            isWhatsapp = detail.IsWhatsapp,
            jobTitle = "Updated title",
            department = detail.Department,
            isCellPhoneWhatsapp = detail.IsCellPhoneWhatsapp,
            isPrimary = detail.IsPrimary
        };
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/v1/clients/{activeClientId}/contacts/{contactId}", update)).StatusCode);
        var updated = await client.GetFromJsonAsync<ClientContactPersonDetailResponse>($"/v1/clients/{activeClientId}/contacts/{contactId}");
        Assert.Equal("Updated title", updated!.JobTitle);
        Assert.True((await client.PatchAsync($"/v1/clients/{activeClientId}/contacts/{contactId}/deactivate", null)).IsSuccessStatusCode);
        Assert.False((await client.GetFromJsonAsync<ClientContactPersonDetailResponse>($"/v1/clients/{activeClientId}/contacts/{contactId}"))!.IsActive);
        Assert.True((await client.PatchAsync($"/v1/clients/{activeClientId}/contacts/{contactId}/activate", null)).IsSuccessStatusCode);
        Assert.True((await client.GetFromJsonAsync<ClientContactPersonDetailResponse>($"/v1/clients/{activeClientId}/contacts/{contactId}"))!.IsActive);
        using (var activeBulk = fixture.BulkContent("active-bulk@example.test"))
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/v1/clients/{activeClientId}/contacts/bulk-upload", activeBulk)).StatusCode);
        Assert.True((await client.DeleteAsync($"/v1/clients/{activeClientId}/contacts/{contactId}")).IsSuccessStatusCode);

        var inactiveClientId = await fixture.CreateClientAsync(client);
        var inactiveContactId = await fixture.CreateContactAsync(client, inactiveClientId, "inactive-contact@example.test");
        Assert.True((await client.PatchAsync($"/v1/clients/{inactiveClientId}/deactivate", null)).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/v1/clients/{inactiveClientId}/contacts/{inactiveContactId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync($"/v1/clients/{inactiveClientId}/contacts", fixture.ContactRequest("blocked-create@example.test"))).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PutAsJsonAsync($"/v1/clients/{inactiveClientId}/contacts/{inactiveContactId}", update)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/clients/{inactiveClientId}/contacts/{inactiveContactId}/activate", null)).StatusCode);
        using (var inactiveBulk = fixture.BulkContent("inactive-bulk@example.test"))
            Assert.Equal(HttpStatusCode.Gone, (await client.PostAsync($"/v1/clients/{inactiveClientId}/contacts/bulk-upload", inactiveBulk)).StatusCode);
        Assert.True((await client.PatchAsync($"/v1/clients/{inactiveClientId}/contacts/{inactiveContactId}/deactivate", null)).IsSuccessStatusCode);
        Assert.True((await client.DeleteAsync($"/v1/clients/{inactiveClientId}/contacts/{inactiveContactId}")).IsSuccessStatusCode);

        var deletedClientId = await fixture.CreateClientAsync(client);
        var deletedContactId = await fixture.CreateContactAsync(client, deletedClientId, "deleted-contact@example.test");
        Assert.True((await client.DeleteAsync($"/v1/clients/{deletedClientId}")).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/clients/{deletedClientId}/contacts/{deletedContactId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync($"/v1/clients/{deletedClientId}/contacts", fixture.ContactRequest("deleted-create@example.test"))).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PutAsJsonAsync($"/v1/clients/{deletedClientId}/contacts/{deletedContactId}", update)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/clients/{deletedClientId}/contacts/{deletedContactId}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/clients/{deletedClientId}/contacts/{deletedContactId}/deactivate", null)).StatusCode);
        using (var deletedBulk = fixture.BulkContent("deleted-bulk@example.test"))
            Assert.Equal(HttpStatusCode.Gone, (await client.PostAsync($"/v1/clients/{deletedClientId}/contacts/bulk-upload", deletedBulk)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.DeleteAsync($"/v1/clients/{deletedClientId}/contacts/{deletedContactId}")).StatusCode);

        using var ptClient = fixture.CreateAuthorizedClient(culture: "pt-PT");
        using var enClient = fixture.CreateAuthorizedClient(culture: "en-US");
        var ptError = await ptClient.PostAsJsonAsync($"/v1/clients/{activeClientId}/contacts", fixture.ContactRequest("localized-pt@example.test", name: null));
        var enError = await enClient.PostAsJsonAsync($"/v1/clients/{activeClientId}/contacts", fixture.ContactRequest("localized-en@example.test", name: null));
        Assert.Equal(HttpStatusCode.BadRequest, ptError.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, enError.StatusCode);
        Assert.NotEqual(await ptError.Content.ReadAsStringAsync(), await enError.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ClientAddressUsesRealHttpLifecycleAndHonorsParentStates()
    {
        await using var fixture = await ClientAggregateHttpFixture.StartAsync();
        using var client = fixture.CreateAuthorizedClient();

        var activeClientId = await fixture.CreateClientAsync(client);
        var addressId = await fixture.CreateAddressAsync(client, activeClientId, "Rua Active", isPrimary: true);
        var detail = await client.GetFromJsonAsync<ClientAddressDetailResponse>($"/v1/clients/{activeClientId}/addresses/{addressId}");
        Assert.NotNull(detail);
        Assert.Equal(activeClientId, detail!.ClientId);
        Assert.Equal(1, detail.TenantId);
        Assert.True(detail.IsPrimary);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/v1/clients/{activeClientId}/addresses", fixture.AddressRequest("Rua Duplicate", isPrimary: true))).StatusCode);

        var update = fixture.AddressRequest("Rua Updated", isPrimary: true);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/v1/clients/{activeClientId}/addresses/{addressId}", update)).StatusCode);
        Assert.Equal("Rua Updated", (await client.GetFromJsonAsync<ClientAddressDetailResponse>($"/v1/clients/{activeClientId}/addresses/{addressId}"))!.Street);
        Assert.True((await client.PatchAsync($"/v1/clients/{activeClientId}/addresses/{addressId}/deactivate", null)).IsSuccessStatusCode);
        Assert.False((await client.GetFromJsonAsync<ClientAddressDetailResponse>($"/v1/clients/{activeClientId}/addresses/{addressId}"))!.IsActive);
        Assert.True((await client.PatchAsync($"/v1/clients/{activeClientId}/addresses/{addressId}/activate", null)).IsSuccessStatusCode);
        Assert.True((await client.GetFromJsonAsync<ClientAddressDetailResponse>($"/v1/clients/{activeClientId}/addresses/{addressId}"))!.IsActive);
        using (var activeBulk = fixture.AddressBulkContent("Rua Bulk Active"))
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/v1/clients/{activeClientId}/addresses/bulk-upload", activeBulk)).StatusCode);
        Assert.True((await client.DeleteAsync($"/v1/clients/{activeClientId}/addresses/{addressId}")).IsSuccessStatusCode);

        using var unauthorized = fixture.CreateAuthorizedClient(addressPermissions: ["GetAll", "Create", "Update", "Activate", "Deactivate", "Delete", "BulkUpload"]);
        Assert.Equal(HttpStatusCode.Forbidden, (await unauthorized.GetAsync($"/v1/clients/{activeClientId}/addresses/{addressId}")).StatusCode);

        var inactiveClientId = await fixture.CreateClientAsync(client);
        var inactiveAddressId = await fixture.CreateAddressAsync(client, inactiveClientId, "Rua Inactive");
        Assert.True((await client.PatchAsync($"/v1/clients/{inactiveClientId}/deactivate", null)).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/v1/clients/{inactiveClientId}/addresses/{inactiveAddressId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync($"/v1/clients/{inactiveClientId}/addresses", fixture.AddressRequest("Rua Blocked Create"))).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PutAsJsonAsync($"/v1/clients/{inactiveClientId}/addresses/{inactiveAddressId}", fixture.AddressRequest("Rua Blocked Update"))).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/clients/{inactiveClientId}/addresses/{inactiveAddressId}/activate", null)).StatusCode);
        using (var inactiveBulk = fixture.AddressBulkContent("Rua Bulk Inactive"))
            Assert.Equal(HttpStatusCode.Gone, (await client.PostAsync($"/v1/clients/{inactiveClientId}/addresses/bulk-upload", inactiveBulk)).StatusCode);
        Assert.True((await client.PatchAsync($"/v1/clients/{inactiveClientId}/addresses/{inactiveAddressId}/deactivate", null)).IsSuccessStatusCode);
        Assert.True((await client.DeleteAsync($"/v1/clients/{inactiveClientId}/addresses/{inactiveAddressId}")).IsSuccessStatusCode);

        var deletedClientId = await fixture.CreateClientAsync(client);
        var deletedAddressId = await fixture.CreateAddressAsync(client, deletedClientId, "Rua Deleted");
        Assert.True((await client.DeleteAsync($"/v1/clients/{deletedClientId}")).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/clients/{deletedClientId}/addresses/{deletedAddressId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync($"/v1/clients/{deletedClientId}/addresses", fixture.AddressRequest("Rua Deleted Create"))).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PutAsJsonAsync($"/v1/clients/{deletedClientId}/addresses/{deletedAddressId}", fixture.AddressRequest("Rua Deleted Update"))).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/clients/{deletedClientId}/addresses/{deletedAddressId}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/clients/{deletedClientId}/addresses/{deletedAddressId}/deactivate", null)).StatusCode);
        using (var deletedBulk = fixture.AddressBulkContent("Rua Bulk Deleted"))
            Assert.Equal(HttpStatusCode.Gone, (await client.PostAsync($"/v1/clients/{deletedClientId}/addresses/bulk-upload", deletedBulk)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.DeleteAsync($"/v1/clients/{deletedClientId}/addresses/{deletedAddressId}")).StatusCode);

        using var ptClient = fixture.CreateAuthorizedClient(culture: "pt-PT");
        using var enClient = fixture.CreateAuthorizedClient(culture: "en-US");
        var ptError = await ptClient.PostAsJsonAsync($"/v1/clients/{activeClientId}/addresses", fixture.AddressRequest(null));
        var enError = await enClient.PostAsJsonAsync($"/v1/clients/{activeClientId}/addresses", fixture.AddressRequest(null));
        Assert.Equal(HttpStatusCode.BadRequest, ptError.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, enError.StatusCode);
        Assert.NotEqual(await ptError.Content.ReadAsStringAsync(), await enError.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ClientFiscalDataUsesRealHttpLifecycleAndHonorsParentStates()
    {
        await using var fixture = await ClientAggregateHttpFixture.StartAsync();
        using var client = fixture.CreateAuthorizedClient();

        var activeClientId = await fixture.CreateClientAsync(client);
        var fiscalId = await fixture.CreateFiscalDataAsync(client, activeClientId, "PT", "TAX-ACTIVE");
        var detail = await client.GetFromJsonAsync<ClientFiscalDataDetailResponse>($"/v1/clients/{activeClientId}/fiscal-data/{fiscalId}");
        Assert.NotNull(detail);
        Assert.Equal(activeClientId, detail!.ClientId);
        Assert.Equal(1, detail.TenantId);
        Assert.Equal("TAX-ACTIVE", detail.TaxNumber);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/v1/clients/{activeClientId}/fiscal-data", fixture.FiscalRequest("TAX-DUP"))).StatusCode);

        var update = fixture.FiscalRequest("TAX-ACTIVE-UPDATED");
        var updateResponse = await client.PutAsJsonAsync($"/v1/clients/{activeClientId}/fiscal-data/{fiscalId}", update);
        var updateBody = await updateResponse.Content.ReadAsStringAsync();
        Assert.True(updateResponse.StatusCode == HttpStatusCode.OK, $"HTTP {(int)updateResponse.StatusCode}: {updateBody}");
        Assert.Equal("TAX-ACTIVE-UPDATED", (await client.GetFromJsonAsync<ClientFiscalDataDetailResponse>($"/v1/clients/{activeClientId}/fiscal-data/{fiscalId}"))!.TaxNumber);
        Assert.True((await client.PatchAsync($"/v1/clients/{activeClientId}/fiscal-data/{fiscalId}/deactivate", null)).IsSuccessStatusCode);
        Assert.False((await client.GetFromJsonAsync<ClientFiscalDataDetailResponse>($"/v1/clients/{activeClientId}/fiscal-data/{fiscalId}"))!.IsActive);
        Assert.True((await client.PatchAsync($"/v1/clients/{activeClientId}/fiscal-data/{fiscalId}/activate", null)).IsSuccessStatusCode);
        Assert.True((await client.GetFromJsonAsync<ClientFiscalDataDetailResponse>($"/v1/clients/{activeClientId}/fiscal-data/{fiscalId}"))!.IsActive);
        var activeBulkClientId = await fixture.CreateClientAsync(client);
        using (var activeBulk = fixture.FiscalBulkContent("TAX-BULK-ACTIVE"))
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/v1/clients/{activeBulkClientId}/fiscal-data/bulk-upload", activeBulk)).StatusCode);
        Assert.True((await client.DeleteAsync($"/v1/clients/{activeClientId}/fiscal-data/{fiscalId}")).IsSuccessStatusCode);

        using var unauthorized = fixture.CreateAuthorizedClient(fiscalPermissions: ["GetAll", "Create", "Update", "Activate", "Deactivate", "Delete", "BulkUpload"]);
        Assert.Equal(HttpStatusCode.Forbidden, (await unauthorized.GetAsync($"/v1/clients/{activeClientId}/fiscal-data/{fiscalId}")).StatusCode);

        var inactiveClientId = await fixture.CreateClientAsync(client);
        var inactiveFiscalId = await fixture.CreateFiscalDataAsync(client, inactiveClientId, "PT", "TAX-INACTIVE");
        Assert.True((await client.PatchAsync($"/v1/clients/{inactiveClientId}/deactivate", null)).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/v1/clients/{inactiveClientId}/fiscal-data/{inactiveFiscalId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync($"/v1/clients/{inactiveClientId}/fiscal-data", fixture.FiscalRequest("TAX-BLOCKED-CREATE"))).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PutAsJsonAsync($"/v1/clients/{inactiveClientId}/fiscal-data/{inactiveFiscalId}", fixture.FiscalRequest("TAX-BLOCKED-UPDATE"))).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/clients/{inactiveClientId}/fiscal-data/{inactiveFiscalId}/activate", null)).StatusCode);
        using (var inactiveBulk = fixture.FiscalBulkContent("TAX-BULK-INACTIVE"))
            Assert.Equal(HttpStatusCode.Gone, (await client.PostAsync($"/v1/clients/{inactiveClientId}/fiscal-data/bulk-upload", inactiveBulk)).StatusCode);
        Assert.True((await client.PatchAsync($"/v1/clients/{inactiveClientId}/fiscal-data/{inactiveFiscalId}/deactivate", null)).IsSuccessStatusCode);
        Assert.True((await client.DeleteAsync($"/v1/clients/{inactiveClientId}/fiscal-data/{inactiveFiscalId}")).IsSuccessStatusCode);

        var deletedClientId = await fixture.CreateClientAsync(client);
        var deletedFiscalId = await fixture.CreateFiscalDataAsync(client, deletedClientId, "PT", "TAX-DELETED");
        Assert.True((await client.DeleteAsync($"/v1/clients/{deletedClientId}")).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/clients/{deletedClientId}/fiscal-data/{deletedFiscalId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync($"/v1/clients/{deletedClientId}/fiscal-data", fixture.FiscalRequest("TAX-DELETED-CREATE"))).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PutAsJsonAsync($"/v1/clients/{deletedClientId}/fiscal-data/{deletedFiscalId}", fixture.FiscalRequest("TAX-DELETED-UPDATE"))).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/clients/{deletedClientId}/fiscal-data/{deletedFiscalId}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/clients/{deletedClientId}/fiscal-data/{deletedFiscalId}/deactivate", null)).StatusCode);
        using (var deletedBulk = fixture.FiscalBulkContent("TAX-BULK-DELETED"))
            Assert.Equal(HttpStatusCode.Gone, (await client.PostAsync($"/v1/clients/{deletedClientId}/fiscal-data/bulk-upload", deletedBulk)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.DeleteAsync($"/v1/clients/{deletedClientId}/fiscal-data/{deletedFiscalId}")).StatusCode);

        using var ptClient = fixture.CreateAuthorizedClient(culture: "pt-PT");
        using var enClient = fixture.CreateAuthorizedClient(culture: "en-US");
        var ptError = await ptClient.PostAsJsonAsync($"/v1/clients/{activeClientId}/fiscal-data", fixture.FiscalRequest(null));
        var enError = await enClient.PostAsJsonAsync($"/v1/clients/{activeClientId}/fiscal-data", fixture.FiscalRequest(null));
        Assert.Equal(HttpStatusCode.BadRequest, ptError.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, enError.StatusCode);
        Assert.NotEqual(await ptError.Content.ReadAsStringAsync(), await enError.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ClientDocumentsUsesRealHttpLifecycleAndHonorsParentStates()
    {
        await using var fixture = await ClientAggregateHttpFixture.StartAsync();
        using var client = fixture.CreateAuthorizedClient();

        var activeClientId = await fixture.CreateClientAsync(client);
        var documentId = await fixture.CreateDocumentAsync(client, activeClientId, "DOC-ACTIVE", isPrimary: true);
        var detail = await client.GetFromJsonAsync<ClientDocumentDetailResponse>($"/v1/clients/{activeClientId}/documents/{documentId}");
        Assert.NotNull(detail);
        Assert.Equal(activeClientId, detail!.ClientId);
        Assert.Equal(1, detail.TenantId);
        Assert.Equal(fixture.DocumentTypeId, detail.DocumentTypeId);
        Assert.Equal("DOC-ACTIVE", detail.DocumentNumber);
        Assert.True(detail.IsPrimary);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/v1/clients/{activeClientId}/documents", fixture.DocumentRequest("DOC-ACTIVE", isPrimary: false))).StatusCode);

        var primaryReplacementId = await fixture.CreateDocumentAsync(client, activeClientId, "DOC-PRIMARY-REPLACEMENT", isPrimary: true);
        var replaced = await client.GetFromJsonAsync<ClientDocumentDetailResponse>($"/v1/clients/{activeClientId}/documents/{documentId}");
        Assert.NotNull(replaced);
        Assert.False(replaced!.IsPrimary);
        Assert.True((await client.PutAsJsonAsync($"/v1/clients/{activeClientId}/documents/{primaryReplacementId}", fixture.DocumentRequest("DOC-UPDATED", isPrimary: true))).IsSuccessStatusCode);
        Assert.Equal("DOC-UPDATED", (await client.GetFromJsonAsync<ClientDocumentDetailResponse>($"/v1/clients/{activeClientId}/documents/{primaryReplacementId}"))!.DocumentNumber);
        Assert.True((await client.PatchAsync($"/v1/clients/{activeClientId}/documents/{primaryReplacementId}/deactivate", null)).IsSuccessStatusCode);
        Assert.False((await client.GetFromJsonAsync<ClientDocumentDetailResponse>($"/v1/clients/{activeClientId}/documents/{primaryReplacementId}"))!.IsActive);
        Assert.True((await client.PatchAsync($"/v1/clients/{activeClientId}/documents/{primaryReplacementId}/activate", null)).IsSuccessStatusCode);
        Assert.True((await client.GetFromJsonAsync<ClientDocumentDetailResponse>($"/v1/clients/{activeClientId}/documents/{primaryReplacementId}"))!.IsActive);

        var bulkClientId = await fixture.CreateClientAsync(client);
        using (var bulk = fixture.DocumentBulkContent("DOC-BULK-ACTIVE"))
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/v1/clients/{bulkClientId}/documents/bulk-upload", bulk)).StatusCode);
        Assert.True((await client.DeleteAsync($"/v1/clients/{activeClientId}/documents/{documentId}")).IsSuccessStatusCode);

        using var unauthorized = fixture.CreateAuthorizedClient(documentPermissions: ["GetAll", "Create", "Update", "Activate", "Deactivate", "Delete", "BulkUpload"]);
        Assert.Equal(HttpStatusCode.Forbidden, (await unauthorized.GetAsync($"/v1/clients/{activeClientId}/documents/{primaryReplacementId}")).StatusCode);

        var inactiveClientId = await fixture.CreateClientAsync(client);
        var inactiveDocumentId = await fixture.CreateDocumentAsync(client, inactiveClientId, "DOC-INACTIVE");
        Assert.True((await client.PatchAsync($"/v1/clients/{inactiveClientId}/deactivate", null)).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/v1/clients/{inactiveClientId}/documents/{inactiveDocumentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync($"/v1/clients/{inactiveClientId}/documents", fixture.DocumentRequest("DOC-INACTIVE-CREATE"))).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PutAsJsonAsync($"/v1/clients/{inactiveClientId}/documents/{inactiveDocumentId}", fixture.DocumentRequest("DOC-INACTIVE-UPDATE"))).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/clients/{inactiveClientId}/documents/{inactiveDocumentId}/activate", null)).StatusCode);
        using (var inactiveBulk = fixture.DocumentBulkContent("DOC-BULK-INACTIVE"))
            Assert.Equal(HttpStatusCode.Gone, (await client.PostAsync($"/v1/clients/{inactiveClientId}/documents/bulk-upload", inactiveBulk)).StatusCode);
        Assert.True((await client.PatchAsync($"/v1/clients/{inactiveClientId}/documents/{inactiveDocumentId}/deactivate", null)).IsSuccessStatusCode);
        Assert.True((await client.DeleteAsync($"/v1/clients/{inactiveClientId}/documents/{inactiveDocumentId}")).IsSuccessStatusCode);

        var deletedClientId = await fixture.CreateClientAsync(client);
        var deletedDocumentId = await fixture.CreateDocumentAsync(client, deletedClientId, "DOC-DELETED");
        Assert.True((await client.DeleteAsync($"/v1/clients/{deletedClientId}")).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/clients/{deletedClientId}/documents/{deletedDocumentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync($"/v1/clients/{deletedClientId}/documents", fixture.DocumentRequest("DOC-DELETED-CREATE"))).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PutAsJsonAsync($"/v1/clients/{deletedClientId}/documents/{deletedDocumentId}", fixture.DocumentRequest("DOC-DELETED-UPDATE"))).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/clients/{deletedClientId}/documents/{deletedDocumentId}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PatchAsync($"/v1/clients/{deletedClientId}/documents/{deletedDocumentId}/deactivate", null)).StatusCode);
        using (var deletedBulk = fixture.DocumentBulkContent("DOC-BULK-DELETED"))
            Assert.Equal(HttpStatusCode.Gone, (await client.PostAsync($"/v1/clients/{deletedClientId}/documents/bulk-upload", deletedBulk)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.DeleteAsync($"/v1/clients/{deletedClientId}/documents/{deletedDocumentId}")).StatusCode);

        using var ptClient = fixture.CreateAuthorizedClient(culture: "pt-PT");
        using var enClient = fixture.CreateAuthorizedClient(culture: "en-US");
        var ptError = await ptClient.PostAsJsonAsync($"/v1/clients/{activeClientId}/documents", fixture.DocumentRequest(null));
        var enError = await enClient.PostAsJsonAsync($"/v1/clients/{activeClientId}/documents", fixture.DocumentRequest(null));
        Assert.Equal(HttpStatusCode.BadRequest, ptError.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, enError.StatusCode);
        Assert.NotEqual(await ptError.Content.ReadAsStringAsync(), await enError.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ClientAggregateUsesOneParentAcrossAllChildrenAndHonorsRootTransitions()
    {
        await using var fixture = await ClientAggregateHttpFixture.StartAsync();
        using var client = fixture.CreateAuthorizedClient();

        var clientId = await fixture.CreateClientAsync(client);
        var contactId = await fixture.CreateContactAsync(client, clientId, "aggregate-contact@example.test");
        var addressId = await fixture.CreateAddressAsync(client, clientId, "Rua Aggregate");
        var fiscalId = await fixture.CreateFiscalDataAsync(client, clientId, "PT", "AGGREGATE-TAX");
        var documentId = await fixture.CreateDocumentAsync(client, clientId, "AGGREGATE-DOCUMENT");

        var clientDetail = await client.GetFromJsonAsync<ClientDetailResponse>($"/v1/clients/{clientId}");
        var contactDetail = await client.GetFromJsonAsync<ClientContactPersonDetailResponse>($"/v1/clients/{clientId}/contacts/{contactId}");
        var addressDetail = await client.GetFromJsonAsync<ClientAddressDetailResponse>($"/v1/clients/{clientId}/addresses/{addressId}");
        var fiscalDetail = await client.GetFromJsonAsync<ClientFiscalDataDetailResponse>($"/v1/clients/{clientId}/fiscal-data/{fiscalId}");
        var documentDetail = await client.GetFromJsonAsync<ClientDocumentDetailResponse>($"/v1/clients/{clientId}/documents/{documentId}");
        Assert.Equal(clientId, clientDetail!.Id);
        Assert.Equal(clientId, contactDetail!.ClientId);
        Assert.Equal(clientId, addressDetail!.ClientId);
        Assert.Equal(clientId, fiscalDetail!.ClientId);
        Assert.Equal(clientId, documentDetail!.ClientId);
        Assert.Equal(1, contactDetail.TenantId);
        Assert.Equal(1, addressDetail.TenantId);
        Assert.Equal(1, fiscalDetail.TenantId);
        Assert.Equal(1, documentDetail.TenantId);

        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/v1/clients/{clientId}/contacts/{contactId}", fixture.ContactRequest("aggregate-contact-updated@example.test"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/v1/clients/{clientId}/addresses/{addressId}", fixture.AddressRequest("Rua Aggregate Updated"))).StatusCode);
        var fiscalUpdate = await client.PutAsJsonAsync($"/v1/clients/{clientId}/fiscal-data/{fiscalId}", fixture.FiscalRequest("AGG-TAX-UPD"));
        var fiscalUpdateBody = await fiscalUpdate.Content.ReadAsStringAsync();
        Assert.True(fiscalUpdate.StatusCode == HttpStatusCode.OK, $"HTTP {(int)fiscalUpdate.StatusCode}: {fiscalUpdateBody}");
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/v1/clients/{clientId}/documents/{documentId}", fixture.DocumentRequest("AGGREGATE-DOCUMENT-UPDATED"))).StatusCode);
        Assert.Equal("aggregate-contact-updated@example.test", (await client.GetFromJsonAsync<ClientContactPersonDetailResponse>($"/v1/clients/{clientId}/contacts/{contactId}"))!.Email);
        Assert.Equal("Rua Aggregate Updated", (await client.GetFromJsonAsync<ClientAddressDetailResponse>($"/v1/clients/{clientId}/addresses/{addressId}"))!.Street);
        Assert.Equal("AGG-TAX-UPD", (await client.GetFromJsonAsync<ClientFiscalDataDetailResponse>($"/v1/clients/{clientId}/fiscal-data/{fiscalId}"))!.TaxNumber);
        Assert.Equal("AGGREGATE-DOCUMENT-UPDATED", (await client.GetFromJsonAsync<ClientDocumentDetailResponse>($"/v1/clients/{clientId}/documents/{documentId}"))!.DocumentNumber);

        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/clients/{clientId}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/v1/clients/{clientId}/contacts/{contactId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/v1/clients/{clientId}/addresses/{addressId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/v1/clients/{clientId}/fiscal-data/{fiscalId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/v1/clients/{clientId}/documents/{documentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync($"/v1/clients/{clientId}/documents", fixture.DocumentRequest("AGGREGATE-BLOCKED"))).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsync($"/v1/clients/{clientId}/activate", null)).StatusCode);
        Assert.True((await client.GetFromJsonAsync<ClientDetailResponse>($"/v1/clients/{clientId}"))!.IsActive);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync($"/v1/clients/{clientId}/documents", fixture.DocumentRequest("AGGREGATE-AFTER-REACTIVATION"))).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/v1/clients/{clientId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/clients/{clientId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/clients/{clientId}/contacts/{contactId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/clients/{clientId}/addresses/{addressId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/clients/{clientId}/fiscal-data/{fiscalId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/v1/clients/{clientId}/documents/{documentId}")).StatusCode);
    }
}

public sealed class ClientAggregateHttpFixture(WebApplication app, SymmetricSecurityKey key) : IAsyncDisposable
{
    private readonly Uri _address = new(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
    public int PartyTypeId { get; } = 1;
    public int AcquisitionSourceTypeId { get; } = 1;
    public int StatusDomainId { get; } = 1;
    public int StatusDefinitionId { get; } = 1;
    public int AddressTypeId { get; } = 1;
    public int DocumentTypeId { get; } = 1;

    public static async Task<ClientAggregateHttpFixture> StartAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddGeritInfrastructure();
        builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserApiService>();
        var databaseName = "client-aggregate-" + Guid.NewGuid();
        builder.Services.AddDbContext<GeritDbContext>(options => options.UseInMemoryDatabase(databaseName));
        builder.Services.AddRouteValidatorSetup();
        var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "client-aggregate-tests",
            ValidateAudience = true,
            ValidAudience = "client-aggregate-tests",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        });
        builder.Services.AddAuthorization();
        var app = builder.Build();
        await SeedAsync(app.Services);
        app.UseMiddleware<RequestLocalizationMiddleware>();
        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapClientEndpoints();
        app.MapClientContactPersonEndpoints();
        app.MapClientAddressEndpoints();
        app.MapClientFiscalDataEndpoints();
        app.MapClientDocumentEndpoints();
        await app.StartAsync();
        return new ClientAggregateHttpFixture(app, key);
    }

    public HttpClient CreateAuthorizedClient(string[]? contactPermissions = null, string[]? addressPermissions = null, string[]? fiscalPermissions = null, string[]? documentPermissions = null, string culture = "pt-PT")
    {
        var client = new HttpClient { BaseAddress = _address };
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Role, "BackOffice"), new Claim("tenant_id", "1"), new Claim("tenantId", "1"), new Claim("TenantId", "1"),
            new Claim("permissions", JsonSerializer.Serialize(new Dictionary<string, string[]>
            {
                ["Clients"] = ["GetAll", "GetBy", "GetPaged", "Create", "Update", "Activate", "Deactivate", "Delete"],
                ["ClientContactPersons"] = contactPermissions ?? ["GetAll", "GetBy", "GetPaged", "Create", "Update", "Activate", "Deactivate", "Delete", "BulkUpload"],
                ["ClientAddresses"] = addressPermissions ?? ["GetAll", "GetBy", "GetPaged", "Create", "Update", "Activate", "Deactivate", "Delete", "BulkUpload"],
                ["ClientFiscalData"] = fiscalPermissions ?? ["GetAll", "GetBy", "GetPaged", "Create", "Update", "Activate", "Deactivate", "Delete", "BulkUpload"]
                , ["ClientDocuments"] = documentPermissions ?? ["GetAll", "GetBy", "GetPaged", "Create", "Update", "Activate", "Deactivate", "Delete", "BulkUpload"]
            }), JsonClaimValueTypes.Json)
        };
        var token = new JwtSecurityToken("client-aggregate-tests", "client-aggregate-tests", claims, expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    public async Task<int> CreateClientAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/v1/clients", new
        {
            partyTypeId = PartyTypeId,
            acquisitionSourceTypeId = AcquisitionSourceTypeId,
            statusDomainId = StatusDomainId,
            statusDefinitionId = StatusDefinitionId,
            name = $"Stateful aggregate client {Guid.NewGuid():N}",
            email = $"client-{Guid.NewGuid():N}@example.test"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    public object ContactRequest(string email, string? name = "Contact") => new
    {
        name,
        email,
        phoneNumber = "351210000000",
        jobTitle = "Manager",
        department = "Operations",
        isPrimary = false,
        isCellPhoneWhatsapp = false
    };

    public MultipartFormDataContent BulkContent(string email)
    {
        var csv = "Name;Email;Phone;CellPhoneNumber;JobTitle;Department;IsCellPhoneWhatsapp;IsPrimary\nBulk Contact;" + email + ";351210000000;;;;false;false\n";
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "contacts.csv");
        return content;
    }

    public async Task<int> CreateContactAsync(HttpClient client, int clientId, string email)
    {
        var response = await client.PostAsJsonAsync($"/v1/clients/{clientId}/contacts", ContactRequest(email));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    public async Task<int> CreateAddressAsync(HttpClient client, int clientId, string street, bool isPrimary = false)
    {
        var response = await client.PostAsJsonAsync($"/v1/clients/{clientId}/addresses", AddressRequest(street, isPrimary));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    public object FiscalRequest(string? taxNumber) => new
    {
        taxNumber,
        vatNumber = "PTVAT12345",
        fiscalCountry = "PT",
        isVatRegistered = true,
        iban = "PT50000000000000000000000",
        fiscalEmail = "fiscal@example.test"
    };

    public MultipartFormDataContent FiscalBulkContent(string taxNumber)
    {
        var csv = "TaxNumber;VatNumber;FiscalCountry;IsVatRegistered;IBAN;FiscalEmail\n" +
            $"{taxNumber};PTVAT12345;PT;true;PT50000000000000000000000;bulk@example.test\n";
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "fiscal.csv");
        return content;
    }

    public async Task<int> CreateFiscalDataAsync(HttpClient client, int clientId, string country, string taxNumber)
    {
        var response = await client.PostAsJsonAsync($"/v1/clients/{clientId}/fiscal-data", FiscalRequest(taxNumber));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"HTTP {(int)response.StatusCode}: {body}");
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    public object DocumentRequest(string? documentNumber, bool isPrimary = false) => new
    {
        documentTypeId = DocumentTypeId,
        documentNumber,
        issuingCountryCode = "PT",
        issuedAt = "2020-01-01",
        expiresAt = "2030-01-01",
        isPrimary
    };

    public MultipartFormDataContent DocumentBulkContent(string documentNumber)
    {
        var csv = "DocumentTypeId;DocumentNumber;IssuingCountryCode;IssuedAt;ExpiresAt;IsPrimary\n" +
            $"{DocumentTypeId};{documentNumber};PT;2020-01-01;2030-01-01;false\n";
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "documents.csv");
        return content;
    }

    public async Task<int> CreateDocumentAsync(HttpClient client, int clientId, string documentNumber, bool isPrimary = false)
    {
        var response = await client.PostAsJsonAsync($"/v1/clients/{clientId}/documents", DocumentRequest(documentNumber, isPrimary));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"HTTP {(int)response.StatusCode}: {body}");
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    public object AddressRequest(string? street, bool isPrimary = false) => new
    {
        addressTypeId = AddressTypeId,
        countryCode = "PT",
        street,
        streetNumber = "10",
        complement = "A",
        neighborhood = "Centro",
        city = "Lisboa",
        district = "Lisboa",
        postalCode = "1000-001",
        note = "fixture",
        isPrimary
    };

    public MultipartFormDataContent AddressBulkContent(string street)
    {
        var csv = "AddressTypeId;CountryCode;Street;StreetNumber;Complement;Neighborhood;City;District;PostalCode;Latitude;Longitude;Note;IsPrimary\n" +
            $"{AddressTypeId};PT;{street};11;;Centro;Lisboa;Lisboa;1000-002;;;fixture;false\n";
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "addresses.csv");
        return content;
    }

    private static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GeritDbContext>();
        var partyType = new PartyTypeEntity("INDIVIDUAL", 1); typeof(PartyTypeEntity).GetProperties().Single(p => p.Name == nameof(Entity.Id) && p.PropertyType == typeof(byte)).SetValue(partyType, (byte)1);
        var acquisition = new AcquisitionSourceTypeEntity("DIRECT", 1); SetId(acquisition, 1);
        var domain = new StatusDomainEntity("CLIENT", 1); SetId(domain, 1);
        var definition = new StatusDefinitionEntity(1, 1, "ACTIVE", 1, true, 1); SetId(definition, 1);
        var addressType = new AddressTypeEntity("GENERAL", 1); SetId(addressType, 1);
        var documentType = new DocumentTypeEntity("IDENTITY", 1); SetId(documentType, 1);
        var tenant = new TenantEntity(1, 1, "Tenant A", "tenant@example.test", null, null, null, 1); SetId(tenant, 1);
        db.AddRange(partyType, acquisition, domain, definition, addressType, documentType, tenant);
        await db.SaveChangesAsync();
    }

    private static void SetId(Entity entity, int id) => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    public async ValueTask DisposeAsync()
    {
        await app.StopAsync();
        await app.DisposeAsync();
    }
}
