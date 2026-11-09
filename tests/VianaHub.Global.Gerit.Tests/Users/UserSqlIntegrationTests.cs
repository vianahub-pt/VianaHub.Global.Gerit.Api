using Microsoft.Data.SqlClient;
using VianaHub.Global.Gerit.Tests.Infrastructure;

namespace VianaHub.Global.Gerit.Tests.Users;

public sealed class UserSqlIntegrationTests
{
    [SqlServerIntegrationFact]
    public async Task UsersSchema_EnforcesNormalizedUniquenessAndTenantPolicy()
    {
        await using var connection = new SqlConnection(
            Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DECLARE @users int = OBJECT_ID(N'dbo.Users');
            SELECT
              (SELECT COUNT(*) FROM sys.indexes WHERE object_id=@users AND is_primary_key=1 AND name=N'PK_Users'),
              (SELECT COUNT(*) FROM sys.indexes WHERE object_id=@users AND is_unique=1 AND has_filter=1
                AND name=N'UX_Users_Tenant_NormalizedEmail' AND filter_definition LIKE N'%IsDeleted%0%'),
              (SELECT COUNT(*) FROM sys.columns WHERE object_id=@users AND name=N'PasswordHash'
                AND system_type_id=TYPE_ID(N'nvarchar') AND max_length=1000 AND is_nullable=0),
              (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id=@users
                AND name=N'FK_Users_Tenant' AND is_disabled=0 AND is_not_trusted=0),
              (SELECT COUNT(*) FROM sys.security_predicates predicate
                JOIN sys.security_policies policy ON policy.object_id=predicate.object_id
                WHERE predicate.target_object_id=@users AND policy.is_enabled=1
                  AND predicate.predicate_definition LIKE N'%fn_TenantAccessPredicate%TenantId%');
            """;
        await using var result = await command.ExecuteReaderAsync();
        Assert.True(await result.ReadAsync());
        Assert.Equal(new[] { 1, 1, 1, 1, 4 }, Enumerable.Range(0, 5).Select(result.GetInt32));
    }
}
