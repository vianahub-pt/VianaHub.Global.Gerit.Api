using EmployeeDocumentsAudit;
using Microsoft.Data.SqlClient;

var target = args.Length == 2 && args[0] == "--audit-target" ? args[1] : null;
if (target != "employee-documents")
    return Fail("Only the employee-documents audit target is supported.");

if (AuditQueries.ContainsMutationCommand())
    return Fail("Audit query safety assertion failed.");

var connectionString = Environment.GetEnvironmentVariable("GERIT_DB_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString))
    return Fail("Database connection secret is unavailable for the selected environment.");

try
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    var snapshot = await ReadSnapshotAsync(connection);

    Console.WriteLine("# Database Audit — EmployeeDocuments");
    Console.WriteLine("Connection: AVAILABLE");
    Console.WriteLine($"Server: {MaskServer(connection.DataSource)}");
    Console.WriteLine($"Database: {connection.Database}");
    Console.WriteLine($"EmployeeDocuments: {(snapshot.TableExists ? "EXISTS" : "MISSING")}");
    Console.WriteLine($"Columns: {Status(snapshot.TableExists && snapshot.Columns.Count == 15)}");
    Console.WriteLine($"Tenant FK: {Status(snapshot.ForeignKeys.Contains("FK_EmployeeDocuments_Tenant") && !snapshot.DisabledOrUntrustedForeignKeys.Contains("FK_EmployeeDocuments_Tenant"))}");
    Console.WriteLine($"Employee FK: {Status(snapshot.ForeignKeys.Contains("FK_EmployeeDocuments_Employee") && !snapshot.DisabledOrUntrustedForeignKeys.Contains("FK_EmployeeDocuments_Employee"))}");
    Console.WriteLine($"DocumentType FK: {Status(snapshot.ForeignKeys.Contains("FK_EmployeeDocuments_DocumentType") && !snapshot.DisabledOrUntrustedForeignKeys.Contains("FK_EmployeeDocuments_DocumentType"))}");
    Console.WriteLine($"Employee lookup index: {Status(snapshot.Indexes.Contains("IX_EmployeeDocuments_Employee"))}");
    Console.WriteLine($"Legacy index: {(snapshot.Indexes.Contains("IX_EmployeeDocuments_Client") ? "PRESENT" : "ABSENT")}");
    Console.WriteLine($"Constraints: {Status(snapshot.Constraints.Count == 4 && snapshot.DisabledOrUntrustedConstraints.Count == 0)}");
    Console.WriteLine($"TenantSecurityPolicy: {(snapshot.PolicyEnabled ? "ENABLED" : "DISABLED")}");
    Console.WriteLine($"RLS FILTER: {Status(snapshot.RlsPredicates.Contains("FILTER"))}");
    Console.WriteLine($"RLS BLOCK INSERT: {Status(snapshot.RlsPredicates.Contains("BLOCK AFTER INSERT"))}");
    Console.WriteLine($"RLS BLOCK UPDATE: {Status(snapshot.RlsPredicates.Contains("BLOCK AFTER UPDATE"))}");
    Console.WriteLine($"RLS BLOCK DELETE: {Status(snapshot.RlsPredicates.Contains("BLOCK BEFORE DELETE"))}");
    Console.WriteLine($"Predicates: {snapshot.RlsPredicates.Count}/4");
    Console.WriteLine($"Existing rows: {snapshot.ExistingRows}");
    Console.WriteLine($"Classification: {snapshot.Classify().ToString().ToUpperInvariant()}");
    Console.WriteLine("Database modified: NO");
    return 0;
}
catch (SqlException ex)
{
    return Fail($"Read-only database audit failed: {ex.GetType().Name}.");
}
catch (InvalidOperationException ex)
{
    return Fail($"Read-only database audit failed: {ex.GetType().Name}.");
}
catch (OperationCanceledException ex)
{
    return Fail($"Read-only database audit failed: {ex.GetType().Name}.");
}

static async Task<AuditSnapshot> ReadSnapshotAsync(SqlConnection connection)
{
    await using var command = connection.CreateCommand();
    command.CommandText = string.Join("\n", AuditQueries.All);
    await using var reader = await command.ExecuteReaderAsync();

    await reader.ReadAsync();
    var tableExists = !reader.IsDBNull(0) && reader.GetValue(0) is not null;
    await reader.NextResultAsync();
    var columns = await ReadNamesAsync(reader);
    await reader.NextResultAsync();
    var foreignKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var disabledForeignKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    while (await reader.ReadAsync())
    {
        var name = reader.GetString(0);
        foreignKeys.Add(name);
        if (reader.GetBoolean(1) || reader.GetBoolean(2))
            disabledForeignKeys.Add(name);
    }
    await reader.NextResultAsync();
    var indexes = await ReadNamesAsync(reader);
    await reader.NextResultAsync();
    var constraints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var disabledConstraints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    while (await reader.ReadAsync())
    {
        var name = reader.GetString(0);
        constraints.Add(name);
        if (reader.GetBoolean(1) || reader.GetBoolean(2))
            disabledConstraints.Add(name);
    }
    await reader.NextResultAsync();
    var policyEnabled = false;
    var policySchemaBound = false;
    if (await reader.ReadAsync())
    {
        policyEnabled = reader.GetBoolean(0);
        policySchemaBound = reader.GetBoolean(1);
    }
    await reader.NextResultAsync();
    var rls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    while (await reader.ReadAsync())
    {
        var type = reader.GetString(0);
        var operation = reader.IsDBNull(1) ? null : reader.GetString(1);
        var definition = reader.GetString(2).Replace("[", string.Empty).Replace("]", string.Empty, StringComparison.Ordinal);
        if (definition.Contains("dbo.fn_TenantAccessPredicate(TenantId)", StringComparison.OrdinalIgnoreCase))
            rls.Add(type == "FILTER" ? "FILTER" : $"BLOCK {operation}");
    }
    await reader.NextResultAsync();
    await reader.ReadAsync();
    var rows = reader.GetInt64(0);

    return new AuditSnapshot(tableExists, columns, foreignKeys, disabledForeignKeys, indexes, constraints, disabledConstraints, rls, policyEnabled, policySchemaBound, rows);
}

static async Task<HashSet<string>> ReadNamesAsync(SqlDataReader reader)
{
    var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    while (await reader.ReadAsync())
        values.Add(reader.GetString(0));
    return values;
}

static string Status(bool value) => value ? "PASS" : "MISSING";

static string MaskServer(string value)
{
    var host = value.Split(',', 2)[0].Trim();
    var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
    return parts.Length == 4 ? $"{parts[0]}.{parts[1]}.***.***" : "masked";
}

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}
