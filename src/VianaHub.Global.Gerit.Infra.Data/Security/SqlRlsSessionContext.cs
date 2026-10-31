using System.Data.Common;

namespace VianaHub.Global.Gerit.Infra.Data.Security;

public static class SqlRlsSessionContext
{
    private const string ResetCommand = "EXEC sp_set_session_context @key=N'IsSuperAdmin', @value=NULL; EXEC sp_set_session_context @key=N'TenantId', @value=NULL;";

    public static async Task ResetAsync(DbConnection connection, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = ResetCommand;
        command.CommandTimeout = 15;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static void Reset(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = ResetCommand;
        command.CommandTimeout = 15;
        command.ExecuteNonQuery();
    }
}
