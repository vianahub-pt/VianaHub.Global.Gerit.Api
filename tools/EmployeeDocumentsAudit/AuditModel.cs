namespace EmployeeDocumentsAudit;

public enum AuditClassification
{
    Complete,
    Partial,
    Missing
}

public sealed record AuditSnapshot(
    bool TableExists,
    IReadOnlySet<string> Columns,
    IReadOnlySet<string> ForeignKeys,
    IReadOnlySet<string> DisabledOrUntrustedForeignKeys,
    IReadOnlySet<string> Indexes,
    IReadOnlySet<string> Constraints,
    IReadOnlySet<string> DisabledOrUntrustedConstraints,
    IReadOnlySet<string> RlsPredicates,
    bool PolicyEnabled,
    bool PolicySchemaBound,
    long ExistingRows)
{
    private static readonly string[] ExpectedColumns =
    [
        "Id", "TenantId", "EmployeeId", "DocumentTypeId", "DocumentNumber",
        "IssuingCountryCode", "IssuedAt", "ExpiresAt", "IsPrimary", "IsActive",
        "IsDeleted", "CreatedBy", "CreatedAt", "ModifiedBy", "ModifiedAt"
    ];

    private static readonly string[] ExpectedForeignKeys =
        ["FK_EmployeeDocuments_Tenant", "FK_EmployeeDocuments_Employee", "FK_EmployeeDocuments_DocumentType"];

    private static readonly string[] ExpectedIndexes =
        ["PK_EmployeeDocuments", "UQ_EmployeeDocuments_Id_Tenant", "UX_EmployeeDocuments_Type_Country_Number", "UX_EmployeeDocuments_Primary", "IX_EmployeeDocuments_Employee"];

    private static readonly string[] ExpectedConstraints =
        ["CK_EmployeeDocuments_Primary_Active", "CK_EmployeeDocuments_Active_Deleted", "CK_EmployeeDocuments_Modification", "CK_EmployeeDocuments_Dates"];

    private static readonly string[] ExpectedRls =
        ["FILTER", "BLOCK AFTER INSERT", "BLOCK AFTER UPDATE", "BLOCK BEFORE DELETE"];

    public AuditClassification Classify()
    {
        if (!TableExists)
            return AuditClassification.Missing;

        var complete = ExpectedColumns.All(Columns.Contains)
            && ExpectedForeignKeys.All(ForeignKeys.Contains)
            && DisabledOrUntrustedForeignKeys.Count == 0
            && ExpectedIndexes.All(Indexes.Contains)
            && ExpectedConstraints.All(Constraints.Contains)
            && DisabledOrUntrustedConstraints.Count == 0
            && PolicyEnabled
            && PolicySchemaBound
            && ExpectedRls.All(RlsPredicates.Contains);

        return complete ? AuditClassification.Complete : AuditClassification.Partial;
    }
}
