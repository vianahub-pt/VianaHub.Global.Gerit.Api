/*
    Idempotent EmployeeDocuments schema migration.
    This script is intentionally not executed by application startup.
*/
SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.EmployeeDocuments', N'U') IS NULL
BEGIN
    IF OBJECT_ID(N'dbo.Tenants', N'U') IS NULL
       OR OBJECT_ID(N'dbo.Employees', N'U') IS NULL
       OR OBJECT_ID(N'dbo.DocumentTypes', N'U') IS NULL
    BEGIN
        THROW 51000, 'EmployeeDocuments dependencies are missing.', 1;
    END;

    CREATE TABLE dbo.EmployeeDocuments
    (
        Id                 INT IDENTITY(1,1) NOT NULL,
        TenantId           INT NOT NULL,
        EmployeeId         INT NOT NULL,
        DocumentTypeId     INT NOT NULL,
        DocumentNumber     NVARCHAR(100) NOT NULL,
        IssuingCountryCode CHAR(2) NOT NULL,
        IssuedAt           DATE NULL,
        ExpiresAt          DATE NULL,
        IsPrimary          BIT NOT NULL CONSTRAINT DF_EmployeeDocuments_IsPrimary DEFAULT 0,
        IsActive           BIT NOT NULL CONSTRAINT DF_EmployeeDocuments_IsActive DEFAULT 1,
        IsDeleted          BIT NOT NULL CONSTRAINT DF_EmployeeDocuments_IsDeleted DEFAULT 0,
        CreatedBy          INT NOT NULL,
        CreatedAt          DATETIME2(7) NOT NULL CONSTRAINT DF_EmployeeDocuments_CreatedAt DEFAULT SYSUTCDATETIME(),
        ModifiedBy         INT NULL,
        ModifiedAt         DATETIME2(7) NULL,
        CONSTRAINT PK_EmployeeDocuments PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_EmployeeDocuments_Id_Tenant UNIQUE (Id, TenantId),
        CONSTRAINT CK_EmployeeDocuments_Primary_Active CHECK (IsPrimary = 0 OR IsActive = 1),
        CONSTRAINT CK_EmployeeDocuments_Active_Deleted CHECK (NOT (IsActive = 1 AND IsDeleted = 1)),
        CONSTRAINT CK_EmployeeDocuments_Modification CHECK ((ModifiedAt IS NULL AND ModifiedBy IS NULL) OR (ModifiedAt IS NOT NULL AND ModifiedBy IS NOT NULL)),
        CONSTRAINT CK_EmployeeDocuments_Dates CHECK (ExpiresAt IS NULL OR IssuedAt IS NULL OR ExpiresAt >= IssuedAt),
        CONSTRAINT FK_EmployeeDocuments_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_EmployeeDocuments_Employee FOREIGN KEY (EmployeeId, TenantId) REFERENCES dbo.Employees(Id, TenantId),
        CONSTRAINT FK_EmployeeDocuments_DocumentType FOREIGN KEY (DocumentTypeId) REFERENCES dbo.DocumentTypes(Id)
    );
