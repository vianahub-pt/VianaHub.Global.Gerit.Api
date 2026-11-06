using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Tests.EmployeeDocuments;

public sealed class EmployeeDocumentSqlServerFactAttribute : FactAttribute
{
    public EmployeeDocumentSqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING")))
            Skip = "SQL Server EmployeeDocuments integration requires GERIT_RLS_TEST_CONNECTIONSTRING.";
    }
}

public sealed class EmployeeDocumentSqlIntegrationTests
{
    [EmployeeDocumentSqlServerFact]
    public async Task SchemaContainsEmployeeDocumentsConstraintsIndexesAndRls()
    {
        await using var db = CreateContext();
        await db.Database.OpenConnectionAsync();
        Assert.Equal(1, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.tables WHERE name = N'EmployeeDocuments' AND schema_id = SCHEMA_ID(N'dbo')"));
        foreach (var name in new[] { "PK_EmployeeDocuments", "UQ_EmployeeDocuments_Id_Tenant", "FK_EmployeeDocuments_Tenant", "FK_EmployeeDocuments_Employee", "FK_EmployeeDocuments_DocumentType", "CK_EmployeeDocuments_Primary_Active", "CK_EmployeeDocuments_Active_Deleted", "CK_EmployeeDocuments_Modification", "CK_EmployeeDocuments_Dates" })
            Assert.Equal(1, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.objects WHERE name = @name", ("@name", name)));
        foreach (var name in new[] { "UX_EmployeeDocuments_Type_Country_Number", "UX_EmployeeDocuments_Primary", "IX_EmployeeDocuments_Employee" })
            Assert.Equal(1, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.indexes WHERE name = @name AND object_id = OBJECT_ID(N'dbo.EmployeeDocuments')", ("@name", name)));
        Assert.Equal(1, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.security_predicates sp JOIN sys.tables t ON t.object_id = sp.target_object_id WHERE t.name = N'EmployeeDocuments' AND sp.predicate_type_desc = N'FILTER'"));
        Assert.Equal(3, await ScalarAsync<int>(db, "SELECT COUNT(*) FROM sys.security_predicates sp JOIN sys.tables t ON t.object_id = sp.target_object_id WHERE t.name = N'EmployeeDocuments' AND sp.predicate_type_desc = N'BLOCK'"));
    }

    [EmployeeDocumentSqlServerFact]
    public async Task TenantRlsHidesEmployeeDocumentsAndDoesNotUseSuperAdmin()
    {
        await using var db = CreateContext();
        await db.Database.OpenConnectionAsync();
        await SetTenantAsync(db, 1);
        var employeeId = await db.Employees.Where(x => !x.IsDeleted).Select(x => x.Id).FirstAsync();
        var document = new EmployeeDocumentsEntity(1, employeeId, 1, $"EMP-RLS-{Guid.NewGuid():N}", "PT", new DateOnly(2020, 1, 1), null, false, 1);
        db.EmployeeDocuments.Add(document);
        await db.SaveChangesAsync();
        Assert.Equal(1, await db.EmployeeDocuments.CountAsync(x => x.Id == document.Id));
        Assert.Null(await ScalarNullableAsync<int>(db, "SELECT TRY_CONVERT(INT, SESSION_CONTEXT(N'IsSuperAdmin'))"));

        await SetTenantAsync(db, 2);
        Assert.False(await db.EmployeeDocuments.AnyAsync(x => x.Id == document.Id));
        var update = await TryExecuteAsync(db, "UPDATE dbo.EmployeeDocuments SET DocumentNumber = DocumentNumber WHERE Id = @id", ("@id", document.Id));
        Assert.Equal(0, update);
        var delete = await TryExecuteAsync(db, "DELETE FROM dbo.EmployeeDocuments WHERE Id = @id", ("@id", document.Id));
        Assert.Equal(0, delete);
        await Assert.ThrowsAnyAsync<SqlException>(() => ExecuteAsync(db, "INSERT dbo.EmployeeDocuments (TenantId, EmployeeId, DocumentTypeId, DocumentNumber, IssuingCountryCode, CreatedBy) VALUES (1, @employee, 1, @number, 'PT', 1)", ("@employee", employeeId), ("@number", $"EMP-BLOCK-{Guid.NewGuid():N}")));
    }

    private static GeritDbContext CreateContext()
    {
        var connectionString = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"))
        {
            Encrypt = true,
            TrustServerCertificate = true,
            ApplicationName = "Gerit.EmployeeDocumentsSqlTest." + Guid.NewGuid().ToString("N"),
            ConnectRetryCount = 0
        }.ConnectionString;
        return new GeritDbContext(new DbContextOptionsBuilder<GeritDbContext>().UseSqlServer(connectionString).Options);
    }

    private static async Task SetTenantAsync(GeritDbContext db, int tenantId) => await ExecuteAsync(db, "EXEC sys.sp_set_session_context @key=N'TenantId', @value=@tenant", ("@tenant", tenantId));

    private static async Task ExecuteAsync(GeritDbContext db, string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters) { var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value; command.Parameters.Add(parameter); }
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<int> TryExecuteAsync(GeritDbContext db, string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters) { var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value; command.Parameters.Add(parameter); }
        try { return await command.ExecuteNonQueryAsync(); } catch (SqlException exception) when (exception.Number is 33504 or 33505 or 33506) { return 0; }
    }

    private static async Task<T> ScalarAsync<T>(GeritDbContext db, string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = sql;
        foreach (var (name, value) in parameters) { var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value; command.Parameters.Add(parameter); }
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<T?> ScalarNullableAsync<T>(GeritDbContext db, string sql) where T : struct
    {
        await using var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = sql;
        var value = await command.ExecuteScalarAsync(); return value is null or DBNull ? null : (T)value;
    }
}
