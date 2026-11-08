/*
   Reconciles the Roles tenant/name uniqueness contract with Create-Tables.sql.
   Read-only preflight is performed before any index is removed.
   Safe to rerun after the canonical filtered index has been created.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
        THROW 51030, N'dbo.Roles does not exist; migration aborted.', 1;

    /* Never choose a row automatically if active duplicates already exist. */
    IF EXISTS (
        SELECT 1
        FROM dbo.Roles
        WHERE IsDeleted = 0
        GROUP BY TenantId, Name
        HAVING COUNT(*) > 1
    )
        THROW 51031, N'Active duplicate Roles exist for TenantId + Name; resolve data manually before migration.', 1;

    DECLARE @legacyExists bit = 0;
    DECLARE @canonicalExists bit = 0;
    DECLARE @legacyValid bit = 0;
    DECLARE @canonicalValid bit = 0;

    SELECT @legacyExists = CASE WHEN EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Roles')
          AND name = N'UQ_Roles_Tenant_Name'
    ) THEN 1 ELSE 0 END;

    SELECT @canonicalExists = CASE WHEN EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Roles')
          AND name = N'UX_Roles_Tenant_Name'
    ) THEN 1 ELSE 0 END;

    /* A same-key unique index under an unknown name is ambiguous and is not touched. */
    IF EXISTS (
        SELECT 1
        FROM sys.indexes i
        WHERE i.object_id = OBJECT_ID(N'dbo.Roles')
          AND i.is_unique = 1
          AND i.is_primary_key = 0
          AND i.is_unique_constraint = 0
          AND i.name NOT IN (N'UQ_Roles_Tenant_Name', N'UX_Roles_Tenant_Name')
          AND (
              SELECT COUNT(*)
              FROM sys.index_columns ic
              WHERE ic.object_id = i.object_id
                AND ic.index_id = i.index_id
                AND ic.is_included_column = 0
          ) = 2
          AND EXISTS (
              SELECT 1
              FROM sys.index_columns ic
              JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
              WHERE ic.object_id = i.object_id
                AND ic.index_id = i.index_id
                AND ic.key_ordinal = 1
                AND c.name = N'TenantId'
          )
          AND EXISTS (
              SELECT 1
              FROM sys.index_columns ic
              JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
              WHERE ic.object_id = i.object_id
                AND ic.index_id = i.index_id
                AND ic.key_ordinal = 2
                AND c.name = N'Name'
          )
    )
        THROW 51032, N'An unknown unique TenantId + Name index exists on dbo.Roles; migration aborted.', 1;

    IF @legacyExists = 1
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM sys.indexes i
            WHERE i.object_id = OBJECT_ID(N'dbo.Roles')
              AND i.name = N'UQ_Roles_Tenant_Name'
              AND i.is_unique = 1
              AND i.is_primary_key = 0
              AND i.is_unique_constraint = 0
              AND ISNULL(i.filter_definition, N'') = N''
              AND (
                  SELECT COUNT(*)
                  FROM sys.index_columns ic
                  WHERE ic.object_id = i.object_id
                    AND ic.index_id = i.index_id
                    AND ic.is_included_column = 0
              ) = 2
              AND EXISTS (
                  SELECT 1 FROM sys.index_columns ic
                  JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                  WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                    AND ic.key_ordinal = 1 AND c.name = N'TenantId'
              )
              AND EXISTS (
                  SELECT 1 FROM sys.index_columns ic
                  JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                  WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                    AND ic.key_ordinal = 2 AND c.name = N'Name'
              )
        ) SET @legacyValid = 1;
        ELSE
            THROW 51033, N'UQ_Roles_Tenant_Name exists with an unexpected definition; migration aborted.', 1;
    END;

    IF @canonicalExists = 1
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM sys.indexes i
            WHERE i.object_id = OBJECT_ID(N'dbo.Roles')
              AND i.name = N'UX_Roles_Tenant_Name'
              AND i.is_unique = 1
              AND i.is_primary_key = 0
              AND i.is_unique_constraint = 0
              AND REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(LOWER(ISNULL(i.filter_definition, N'')), N' ', N''), N'(', N''), N')', N''), N'[', N''), N']', N'') = N'isdeleted=0'
              AND (
                  SELECT COUNT(*)
                  FROM sys.index_columns ic
                  WHERE ic.object_id = i.object_id
                    AND ic.index_id = i.index_id
                    AND ic.is_included_column = 0
              ) = 2
              AND EXISTS (
                  SELECT 1 FROM sys.index_columns ic
                  JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                  WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                    AND ic.key_ordinal = 1 AND c.name = N'TenantId'
              )
              AND EXISTS (
                  SELECT 1 FROM sys.index_columns ic
                  JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                  WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                    AND ic.key_ordinal = 2 AND c.name = N'Name'
              )
        ) SET @canonicalValid = 1;
        ELSE
            THROW 51034, N'UX_Roles_Tenant_Name exists with an unexpected definition; migration aborted.', 1;
    END;

    IF @legacyExists = 1 AND @canonicalExists = 1
        THROW 51035, N'Both legacy and canonical Roles indexes exist; migration aborted for manual review.', 1;

    IF @canonicalValid = 0
    BEGIN
        IF @legacyValid = 1
            DROP INDEX UQ_Roles_Tenant_Name ON dbo.Roles;

        CREATE UNIQUE INDEX UX_Roles_Tenant_Name
            ON dbo.Roles (TenantId, Name)
            WHERE IsDeleted = 0;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes i
        WHERE i.object_id = OBJECT_ID(N'dbo.Roles')
          AND i.name = N'UX_Roles_Tenant_Name'
          AND i.is_unique = 1
          AND i.has_filter = 1
          AND REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(LOWER(ISNULL(i.filter_definition, N'')), N' ', N''), N'(', N''), N')', N''), N'[', N''), N']', N'') = N'isdeleted=0'
    )
        THROW 51036, N'Canonical UX_Roles_Tenant_Name validation failed.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
