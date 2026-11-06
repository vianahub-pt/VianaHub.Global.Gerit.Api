namespace EmployeeDocumentsAudit;

public static class AuditQueries
{
    public static readonly IReadOnlyList<string> All =
    [
        "SELECT OBJECT_ID(N'dbo.EmployeeDocuments', N'U') AS ObjectId;",
        "SELECT c.name FROM sys.columns c WHERE c.object_id = OBJECT_ID(N'dbo.EmployeeDocuments') ORDER BY c.column_id;",
        "SELECT fk.name, fk.is_disabled, fk.is_not_trusted FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.EmployeeDocuments');",
        "SELECT i.name FROM sys.indexes i WHERE i.object_id = OBJECT_ID(N'dbo.EmployeeDocuments') AND i.name IS NOT NULL;",
        "SELECT cc.name, cc.is_disabled, cc.is_not_trusted FROM sys.check_constraints cc WHERE cc.parent_object_id = OBJECT_ID(N'dbo.EmployeeDocuments');",
        "SELECT sp.is_enabled, sp.is_schema_bound FROM sys.security_policies sp WHERE sp.name = N'TenantSecurityPolicy' AND sp.schema_id = SCHEMA_ID(N'dbo');",
        "SELECT spr.predicate_type_desc, spr.operation_desc, spr.predicate_definition FROM sys.security_predicates spr JOIN sys.security_policies sp ON sp.object_id = spr.object_id WHERE sp.name = N'TenantSecurityPolicy' AND sp.schema_id = SCHEMA_ID(N'dbo') AND spr.target_object_id = OBJECT_ID(N'dbo.EmployeeDocuments');",
        "SELECT CASE WHEN OBJECT_ID(N'dbo.EmployeeDocuments', N'U') IS NULL THEN CONVERT(bigint, 0) ELSE (SELECT COUNT_BIG(*) FROM dbo.EmployeeDocuments) END;"
    ];

    public static bool ContainsMutationCommand() => All.Any(query =>
        new[] { "INSERT", "UPDATE", "DELETE", "MERGE", "DROP", "ALTER", "CREATE", "TRUNCATE", "EXEC", "EXECUTE" }
            .Any(token => query.Contains(token, StringComparison.OrdinalIgnoreCase)));
}
