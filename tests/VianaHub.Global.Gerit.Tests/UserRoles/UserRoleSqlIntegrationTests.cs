using Microsoft.Data.SqlClient;
using VianaHub.Global.Gerit.Tests.Infrastructure;

namespace VianaHub.Global.Gerit.Tests.UserRoles;

public sealed class UserRoleSqlIntegrationTests
{
    [SqlServerIntegrationFact]
    public async Task SchemaEnforcesNaturalKeyCompositeParentsAndRls()
    {
        await using var connection = new SqlConnection(
            Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DECLARE @id int = OBJECT_ID(N'dbo.UserRoles');
            SELECT
              (SELECT COUNT(*) FROM sys.columns WHERE object_id=@id AND system_type_id=TYPE_ID(N'int') AND is_nullable=0),
              (SELECT COUNT(*) FROM sys.columns WHERE object_id=@id AND name=N'Id' AND is_identity=1),
              (SELECT COUNT(*) FROM sys.key_constraints WHERE parent_object_id=@id AND name=N'PK_UserRoles'),
              (SELECT COUNT(*) FROM sys.key_constraints WHERE parent_object_id=@id AND name=N'UQ_UserRoles'),
              (SELECT COUNT(*) FROM sys.foreign_key_columns c JOIN sys.foreign_keys f ON f.object_id=c.constraint_object_id
                WHERE f.parent_object_id=@id AND f.name IN (N'FK_UserRoles_User',N'FK_UserRoles_Role',N'FK_UserRoles_Tenant')
                  AND f.is_disabled=0 AND f.is_not_trusted=0 AND f.delete_referential_action=0),
              (SELECT COUNT(*) FROM sys.security_predicates s JOIN sys.security_policies p ON p.object_id=s.object_id
                WHERE s.target_object_id=@id AND p.is_enabled=1
                  AND s.predicate_definition LIKE N'%fn_TenantAccessPredicate%TenantId%');
            """;
        await using var result = await command.ExecuteReaderAsync();
        Assert.True(await result.ReadAsync());
        Assert.Equal(new[] { 4, 1, 1, 1, 5, 4 }, Enumerable.Range(0, 6).Select(result.GetInt32));
    }
}
