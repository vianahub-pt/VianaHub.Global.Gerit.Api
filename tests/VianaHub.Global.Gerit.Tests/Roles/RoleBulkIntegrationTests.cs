using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using VianaHub.Global.Gerit.Tests.Infrastructure;

namespace VianaHub.Global.Gerit.Tests.Roles;

[Collection("Roles SQL HTTP")]
public sealed class RoleBulkIntegrationTests : IClassFixture<GeritSqlWebApplicationFactory>
{
    private readonly GeritSqlWebApplicationFactory _factory;

    public RoleBulkIntegrationTests(GeritSqlWebApplicationFactory factory) => _factory = factory;

    [SqlServerIntegrationFact]
    public async Task ValidBatchPersistsRowsAndCleanupRemovesThemFromReads()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "BulkUpload,GetPaged,Delete");
        var prefix = $"TEST_ROLE_BULK_{Guid.NewGuid():N}";
        var csv = $"Name;Description\n{prefix}_A;First\n{prefix}_B;Second\n";

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(csv, Encoding.UTF8), "file", "roles.csv");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/v1/roles/bulk-upload", content)).StatusCode);

        var page = await client.GetStringAsync($"/v1/roles/paged?PageNumber=1&PageSize=10&Search={prefix}");
        Assert.Contains(prefix, page, StringComparison.Ordinal);
        await CleanupBySearchAsync(client, prefix);
        Assert.DoesNotContain(prefix, await client.GetStringAsync($"/v1/roles/paged?PageNumber=1&PageSize=10&Search={prefix}"), StringComparison.Ordinal);
    }

    [SqlServerIntegrationFact]
    public async Task DuplicateAndMalformedBatchesPersistNothing()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "BulkUpload,GetPaged");
        var prefix = $"TEST_ROLE_BULK_{Guid.NewGuid():N}";

        using (var duplicate = new MultipartFormDataContent())
        {
            duplicate.Add(new StringContent($"Name;Description\n{prefix};First\n {prefix} ;Second\n", Encoding.UTF8), "file", "roles.csv");
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync("/v1/roles/bulk-upload", duplicate)).StatusCode);
        }

        using (var malformed = new MultipartFormDataContent())
        {
            malformed.Add(new StringContent($"Name;Description\n{prefix}_M;\"unterminated\n", Encoding.UTF8), "file", "roles.csv");
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/v1/roles/bulk-upload", malformed)).StatusCode);
        }

        Assert.DoesNotContain(prefix, await client.GetStringAsync($"/v1/roles/paged?PageNumber=1&PageSize=10&Search={prefix}"), StringComparison.Ordinal);
    }

    [SqlServerIntegrationFact]
    public async Task MissingDescriptionIsRejected()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "BulkUpload");
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Name;Description\nTEST_ROLE_MISSING_DESCRIPTION;\n", Encoding.UTF8), "file", "roles.csv");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/v1/roles/bulk-upload", content)).StatusCode);
    }

    [SqlServerIntegrationFact]
    public async Task ExistingDuplicateRejectsWholeBatchWithoutPersistingValidRow()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "Create,BulkUpload,GetPaged,Delete");
        var prefix = $"TEST_ROLE_EXISTING_{Guid.NewGuid():N}";
        var existingId = 0;

        try
        {
            using var created = await client.PostAsJsonAsync("/v1/roles/", new { name = $"{prefix}_DUP", description = "Existing" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            existingId = (await created.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["id"];

            using var batch = new MultipartFormDataContent();
            batch.Add(new StringContent($"Name;Description\n{prefix}_DUP;Duplicate\n{prefix}_VALID;Valid\n", Encoding.UTF8), "file", "roles.csv");
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync("/v1/roles/bulk-upload", batch)).StatusCode);

            var page = await client.GetStringAsync($"/v1/roles/paged?PageNumber=1&PageSize=20&Search={prefix}");
            Assert.Contains($"{prefix}_DUP", page, StringComparison.Ordinal);
            Assert.DoesNotContain($"{prefix}_VALID", page, StringComparison.Ordinal);
        }
        finally
        {
            if (existingId > 0)
            {
                using var cleanup = await client.DeleteAsync($"/v1/roles/{existingId}");
            }
        }
    }

    [SqlServerIntegrationFact]
    public async Task ConcurrentBulkConflictRollsBackTheLosingBatch()
    {
        using var clientA = _factory.CreateClient();
        using var clientB = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Add("X-Test-Permissions", "BulkUpload,GetPaged,Delete");
        clientB.DefaultRequestHeaders.Add("X-Test-Permissions", "BulkUpload,GetPaged,Delete");
        var prefix = $"TEST_ROLE_BULK_RACE_{Guid.NewGuid():N}";

        using var batchA = new MultipartFormDataContent();
        batchA.Add(new StringContent($"Name;Description\n{prefix}_SHARED;A\n{prefix}_ONLY_A;A\n", Encoding.UTF8), "file", "roles-a.csv");
        using var batchB = new MultipartFormDataContent();
        batchB.Add(new StringContent($"Name;Description\n{prefix}_SHARED;B\n{prefix}_ONLY_B;B\n", Encoding.UTF8), "file", "roles-b.csv");

        try
        {
            var firstRequest = clientA.PostAsync("/v1/roles/bulk-upload", batchA);
            var secondRequest = clientB.PostAsync("/v1/roles/bulk-upload", batchB);
            using var responseA = await firstRequest;
            using var responseB = await secondRequest;
            var responses = new[] { responseA, responseB };
            Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.OK);
            Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            var page = await clientA.GetStringAsync($"/v1/roles/paged?PageNumber=1&PageSize=20&Search={prefix}");
            Assert.Contains($"{prefix}_SHARED", page, StringComparison.Ordinal);
            var onlyA = page.Contains($"{prefix}_ONLY_A", StringComparison.Ordinal);
            var onlyB = page.Contains($"{prefix}_ONLY_B", StringComparison.Ordinal);
            Assert.NotEqual(onlyA, onlyB);
        }
        finally
        {
            await CleanupBySearchAsync(clientA, prefix);
        }
    }

    private static async Task CleanupBySearchAsync(HttpClient client, string search)
    {
        using var response = await client.GetAsync($"/v1/roles/paged?PageNumber=1&PageSize=20&Search={search}");
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (!payload.RootElement.TryGetProperty("items", out var items))
            return;

        foreach (var item in items.EnumerateArray().Where(item => item.TryGetProperty("id", out _)))
        {
            var id = item.GetProperty("id").GetInt32();
            using var deleted = await client.DeleteAsync($"/v1/roles/{id}");
        }
    }
}
