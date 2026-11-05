/* Migração manual, não executar automaticamente.
   Apenas Individual -> INDIVIDUAL e Organization -> ORGANIZATION em PartyTypes.
   Não traduz valores desconhecidos e não altera IDs, FKs, traduções ou outros catálogos.
   Colisões incluem registos removidos: o modelo EF pode impor unicidade sem filtro.
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

    IF OBJECT_ID(N'dbo.PartyTypes', N'U') IS NULL
        THROW 51100, N'PartyTypes não existe. Verifique a base selecionada.', 1;

    DECLARE @Mappings TABLE (
        PreviousCode NVARCHAR(50) COLLATE Latin1_General_100_BIN2 PRIMARY KEY,
        CanonicalCode NVARCHAR(50) COLLATE Latin1_General_100_BIN2 NOT NULL
    );
    INSERT INTO @Mappings (PreviousCode, CanonicalCode)
    VALUES (N'Individual', N'INDIVIDUAL'), (N'Organization', N'ORGANIZATION');

    -- Impede inserções concorrentes entre a pré-validação e o UPDATE.
    DECLARE @RowCount BIGINT;
    SELECT @RowCount = COUNT_BIG(*) FROM dbo.PartyTypes WITH (TABLOCKX, HOLDLOCK);

    -- Comparação conservadora, incluindo variantes de casing/acento e apagados.
    -- Uma variante desconhecida não é convertida; se colidir com o destino, aborta.
    IF EXISTS (
        SELECT m.CanonicalCode
        FROM @Mappings m
        JOIN dbo.PartyTypes p ON p.Code COLLATE Latin1_General_100_CI_AI = m.CanonicalCode COLLATE Latin1_General_100_CI_AI
        GROUP BY m.CanonicalCode
        HAVING COUNT_BIG(*) > 1
    )
    BEGIN
        SELECT p.Id, p.Code, p.IsDeleted, m.CanonicalCode AS IntendedCode
        FROM @Mappings m
        JOIN dbo.PartyTypes p ON p.Code COLLATE Latin1_General_100_CI_AI = m.CanonicalCode COLLATE Latin1_General_100_CI_AI
        WHERE m.CanonicalCode IN (
            SELECT c.CanonicalCode FROM @Mappings c
            JOIN dbo.PartyTypes existing ON existing.Code COLLATE Latin1_General_100_CI_AI = c.CanonicalCode COLLATE Latin1_General_100_CI_AI
            GROUP BY c.CanonicalCode HAVING COUNT_BIG(*) > 1
        );
        THROW 51101, N'Colisão de Code canónico, incluindo registos removidos. Nenhum dado foi alterado.', 1;
    END;

    -- BIN2 torna a mudança de casing explícita mesmo numa base case-insensitive.
    UPDATE p
    SET p.Code = m.CanonicalCode
    OUTPUT inserted.Id, deleted.Code AS PreviousCode, inserted.Code AS CanonicalCode
    FROM dbo.PartyTypes p
    JOIN @Mappings m ON p.Code COLLATE Latin1_General_100_BIN2 = m.PreviousCode
                   AND DATALENGTH(p.Code) = DATALENGTH(m.PreviousCode);

    -- Valores fora do mapping são apenas apresentados, nunca interpretados/traduzidos.
    SELECT p.Id, p.Code AS PreservedUnknownCode, p.IsDeleted
    FROM dbo.PartyTypes p
    WHERE NOT EXISTS (
        SELECT 1 FROM @Mappings m
        WHERE p.Code COLLATE Latin1_General_100_BIN2 = m.CanonicalCode
          AND DATALENGTH(p.Code) = DATALENGTH(m.CanonicalCode)
    );

    COMMIT TRANSACTION;
    PRINT N'Normalização concluída. Valores já canónicos são preservados; reveja os valores desconhecidos apresentados.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