END;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.EmployeeDocuments') AND name = N'IssuingCountryCode' AND (is_nullable = 1 OR max_length <> 2))
    THROW 51001, 'EmployeeDocuments.IssuingCountryCode has an incompatible definition.', 1;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.EmployeeDocuments') AND name = N'DocumentNumber' AND (is_nullable = 1 OR max_length <> 200))
    THROW 51002, 'EmployeeDocuments.DocumentNumber has an incompatible definition.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_EmployeeDocuments_Tenant' AND parent_object_id = OBJECT_ID(N'dbo.EmployeeDocuments'))
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id WHERE fk.parent_object_id = OBJECT_ID(N'dbo.EmployeeDocuments') AND fk.name <> N'FK_EmployeeDocuments_Tenant' AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = N'TenantId' AND OBJECT_NAME(fk.referenced_object_id) = N'Tenants')
        THROW 51003, 'EmployeeDocuments has an incompatible Tenant foreign key.', 1;
    ALTER TABLE dbo.EmployeeDocuments ADD CONSTRAINT FK_EmployeeDocuments_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id);
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_EmployeeDocuments_Employee' AND parent_object_id = OBJECT_ID(N'dbo.EmployeeDocuments'))
    ALTER TABLE dbo.EmployeeDocuments ADD CONSTRAINT FK_EmployeeDocuments_Employee FOREIGN KEY (EmployeeId, TenantId) REFERENCES dbo.Employees(Id, TenantId);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_EmployeeDocuments_DocumentType' AND parent_object_id = OBJECT_ID(N'dbo.EmployeeDocuments'))
    ALTER TABLE dbo.EmployeeDocuments ADD CONSTRAINT FK_EmployeeDocuments_DocumentType FOREIGN KEY (DocumentTypeId) REFERENCES dbo.DocumentTypes(Id);

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'PK_EmployeeDocuments' AND parent_object_id = OBJECT_ID(N'dbo.EmployeeDocuments'))
    ALTER TABLE dbo.EmployeeDocuments ADD CONSTRAINT PK_EmployeeDocuments PRIMARY KEY CLUSTERED (Id);

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_EmployeeDocuments_Id_Tenant' AND parent_object_id = OBJECT_ID(N'dbo.EmployeeDocuments'))
    ALTER TABLE dbo.EmployeeDocuments ADD CONSTRAINT UQ_EmployeeDocuments_Id_Tenant UNIQUE (Id, TenantId);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_EmployeeDocuments_Primary_Active' AND parent_object_id = OBJECT_ID(N'dbo.EmployeeDocuments'))
    ALTER TABLE dbo.EmployeeDocuments ADD CONSTRAINT CK_EmployeeDocuments_Primary_Active CHECK (IsPrimary = 0 OR IsActive = 1);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_EmployeeDocuments_Active_Deleted' AND parent_object_id = OBJECT_ID(N'dbo.EmployeeDocuments'))
    ALTER TABLE dbo.EmployeeDocuments ADD CONSTRAINT CK_EmployeeDocuments_Active_Deleted CHECK (NOT (IsActive = 1 AND IsDeleted = 1));

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_EmployeeDocuments_Modification' AND parent_object_id = OBJECT_ID(N'dbo.EmployeeDocuments'))
    ALTER TABLE dbo.EmployeeDocuments ADD CONSTRAINT CK_EmployeeDocuments_Modification CHECK ((ModifiedAt IS NULL AND ModifiedBy IS NULL) OR (ModifiedAt IS NOT NULL AND ModifiedBy IS NOT NULL));

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_EmployeeDocuments_Dates' AND parent_object_id = OBJECT_ID(N'dbo.EmployeeDocuments'))
    ALTER TABLE dbo.EmployeeDocuments ADD CONSTRAINT CK_EmployeeDocuments_Dates CHECK (ExpiresAt IS NULL OR IssuedAt IS NULL OR ExpiresAt >= IssuedAt);

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.EmployeeDocuments') AND name = N'IX_EmployeeDocuments_Client')
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.EmployeeDocuments') AND name = N'IX_EmployeeDocuments_Employee')
    EXEC sys.sp_rename N'dbo.EmployeeDocuments.IX_EmployeeDocuments_Client', N'IX_EmployeeDocuments_Employee', N'INDEX';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.EmployeeDocuments') AND name = N'UX_EmployeeDocuments_Type_Country_Number')
    CREATE UNIQUE INDEX UX_EmployeeDocuments_Type_Country_Number ON dbo.EmployeeDocuments (TenantId, DocumentTypeId, IssuingCountryCode, DocumentNumber) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.EmployeeDocuments') AND name = N'UX_EmployeeDocuments_Primary')
    CREATE UNIQUE INDEX UX_EmployeeDocuments_Primary ON dbo.EmployeeDocuments (TenantId, EmployeeId) WHERE IsPrimary = 1 AND IsActive = 1 AND IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.EmployeeDocuments') AND name = N'IX_EmployeeDocuments_Employee')
    CREATE NONCLUSTERED INDEX IX_EmployeeDocuments_Employee ON dbo.EmployeeDocuments (TenantId, EmployeeId) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantSecurityPolicy' AND schema_id = SCHEMA_ID(N'dbo'))
    THROW 51004, 'TenantSecurityPolicy is required before EmployeeDocuments RLS can be extended.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.security_predicates spr JOIN sys.security_policies sp ON sp.object_id = spr.object_id WHERE sp.name = N'TenantSecurityPolicy' AND spr.target_object_id = OBJECT_ID(N'dbo.EmployeeDocuments') AND spr.predicate_type = 0)
    ALTER SECURITY POLICY dbo.TenantSecurityPolicy ADD FILTER PREDICATE dbo.fn_TenantAccessPredicate(TenantId) ON dbo.EmployeeDocuments;

IF NOT EXISTS (SELECT 1 FROM sys.security_predicates spr JOIN sys.security_policies sp ON sp.object_id = spr.object_id WHERE sp.name = N'TenantSecurityPolicy' AND spr.target_object_id = OBJECT_ID(N'dbo.EmployeeDocuments') AND spr.predicate_type = 1 AND spr.operation = 1)
    ALTER SECURITY POLICY dbo.TenantSecurityPolicy ADD BLOCK PREDICATE dbo.fn_TenantAccessPredicate(TenantId) ON dbo.EmployeeDocuments AFTER INSERT;

IF NOT EXISTS (SELECT 1 FROM sys.security_predicates spr JOIN sys.security_policies sp ON sp.object_id = spr.object_id WHERE sp.name = N'TenantSecurityPolicy' AND spr.target_object_id = OBJECT_ID(N'dbo.EmployeeDocuments') AND spr.predicate_type = 1 AND spr.operation = 2)
    ALTER SECURITY POLICY dbo.TenantSecurityPolicy ADD BLOCK PREDICATE dbo.fn_TenantAccessPredicate(TenantId) ON dbo.EmployeeDocuments AFTER UPDATE;

IF NOT EXISTS (SELECT 1 FROM sys.security_predicates spr JOIN sys.security_policies sp ON sp.object_id = spr.object_id WHERE sp.name = N'TenantSecurityPolicy' AND spr.target_object_id = OBJECT_ID(N'dbo.EmployeeDocuments') AND spr.predicate_type = 1 AND spr.operation = 4)
    ALTER SECURITY POLICY dbo.TenantSecurityPolicy ADD BLOCK PREDICATE dbo.fn_TenantAccessPredicate(TenantId) ON dbo.EmployeeDocuments BEFORE DELETE;

COMMIT TRANSACTION;
