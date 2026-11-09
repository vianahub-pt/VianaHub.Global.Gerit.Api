using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.UserRole;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Exceptions;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Repository.Identity;
using VianaHub.Global.Gerit.Tests.Infrastructure;

namespace VianaHub.Global.Gerit.Tests.UserRoles;

[Collection("Roles SQL HTTP")]
public sealed class UserRoleHttpIntegrationTests
{
    [SqlServerIntegrationFact]
    public async Task AssociationLifecycle_ReturnsCanonicalIdentifiersAndPhysicallyDeletes()
    {
        var tenants = new SqlTestTenantFixture();
        var (tenantId, _) = await tenants.CreateAsync();
        await using var db = new SqlConnection(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"));
        try
        {
            await db.OpenAsync();
            await using var command = db.CreateCommand();
            command.Parameters.AddWithValue("@tenant", tenantId);
            command.Parameters.AddWithValue("@email", $"user-role-{Guid.NewGuid():N}@example.invalid");
            command.CommandText = """
                EXEC sys.sp_set_session_context @key=N'TenantId', @value=@tenant;
                INSERT dbo.Users (TenantId, Name, Email, NormalizedEmail, PasswordHash, CreatedBy)
                OUTPUT INSERTED.Id VALUES (@tenant, N'TEST_USER_ROLE', @email, UPPER(@email), N'fixture-only', 1);
                """;
            var userId = Assert.IsType<int>(await command.ExecuteScalarAsync());
            command.CommandText = """
                INSERT dbo.Roles (TenantId, Name, Description, CreatedBy)
                OUTPUT INSERTED.Id VALUES (@tenant, N'TEST_ROLE_USER_ROLE', N'Test only', 1);
                """;
            var roleId = Assert.IsType<int>(await command.ExecuteScalarAsync());
            var gate = new AssociationInsertGate(tenantId);
            using var factory = new GeritSqlWebApplicationFactory();
            using var app = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                var original = services.Last(service => service.ServiceType == typeof(DbContextOptions<GeritDbContext>));
                services.AddScoped(provider =>
                {
                    var options = (DbContextOptions<GeritDbContext>)original.ImplementationFactory!(provider);
                    return new DbContextOptionsBuilder<GeritDbContext>(options).AddInterceptors(gate).Options;
                });
            }));
            using var client = app.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Tenant", tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            client.DefaultRequestHeaders.Add("X-Test-Permission-Resource", "UserRoles");
            client.DefaultRequestHeaders.Add("X-Test-Permissions", "Create,GetAll,GetBy,Delete,BulkUpload");
            const string root = "/v1/user-roles";
            var path = $"{root}/users/{userId}/roles/{roleId}";
            using var missingUser = await client.PostAsJsonAsync(root + "/", new { userId = int.MaxValue, roleId });
            Assert.Equal(HttpStatusCode.NotFound, missingUser.StatusCode);
            using var missingRole = await client.PostAsJsonAsync(root + "/", new { userId, roleId = int.MaxValue });
            Assert.Equal(HttpStatusCode.NotFound, missingRole.StatusCode);
            var createRequest = new { userId, roleId, tenantId = int.MaxValue };
            var results = await Task.WhenAll(
                client.PostAsJsonAsync(root + "/", createRequest),
                client.PostAsJsonAsync(root + "/", createRequest));
            using var first = results[0];
            using var second = results[1];
            Assert.Equal(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict },
                results.Select(result => result.StatusCode).Order());
            Assert.Equal(2, gate.Arrivals);
            Assert.Equal(1, gate.SqlConflicts);
            var create = results.Single(result => result.StatusCode == HttpStatusCode.Created);
            var conflict = results.Single(result => result.StatusCode == HttpStatusCode.Conflict);
            var conflictBody = await conflict.Content.ReadAsStringAsync();
            Assert.DoesNotContain("UQ_UserRoles", conflictBody, StringComparison.Ordinal);
            Assert.DoesNotContain("SqlException", conflictBody, StringComparison.Ordinal);
            var association = await create.Content.ReadFromJsonAsync<UserRoleResponse>();
            Assert.NotNull(association);
            Assert.True(association.Id > 0);
            Assert.Equal(userId, association.UserId);
            Assert.Equal(roleId, association.RoleId);
            Assert.Equal("TEST_USER_ROLE", association.UserName);
            Assert.Equal("TEST_ROLE_USER_ROLE", association.RoleName);
            foreach (var (locale, expected) in new[]
            {
                ("pt-PT", "Já existe esta função para este utilizador"),
                ("pt-BR", "Já existe esta função para este usuário"),
                ("en-US", "This role already exists for this user"),
                ("es-ES", "Este rol ya existe para este usuario")
            })
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, root + "/")
                {
                    Content = JsonContent.Create(new { userId, roleId })
                };
                request.Headers.Add("Accept-Language", locale);
                using var duplicate = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
                var error = await duplicate.Content.ReadFromJsonAsync<VianaHub.Global.Gerit.Application.Dtos.Base.ErrorResponse>();
                Assert.NotNull(error);
                Assert.Contains(expected, error.Errors.Values.SelectMany(messages => messages));

                client.DefaultRequestHeaders.Add("Accept-Language", locale);
                try
                {
                    using var invalid = await client.PostAsJsonAsync(root + "/", new { userId = 0, roleId });
                    await AssertLocalizedErrorAsync(invalid, HttpStatusCode.BadRequest);
                    using var absentUser = await client.PostAsJsonAsync(root + "/", new { userId = int.MaxValue, roleId });
                    await AssertLocalizedErrorAsync(absentUser, HttpStatusCode.NotFound);
                    using var absentRole = await client.PostAsJsonAsync(root + "/", new { userId, roleId = int.MaxValue });
                    await AssertLocalizedErrorAsync(absentRole, HttpStatusCode.NotFound);
                    using var absentAssociation = await client.GetAsync($"{root}/users/{int.MaxValue}/roles/{roleId}");
                    await AssertLocalizedErrorAsync(absentAssociation, HttpStatusCode.Gone);
                    using var invalidCsv = await UploadAsync(client, "RoleId;UserId\n1;2\n");
                    await AssertLocalizedErrorAsync(invalidCsv, HttpStatusCode.BadRequest);
                    using var unauthorizedRequest = new HttpRequestMessage(HttpMethod.Get, path);
                    unauthorizedRequest.Headers.Add("X-Test-Role", "User");
                    using var unauthorized = await client.SendAsync(unauthorizedRequest);
                    await AssertLocalizedErrorAsync(unauthorized, HttpStatusCode.Forbidden);
                }
                finally
                {
                    client.DefaultRequestHeaders.Remove("Accept-Language");
                }
            }
            var detail = await client.GetFromJsonAsync<UserRoleDetailResponse>(path);
            Assert.Equal(association.Id, detail?.Id);
            foreach (var listPath in new[] { root + "/", $"{root}/user/{userId}", $"{root}/role/{roleId}" })
            {
                var list = await client.GetFromJsonAsync<UserRoleResponse[]>(listPath);
                Assert.Equal(association.Id, Assert.Single(list!).Id);
            }
            var (otherTenant, _) = await tenants.CreateAsync();
            try
            {
                using var otherClient = app.CreateClient();
                otherClient.DefaultRequestHeaders.Add("X-Test-Tenant", otherTenant.ToString(System.Globalization.CultureInfo.InvariantCulture));
                otherClient.DefaultRequestHeaders.Add("X-Test-Permission-Resource", "UserRoles");
                otherClient.DefaultRequestHeaders.Add("X-Test-Permissions", "Create,GetAll,GetBy,Delete,BulkUpload");
                foreach (var listPath in new[] { root + "/", $"{root}/user/{userId}", $"{root}/role/{roleId}" })
                    Assert.Empty((await otherClient.GetFromJsonAsync<UserRoleResponse[]>(listPath))!);
                using var hidden = await otherClient.GetAsync(path);
                Assert.Equal(HttpStatusCode.Gone, hidden.StatusCode);
                using var crossDelete = await otherClient.DeleteAsync(path);
                Assert.Equal(HttpStatusCode.Gone, crossDelete.StatusCode);
                using var crossCreate = await otherClient.PostAsJsonAsync(root + "/", createRequest);
                Assert.Equal(HttpStatusCode.NotFound, crossCreate.StatusCode);
                using var crossBulk = await UploadAsync(otherClient, $"UserId;RoleId\n{userId};{roleId}\n");
                Assert.Equal(HttpStatusCode.NotFound, crossBulk.StatusCode);
                await AssertCrossTenantForeignKeysAsync(db, tenantId, otherTenant, userId, roleId);
            }
            finally
            {
                await tenants.DeleteAsync(otherTenant);
            }
            client.DefaultRequestHeaders.Add("X-Test-Role", "User");
            using var denied = await client.GetAsync($"{root}/role/{roleId}");
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            client.DefaultRequestHeaders.Remove("X-Test-Role");
            foreach (var (method, route, permission) in new[]
            {
                (HttpMethod.Get, root + "/", "GetAll"),
                (HttpMethod.Get, path, "GetBy"),
                (HttpMethod.Get, $"{root}/user/{userId}", "GetBy"),
                (HttpMethod.Get, $"{root}/role/{roleId}", "GetBy"),
                (HttpMethod.Post, root + "/", "Create"),
                (HttpMethod.Delete, path, "Delete"),
                (HttpMethod.Post, root + "/bulk-upload", "BulkUpload")
            })
            {
                using var deniedClient = app.CreateClient();
                deniedClient.DefaultRequestHeaders.Add("X-Test-Tenant", tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture));
                deniedClient.DefaultRequestHeaders.Add("X-Test-Permission-Resource", "UserRoles");
                foreach (var (role, grant, expected) in new[]
                {
                    ("User", permission, HttpStatusCode.Forbidden),
                    ("BackOffice", "Unrelated", HttpStatusCode.Forbidden),
                    ("anonymous", permission, HttpStatusCode.Unauthorized)
                })
                {
                    using var request = new HttpRequestMessage(method, route);
                    request.Headers.Add("X-Test-Role", role);
                    request.Headers.Add("X-Test-Permissions", grant);
                    if (role == "anonymous")
                        request.Headers.Add("X-Test-Anonymous", "1");
                    if (permission == "Create")
                        request.Content = JsonContent.Create(createRequest);
                    using var result = await deniedClient.SendAsync(request);
                    Assert.Equal(expected, result.StatusCode);
                }
            }
            using var deleted = await client.DeleteAsync(path);
            Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
            using var repeatedDelete = await client.DeleteAsync(path);
            Assert.Equal(HttpStatusCode.Gone, repeatedDelete.StatusCode);
            using var gone = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Gone, gone.StatusCode);
            command.CommandText = "SELECT COUNT(*) FROM dbo.UserRoles WHERE TenantId=@tenant";
            Assert.Equal(0, Assert.IsType<int>(await command.ExecuteScalarAsync()));

            var validRow = $"{userId};{roleId}\n";
            using var invalidBulk = await UploadAsync(client, "UserId;RoleId\n" + validRow + "invalid;row\n");
            Assert.Equal(HttpStatusCode.BadRequest, invalidBulk.StatusCode);
            using var invalidHeader = await UploadAsync(client, "RoleId;UserId\n" + validRow);
            Assert.Equal(HttpStatusCode.BadRequest, invalidHeader.StatusCode);
            using var missingBulkUser = await UploadAsync(client, $"UserId;RoleId\n{int.MaxValue};{roleId}\n");
            Assert.Equal(HttpStatusCode.NotFound, missingBulkUser.StatusCode);
            using var missingBulkRole = await UploadAsync(client, $"UserId;RoleId\n{userId};{int.MaxValue}\n");
            Assert.Equal(HttpStatusCode.NotFound, missingBulkRole.StatusCode);
            using var duplicateBulk = await UploadAsync(client, "UserId;RoleId\n" + validRow + validRow);
            Assert.Equal(HttpStatusCode.Conflict, duplicateBulk.StatusCode);
            Assert.Equal(0, Assert.IsType<int>(await command.ExecuteScalarAsync()));
            gate.Reset();
            var bulkResults = await Task.WhenAll(
                UploadAsync(client, "UserId;RoleId\n" + validRow),
                UploadAsync(client, "UserId;RoleId\n" + validRow));
            using var firstBulk = bulkResults[0];
            using var secondBulk = bulkResults[1];
            Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.Conflict },
                bulkResults.Select(result => result.StatusCode).Order());
            Assert.Equal(2, gate.Arrivals);
            Assert.Equal(1, gate.SqlConflicts);
            using var existingBulk = await UploadAsync(client, "UserId;RoleId\n" + validRow);
            Assert.Equal(HttpStatusCode.Conflict, existingBulk.StatusCode);
            Assert.Equal(1, Assert.IsType<int>(await command.ExecuteScalarAsync()));

            command.CommandText = """
                INSERT dbo.Roles (TenantId, Name, Description, CreatedBy)
                OUTPUT INSERTED.Id VALUES (@tenant, N'TEST_ROLE_USER_ROLE_ROLLBACK', N'Test only', 1);
                """;
            var secondRoleId = Assert.IsType<int>(await command.ExecuteScalarAsync());
            await using var context = new GeritDbContext(
                new DbContextOptionsBuilder<GeritDbContext>().UseSqlServer(db).Options);
            var repository = new UserRoleDataRepository(context);
            await Assert.ThrowsAsync<UserRoleConflictException>(() => repository.CreateManyAsync(
                [new(tenantId, userId, secondRoleId), new(tenantId, userId, roleId)], CancellationToken.None));
            Assert.Empty(context.ChangeTracker.Entries<UserRolesEntity>());
            command.CommandText = "SELECT COUNT(*) FROM dbo.UserRoles WHERE TenantId=@tenant";
            Assert.Equal(1, Assert.IsType<int>(await command.ExecuteScalarAsync()));
        }
        finally
        {
            if (db.State == System.Data.ConnectionState.Open)
            {
                await using var cleanup = db.CreateCommand();
                cleanup.Parameters.AddWithValue("@tenant", tenantId);
                cleanup.CommandText = """
                    EXEC sys.sp_set_session_context @key=N'TenantId', @value=@tenant;
                    DELETE FROM dbo.UserRoles WHERE TenantId=@tenant;
                    DELETE FROM dbo.Users WHERE TenantId=@tenant;
                    """;
                await cleanup.ExecuteNonQueryAsync();
            }
            await tenants.DeleteAsync(tenantId);
        }
    }

    private static async Task AssertLocalizedErrorAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<VianaHub.Global.Gerit.Application.Dtos.Base.ErrorResponse>();
        Assert.NotNull(error);
        var messages = error.Errors.Values.SelectMany(values => values).Append(error.Title ?? string.Empty).ToArray();
        Assert.Contains(messages, message => !string.IsNullOrWhiteSpace(message));
        Assert.DoesNotContain(messages, message =>
            System.Text.RegularExpressions.Regex.IsMatch(message, @"^(Api|Application|Domain)\.[A-Za-z0-9_.]+$"));
    }

    private static async Task AssertCrossTenantForeignKeysAsync(
        SqlConnection db, int tenantId, int otherTenant, int userId, int roleId)
    {
        await using var transaction = (SqlTransaction)await db.BeginTransactionAsync();
        await using var command = db.CreateCommand();
        command.Transaction = transaction;
        command.Parameters.AddWithValue("@tenant", tenantId);
        command.Parameters.AddWithValue("@other", otherTenant);
        command.Parameters.AddWithValue("@user", userId);
        command.Parameters.AddWithValue("@role", roleId);
        command.Parameters.AddWithValue("@email", $"fk-{Guid.NewGuid():N}@example.invalid");
        try
        {
            command.CommandText = """
                EXEC sys.sp_set_session_context @key=N'TenantId', @value=@other;
                INSERT dbo.Users (TenantId, Name, Email, NormalizedEmail, PasswordHash, CreatedBy)
                OUTPUT INSERTED.Id VALUES (@other, N'TEST_FK_USER', @email, UPPER(@email), N'fixture-only', 1);
                """;
            command.Parameters.AddWithValue("@otherUser", Assert.IsType<int>(await command.ExecuteScalarAsync()));
            command.CommandText = """
                INSERT dbo.Roles (TenantId, Name, Description, CreatedBy)
                OUTPUT INSERTED.Id VALUES (@other, N'TEST_ROLE_FK', N'Test only', 1);
                """;
            command.Parameters.AddWithValue("@otherRole", Assert.IsType<int>(await command.ExecuteScalarAsync()));
            command.CommandText = "INSERT dbo.UserRoles (TenantId, UserId, RoleId) VALUES (@other, @user, @otherRole)";
            var wrongUser = await Assert.ThrowsAsync<SqlException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal(547, wrongUser.Number);
            Assert.Contains("FK_UserRoles_User", wrongUser.Message, StringComparison.Ordinal);
            command.CommandText = "INSERT dbo.UserRoles (TenantId, UserId, RoleId) VALUES (@other, @otherUser, @role)";
            var wrongRole = await Assert.ThrowsAsync<SqlException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal(547, wrongRole.Number);
            Assert.Contains("FK_UserRoles_Role", wrongRole.Message, StringComparison.Ordinal);
        }
        finally
        {
            await transaction.RollbackAsync();
            command.Transaction = null;
            command.CommandText = "EXEC sys.sp_set_session_context @key=N'TenantId', @value=@tenant";
            await command.ExecuteNonQueryAsync();
        }
    }

    private sealed class AssociationInsertGate(int tenantId) : SaveChangesInterceptor
    {
        private TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;
        private int _sqlConflicts;
        public int Arrivals => _arrivals;
        public int SqlConflicts => _sqlConflicts;

        public void Reset()
        {
            _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _arrivals = 0;
            _sqlConflicts = 0;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context?.ChangeTracker.Entries<UserRolesEntity>()
                .Any(entry => entry.State == EntityState.Added && entry.Entity.TenantId == tenantId) == true)
            {
                if (Interlocked.Increment(ref _arrivals) == 2)
                    _release.TrySetResult();
                await _release.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
            return result;
        }

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            if (eventData.Exception.InnerException is SqlException { Number: 2601 or 2627 })
                Interlocked.Increment(ref _sqlConflicts);
            return Task.CompletedTask;
        }
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string csv)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(csv, System.Text.Encoding.UTF8, "text/csv"), "file", "user-roles.csv");
        return await client.PostAsync("/v1/user-roles/bulk-upload", form);
    }
}
