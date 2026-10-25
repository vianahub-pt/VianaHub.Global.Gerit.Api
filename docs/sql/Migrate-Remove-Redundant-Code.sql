/* Migração manual: remove exclusivamente Code de Actions, Resources e Roles.
   Não executar automaticamente. Faça backup e homologue numa cópia da base.
   Dependências funcionais (módulos, colunas calculadas, FKs) bloqueiam a migração
   para revisão explícita; as FKs por Id de RolePermissions não são alteradas.
   Índices compostos que incluam Code também são dependências diretas removidas.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;

    -- A identidade semântica deve ser única antes da remoção da chave redundante.
    IF EXISTS (SELECT Name FROM dbo.Actions WHERE IsDeleted = 0 GROUP BY Name HAVING COUNT(*) > 1)
       OR EXISTS (SELECT Name FROM dbo.Resources WHERE IsDeleted = 0 GROUP BY Name HAVING COUNT(*) > 1)
    BEGIN
        SELECT N'Actions' AS TableName, Name, COUNT(*) AS DuplicateCount
        FROM dbo.Actions WHERE IsDeleted = 0 GROUP BY Name HAVING COUNT(*) > 1
        UNION ALL
        SELECT N'Resources', Name, COUNT(*)
        FROM dbo.Resources WHERE IsDeleted = 0 GROUP BY Name HAVING COUNT(*) > 1;
        THROW 51002, N'Existem nomes canónicos duplicados. Resolva os dados explicitamente antes da migração.', 1;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Actions') AND name = N'UX_Actions_Name')
    BEGIN
        CREATE UNIQUE INDEX UX_Actions_Name ON dbo.Actions (Name) WHERE IsDeleted = 0;
        PRINT N'Criado: UX_Actions_Name';
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Resources') AND name = N'UX_Resources_Name')
    BEGIN
        CREATE UNIQUE INDEX UX_Resources_Name ON dbo.Resources (Name) WHERE IsDeleted = 0;
        PRINT N'Criado: UX_Resources_Name';
    END;

    DECLARE @Targets TABLE (ObjectId INT PRIMARY KEY, ColumnId INT, TableName SYSNAME);
    INSERT INTO @Targets (ObjectId, ColumnId, TableName)
    SELECT t.object_id, c.column_id, t.name
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    JOIN sys.columns c ON c.object_id = t.object_id AND c.name = N'Code'
    WHERE s.name = N'dbo' AND t.name IN (N'Actions', N'Resources', N'Roles');

    -- Módulos e colunas calculadas exigem uma decisão funcional, nunca DROP cego.
    IF EXISTS (
        SELECT 1 FROM sys.sql_expression_dependencies d
        JOIN @Targets t ON t.ObjectId = d.referenced_id
        JOIN sys.objects o ON o.object_id = d.referencing_id
        WHERE d.referenced_minor_id IN (0, t.ColumnId)
          AND o.type NOT IN (N'C', N'D')
    )
    BEGIN
        SELECT OBJECT_SCHEMA_NAME(d.referencing_id) AS SchemaName,
               OBJECT_NAME(d.referencing_id) AS DependentObject, d.referencing_minor_id
        FROM sys.sql_expression_dependencies d
        JOIN @Targets t ON t.ObjectId = d.referenced_id
        JOIN sys.objects o ON o.object_id = d.referencing_id
        WHERE d.referenced_minor_id IN (0, t.ColumnId) AND o.type NOT IN (N'C', N'D');
        THROW 51000, N'Existem dependências SQL funcionais. Reveja os objetos listados antes da migração.', 1;
    END;

    IF EXISTS (
        SELECT 1 FROM sys.foreign_key_columns f
        JOIN @Targets t ON (f.parent_object_id = t.ObjectId AND f.parent_column_id = t.ColumnId)
                        OR (f.referenced_object_id = t.ObjectId AND f.referenced_column_id = t.ColumnId)
    )
        THROW 51001, N'Existe uma foreign key diretamente associada a Code. É necessária revisão manual.', 1;

    DECLARE @Commands TABLE (Sequence INT IDENTITY, Command NVARCHAR(MAX), ObjectName NVARCHAR(512));

    INSERT INTO @Commands (Command, ObjectName)
    SELECT N'ALTER TABLE dbo.' + QUOTENAME(t.TableName) + N' DROP CONSTRAINT ' + QUOTENAME(k.name) + N';', k.name
    FROM @Targets t
    JOIN sys.index_columns ic ON ic.object_id = t.ObjectId AND ic.column_id = t.ColumnId
    JOIN sys.key_constraints k ON k.parent_object_id = ic.object_id AND k.unique_index_id = ic.index_id;

    INSERT INTO @Commands (Command, ObjectName)
    SELECT N'DROP INDEX ' + QUOTENAME(i.name) + N' ON dbo.' + QUOTENAME(t.TableName) + N';', i.name
    FROM @Targets t
    JOIN sys.indexes i ON i.object_id = t.ObjectId
    WHERE i.is_primary_key = 0 AND i.is_unique_constraint = 0
      AND (EXISTS (
          SELECT 1 FROM sys.index_columns ic
          WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.column_id = t.ColumnId
      ) OR CHARINDEX(N'[Code]', i.filter_definition COLLATE Latin1_General_100_CI_AS) > 0);

    INSERT INTO @Commands (Command, ObjectName)
    SELECT N'ALTER TABLE dbo.' + QUOTENAME(t.TableName) + N' DROP CONSTRAINT ' + QUOTENAME(d.name) + N';', d.name
    FROM @Targets t
    JOIN sys.default_constraints d ON d.parent_object_id = t.ObjectId AND d.parent_column_id = t.ColumnId;

    INSERT INTO @Commands (Command, ObjectName)
    SELECT DISTINCT N'ALTER TABLE dbo.' + QUOTENAME(t.TableName) + N' DROP CONSTRAINT ' + QUOTENAME(c.name) + N';', c.name
    FROM @Targets t
    JOIN sys.check_constraints c ON c.parent_object_id = t.ObjectId
    WHERE c.parent_column_id = t.ColumnId OR EXISTS (
        SELECT 1 FROM sys.sql_expression_dependencies d
        WHERE d.referencing_id = c.object_id AND d.referenced_id = t.ObjectId AND d.referenced_minor_id = t.ColumnId
    );

    -- Estatísticas criadas pelo utilizador também podem impedir DROP COLUMN.
    INSERT INTO @Commands (Command, ObjectName)
    SELECT N'DROP STATISTICS dbo.' + QUOTENAME(t.TableName) + N'.' + QUOTENAME(s.name) + N';', s.name
    FROM @Targets t
    JOIN sys.stats_columns sc ON sc.object_id = t.ObjectId AND sc.column_id = t.ColumnId
    JOIN sys.stats s ON s.object_id = sc.object_id AND s.stats_id = sc.stats_id
    WHERE s.user_created = 1 AND NOT EXISTS (
        SELECT 1 FROM sys.indexes i WHERE i.object_id = s.object_id AND i.index_id = s.stats_id
    );

    INSERT INTO @Commands (Command, ObjectName)
    SELECT N'ALTER TABLE dbo.' + QUOTENAME(TableName) + N' DROP COLUMN [Code];', N'dbo.' + TableName + N'.Code'
    FROM @Targets;

    DECLARE @Sequence INT = 1, @Last INT, @Command NVARCHAR(MAX), @ObjectName NVARCHAR(512);
    SELECT @Last = MAX(Sequence) FROM @Commands;
    WHILE @Sequence <= @Last
    BEGIN
        SELECT @Command = Command, @ObjectName = ObjectName FROM @Commands WHERE Sequence = @Sequence;
        EXEC sys.sp_executesql @Command;
        PRINT N'Removido nesta transação: ' + @ObjectName;
        SET @Sequence += 1;
    END;

    COMMIT TRANSACTION;
    PRINT N'Migração concluída. Colunas já ausentes são ignoradas.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
