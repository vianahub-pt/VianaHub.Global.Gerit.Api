using Microsoft.Data.SqlClient;

namespace VianaHub.Global.Gerit.Tests.Actions;

public sealed class ActionSqlServerFactAttribute : FactAttribute
{
    public ActionSqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING")))
            Skip = "SQL Server integration requires GERIT_RLS_TEST_CONNECTIONSTRING.";
    }
}

public sealed class ActionSqlIntegrationTests
{
    [ActionSqlServerFact]
    public async Task ActionsSchemaRetainsFilteredUniqueIndexAndStateCheck()
    {
        var configured = Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING")!;

        await using var connection = new SqlConnection(configured);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Actions') AND name = 'UX_Actions_Name' AND is_unique = 1 AND has_filter = 1 AND filter_definition LIKE '%IsDeleted%') THEN 1 ELSE 0 END,
                CASE WHEN EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Actions') AND name = 'CK_Actions_Active_Deleted') THEN 1 ELSE 0 END;
            """;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal(1, reader.GetInt32(1));
    }
}
