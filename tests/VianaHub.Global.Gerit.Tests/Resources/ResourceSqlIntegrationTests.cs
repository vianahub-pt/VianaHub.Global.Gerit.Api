using Microsoft.Data.SqlClient;

namespace VianaHub.Global.Gerit.Tests.Resources;

public sealed class ResourceSqlServerFactAttribute : FactAttribute
{
    public ResourceSqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING")))
            Skip = "SQL Server integration requires GERIT_RLS_TEST_CONNECTIONSTRING.";
    }
}

public sealed class ResourceSqlIntegrationTests
{
    [ResourceSqlServerFact]
    public async Task ResourcesSchemaHasExpectedConstraintsAndNoTenantRls()
    {
        var configured = Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING")!;
        await using var connection = new SqlConnection(configured);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                CASE WHEN EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.Resources')) THEN 1 ELSE 0 END,
                CASE WHEN EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Resources') AND name = 'Name' AND is_nullable = 0 AND max_length = 200) THEN 1 ELSE 0 END,
                CASE WHEN EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Resources') AND name = 'Description' AND is_nullable = 0 AND max_length = 1000) THEN 1 ELSE 0 END,
                CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Resources') AND name = 'UX_Resources_Name' AND is_unique = 1 AND has_filter = 1 AND filter_definition LIKE '%IsDeleted%') THEN 1 ELSE 0 END,
                CASE WHEN EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Resources') AND name = 'CK_Resources_Active_Deleted') THEN 1 ELSE 0 END,
                CASE WHEN EXISTS (SELECT 1 FROM sys.security_predicates WHERE target_object_id = OBJECT_ID('dbo.Resources')) THEN 1 ELSE 0 END,
                COL_LENGTH('dbo.Resources', 'TenantId');
            """;

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal(1, reader.GetInt32(1));
        Assert.Equal(1, reader.GetInt32(2));
        Assert.Equal(1, reader.GetInt32(3));
        Assert.Equal(1, reader.GetInt32(4));
        Assert.Equal(0, reader.GetInt32(5));
        Assert.Equal(DBNull.Value, reader.GetValue(6));
    }
}
