using EmployeeDocumentsAudit;

namespace VianaHub.Global.Gerit.Tests.EmployeeDocuments;

public sealed class EmployeeDocumentsAuditTests
{
    [Fact]
    public void CompleteMetadataIsClassifiedComplete() => Assert.Equal(AuditClassification.Complete, Complete().Classify());

    [Fact]
    public void MissingTableIsClassifiedMissing() => Assert.Equal(AuditClassification.Missing, (Complete() with { TableExists = false }).Classify());

    [Fact]
    public void MissingForeignKeyIsClassifiedPartial() => Assert.Equal(AuditClassification.Partial, (Complete() with { ForeignKeys = Set() }).Classify());

    [Fact]
    public void LegacyIndexIsClassifiedPartial() => Assert.Equal(AuditClassification.Partial, (Complete() with { Indexes = Set("IX_EmployeeDocuments_Client") }).Classify());

    [Fact]
    public void MissingFilterIsClassifiedPartial() => Assert.Equal(AuditClassification.Partial, (Complete() with { RlsPredicates = Set("BLOCK AFTER INSERT", "BLOCK AFTER UPDATE", "BLOCK BEFORE DELETE") }).Classify());

    [Fact]
    public void MissingBlockIsClassifiedPartial() => Assert.Equal(AuditClassification.Partial, (Complete() with { RlsPredicates = Set("FILTER", "BLOCK AFTER INSERT", "BLOCK AFTER UPDATE") }).Classify());

    [Fact]
    public void AuditQueriesContainNoMutationCommands() => Assert.False(AuditQueries.ContainsMutationCommand());

    private static AuditSnapshot Complete() => new(
        true,
        Set("Id", "TenantId", "EmployeeId", "DocumentTypeId", "DocumentNumber", "IssuingCountryCode", "IssuedAt", "ExpiresAt", "IsPrimary", "IsActive", "IsDeleted", "CreatedBy", "CreatedAt", "ModifiedBy", "ModifiedAt"),
        Set("FK_EmployeeDocuments_Tenant", "FK_EmployeeDocuments_Employee", "FK_EmployeeDocuments_DocumentType"),
        Set(),
        Set("PK_EmployeeDocuments", "UQ_EmployeeDocuments_Id_Tenant", "UX_EmployeeDocuments_Type_Country_Number", "UX_EmployeeDocuments_Primary", "IX_EmployeeDocuments_Employee"),
        Set("CK_EmployeeDocuments_Primary_Active", "CK_EmployeeDocuments_Active_Deleted", "CK_EmployeeDocuments_Modification", "CK_EmployeeDocuments_Dates"),
        Set(),
        Set("FILTER", "BLOCK AFTER INSERT", "BLOCK AFTER UPDATE", "BLOCK BEFORE DELETE"),
        true,
        true,
        0);

    private static HashSet<string> Set(params string[] values) => new(values, StringComparer.OrdinalIgnoreCase);
}
