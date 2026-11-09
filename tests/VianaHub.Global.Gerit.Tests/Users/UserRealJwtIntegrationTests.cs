using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Tools.Cryptography;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Tests.Infrastructure;

namespace VianaHub.Global.Gerit.Tests.Users;

[Collection("Roles SQL HTTP")]
public sealed class UserRealJwtIntegrationTests
{
    [SqlServerIntegrationTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RealJwt_EnforcesPasswordOwnership(bool verifyAdmin)
    {
        var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"))
        {
            Pooling = false
        };
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "EXEC sys.sp_set_session_context @key=N'IsSuperAdmin', @value=1";
        await command.ExecuteNonQueryAsync();
        await using var seedTransaction = (SqlTransaction)await connection.BeginTransactionAsync();
        command.Transaction = seedTransaction;
        var suffix = Guid.NewGuid().ToString("N");
        var tenantName = $"TEST_TENANT_USERS_JWT_{suffix}";
        var email = $"jwt-{suffix}@example.invalid";
        var initial = $"Aa1!{Guid.NewGuid():N}";
        var replacement = $"Bb2!{Guid.NewGuid():N}";
        command.Parameters.AddWithValue("@name", tenantName);
        command.Parameters.AddWithValue("@email", email);
        command.CommandText = """
            INSERT dbo.Tenants (PartyTypeId, AcquisitionSourceTypeId, Name, Email, CreatedBy)
            OUTPUT INSERTED.Id
            SELECT p.Id, a.Id, @name, @email, 1
            FROM dbo.PartyTypes p CROSS JOIN dbo.AcquisitionSourceTypes a
            WHERE p.Code=N'ORGANIZATION' AND p.IsActive=1 AND p.IsDeleted=0
              AND a.Code=N'OTHER' AND a.IsActive=1 AND a.IsDeleted=0;
            """;
        var tenant = Assert.IsType<int>(await command.ExecuteScalarAsync());
        command.Parameters.AddWithValue("@tenant", tenant);
        try
        {
            command.CommandText = """
                INSERT dbo.StatusDefinitions (TenantId, StatusDomainId, Code, CreatedBy)
                SELECT @tenant, Id, N'ACTIVE', 1 FROM dbo.StatusDomains
                WHERE Code=N'SUBSCRIPTION' AND IsActive=1 AND IsDeleted=0;
                INSERT dbo.Subscriptions (TenantId, StatusDefinitionId, StatusDomainId, SubscriptionPlanId,
                    AgreedAmount, BillingInterval, CurrencyCode, CurrentPeriodStart, CurrentPeriodEnd, CreatedBy)
                SELECT @tenant, s.Id, s.StatusDomainId, p.Id, 0, N'month', p.Currency,
                    DATEADD(day,-1,SYSUTCDATETIME()), DATEADD(day,1,SYSUTCDATETIME()), 1
                FROM dbo.StatusDefinitions s CROSS JOIN dbo.SubscriptionPlans p
                WHERE s.TenantId=@tenant AND s.Code=N'ACTIVE'
                  AND p.Code=N'FREE' AND p.IsActive=1 AND p.IsDeleted=0;
                """;
            Assert.Equal(2, await command.ExecuteNonQueryAsync());

            using var factory = new RealJwtFactory();
            using var client = factory.CreateClient();
            using var scope = factory.Services.CreateScope();
            var master = scope.ServiceProvider.GetRequiredService<ISecretProvider>().GetMasterKey();
            Assert.True(!string.IsNullOrEmpty(master));
            var pair = CryptoRSA.GenerateKeyPair();
            var key = new JwtKeysEntity(tenant, pair.PublicKeyPem,
                CryptoRSA.EncryptPrivateKey(pair.PrivateKeyPem, master!), 1);
            Assert.True(key.IsValidForSigning());
            var options = new DbContextOptionsBuilder<GeritDbContext>().UseSqlServer(connection).Options;
            await using var context = new GeritDbContext(options);
            await context.Database.UseTransactionAsync(seedTransaction);
            var user = new UserEntity(tenant, "JWT owner", email, DomainExtensions.HashClientSecret(initial), null, 1);
            var other = new UserEntity(tenant, "JWT other", $"other-{suffix}@example.invalid",
                DomainExtensions.HashClientSecret(initial), null, 1);
            context.AddRange(key, user, other);
            await context.SaveChangesAsync();
            await seedTransaction.CommitAsync();
            command.Transaction = null;
            command.CommandText = "SELECT COUNT(*) FROM dbo.UserRoles WHERE TenantId=@tenant";
            Assert.Equal(0, Assert.IsType<int>(await command.ExecuteScalarAsync()));

            var token = await LoginAsync(client, email, initial);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            Assert.Equal(user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), jwt.Subject);
            Assert.Equal(tenant.ToString(System.Globalization.CultureInfo.InvariantCulture),
                jwt.Claims.Single(claim => claim.Type == "tenant_id").Value);
            Assert.DoesNotContain(jwt.Claims, claim => claim.Type is "role" or "roles" or ClaimTypes.Role or "permissions");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var request = new { currentPassword = initial, newPassword = replacement };
            using var crossUser = await client.PatchAsJsonAsync($"/v1/users/{other.Id}/password", request);
            Assert.Equal(HttpStatusCode.Forbidden, crossUser.StatusCode);
            using var own = await client.PatchAsJsonAsync($"/v1/users/{user.Id}/password", request);
            Assert.Equal(HttpStatusCode.OK, own.StatusCode);
            foreach (var response in new[] { crossUser, own })
            {
                var body = await response.Content.ReadAsStringAsync();
                Assert.False(body.Contains(initial, StringComparison.Ordinal) || body.Contains(replacement, StringComparison.Ordinal));
            }
            client.DefaultRequestHeaders.Authorization = null;
            await LoginAsync(client, email, replacement);
            using var stale = await client.PostAsJsonAsync("/v1/auth/login", new { email, password = initial });
            Assert.Equal(HttpStatusCode.Unauthorized, stale.StatusCode);

            if (verifyAdmin)
            {
                command.Parameters.AddWithValue("@adminUser", other.Id);
                command.CommandText = """
                    INSERT dbo.Roles (TenantId, Name, Description, CreatedBy)
                    VALUES (@tenant, N'Admin', N'Isolated Users JWT fixture', 1);
                    DECLARE @role int = CONVERT(int, SCOPE_IDENTITY());
                    INSERT dbo.UserRoles (TenantId, UserId, RoleId) VALUES (@tenant, @adminUser, @role);
                    INSERT dbo.RolePermissions (TenantId, RoleId, ResourceId, ActionId)
                    SELECT @tenant, @role, r.Id, a.Id FROM dbo.Resources r CROSS JOIN dbo.Actions a
                    WHERE r.Name=N'Users' AND r.IsActive=1 AND r.IsDeleted=0
                      AND a.Name IN (N'GetBy', N'Update') AND a.IsActive=1 AND a.IsDeleted=0;
                    SELECT @role;
                    """;
                var assignedRoleId = Assert.IsType<int>(await command.ExecuteScalarAsync());
                command.Parameters.AddWithValue("@assignedRole", assignedRoleId);
                var adminToken = await LoginAsync(client, other.Email!, initial);
                var adminJwt = new JwtSecurityTokenHandler().ReadJwtToken(adminToken);
                Assert.Contains(adminJwt.Claims, claim => claim.Type == "role" && claim.Value == "admin");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
                using var detail = await client.GetAsync($"/v1/users/{user.Id}");
                Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
                using var profile = await client.PutAsJsonAsync($"/v1/users/{user.Id}", new { name = "JWT owner updated" });
                Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
                using var forbidden = await client.PatchAsJsonAsync($"/v1/users/{user.Id}/password",
                    new { currentPassword = replacement, newPassword = initial });
                Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

                client.DefaultRequestHeaders.Authorization = null;
                command.CommandText = "UPDATE dbo.Roles SET IsActive=0 WHERE Id=@assignedRole AND TenantId=@tenant";
                Assert.Equal(1, await command.ExecuteNonQueryAsync());
                await AssertNoGrantsAsync(client, other.Email!, initial);

                command.CommandText = "UPDATE dbo.Roles SET IsActive=1 WHERE Id=@assignedRole AND TenantId=@tenant";
                Assert.Equal(1, await command.ExecuteNonQueryAsync());
                var reactivated = new JwtSecurityTokenHandler().ReadJwtToken(await LoginAsync(client, other.Email!, initial));
                Assert.Contains(reactivated.Claims, claim => claim.Type == "role" && claim.Value == "admin");
                Assert.Contains(reactivated.Claims, claim => claim.Type == "permissions");

                command.CommandText = "UPDATE dbo.Roles SET IsActive=0, IsDeleted=1 WHERE Id=@assignedRole AND TenantId=@tenant";
                Assert.Equal(1, await command.ExecuteNonQueryAsync());
                await AssertNoGrantsAsync(client, other.Email!, initial);

                command.CommandText = """
                    UPDATE dbo.Roles SET IsActive=1, IsDeleted=0 WHERE Id=@assignedRole AND TenantId=@tenant;
                    DELETE FROM dbo.UserRoles WHERE TenantId=@tenant AND UserId=@adminUser AND RoleId=@assignedRole;
                    """;
                Assert.Equal(2, await command.ExecuteNonQueryAsync());
                await AssertNoGrantsAsync(client, other.Email!, initial);
            }
        }
        finally
        {
            if (seedTransaction.Connection is not null)
                await seedTransaction.RollbackAsync();
            command.Transaction = null;
            command.CommandText = """
                DELETE FROM dbo.RefreshTokens WHERE TenantId=@tenant;
                DELETE FROM dbo.RolePermissions WHERE TenantId=@tenant;
                DELETE FROM dbo.UserRoles WHERE TenantId=@tenant;
                DELETE FROM dbo.Roles WHERE TenantId=@tenant;
                DELETE FROM dbo.JwtKeys WHERE TenantId=@tenant;
                DELETE FROM dbo.Subscriptions WHERE TenantId=@tenant;
                DELETE FROM dbo.StatusDefinitions WHERE TenantId=@tenant;
                DELETE FROM dbo.Users WHERE TenantId=@tenant;
                DELETE FROM dbo.Tenants WHERE Id=@tenant AND Name=@name;
                SELECT (SELECT COUNT(*) FROM dbo.Users WHERE TenantId=@tenant),
                       (SELECT COUNT(*) FROM dbo.UserRoles WHERE TenantId=@tenant),
                       (SELECT COUNT(*) FROM dbo.JwtKeys WHERE TenantId=@tenant),
                       (SELECT COUNT(*) FROM dbo.Subscriptions WHERE TenantId=@tenant),
                       (SELECT COUNT(*) FROM dbo.Tenants WHERE Id=@tenant),
                       (SELECT COUNT(*) FROM dbo.RolePermissions WHERE TenantId=@tenant),
                       (SELECT COUNT(*) FROM dbo.Roles WHERE TenantId=@tenant);
                """;
            await using var residual = await command.ExecuteReaderAsync();
            Assert.True(await residual.ReadAsync());
            Assert.Equal(new int[7], Enumerable.Range(0, 7).Select(residual.GetInt32));
        }
    }

    private static async Task AssertNoGrantsAsync(HttpClient client, string email, string password)
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(await LoginAsync(client, email, password));
        Assert.DoesNotContain(token.Claims, claim => claim.Type is "role" or "roles" or ClaimTypes.Role or "permissions");
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync("/v1/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(body.TryGetProperty("accessToken", out var accessToken));
        var token = accessToken.GetString();
        Assert.True(!string.IsNullOrWhiteSpace(token));
        Assert.DoesNotContain(body.EnumerateObject(), property => property.Name.Contains("password", StringComparison.OrdinalIgnoreCase));
        return token!;
    }

    private sealed class RealJwtFactory : GeritSqlWebApplicationFactory
    {
        protected override bool UseSimulatedAuthentication => false;
    }
}
