/* =========================================================
   INITIAL BACKOFFICE SEED v2 - CARGA REALISTA DE DEMONSTRACAO
   Repositorio: vianahub-pt/VianaHub.Global.Gerit

   Caracteristicas:
   - Tenant unico "Gerit Demo Lda" (demo@gerit.pt)
   - 2 utilizadores: Dener Viana (BackOffice) + Admin (Manager)
   - 3 meses de operacao (Janeiro-Marco 2026)
   - 15 clientes realistas (mix individuais/organizacoes)
    - 9 colaboradores, 3 equipas regionais
   - 20 visitas com equipas, veiculos e equipamentos
   - Dados fiscais realistas, coordenadas GPS de Portugal
   - Script idempotente (IF NOT EXISTS)
   - Assinatura STANDARD ativa

   Executar APOS Create-Tables.sql (ou apos Initial_BackOffice.sql)
   ========================================================= */

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

    IF OBJECT_ID(N'dbo.Tenants', N'U') IS NULL
        THROW 51000, 'O schema ainda nao foi criado. Execute primeiro o Create-Tables.sql.', 1;

    EXEC sys.sp_set_session_context @key = N'IsSuperAdmin', @value = 1;

    DECLARE @UtcNow DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @SeedActorId INT = 1;

    /* =========================================================
       1. PARTY TYPES E TRADUCOES
       ========================================================= */

    DECLARE @PartyTypeSeed TABLE (
        Id TINYINT PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL,
        PtName NVARCHAR(100) NOT NULL, PtDescription NVARCHAR(300) NULL,
        EnName NVARCHAR(100) NOT NULL, EnDescription NVARCHAR(300) NULL
    );
    INSERT INTO @PartyTypeSeed (Id, Code, PtName, PtDescription, EnName, EnDescription)
    VALUES
        (1, N'INDIVIDUAL', N'Pessoa singular', N'Representa uma pessoa singular.', N'Individual', N'Represents a natural person.'),
        (2, N'ORGANIZATION', N'Organizacao', N'Representa uma empresa, associacao ou outra pessoa coletiva.', N'Organization', N'Represents a company, association, or other legal entity.');

    UPDATE pt SET pt.Code = s.Code, pt.IsActive = 1, pt.IsDeleted = 0, pt.ModifiedBy = @SeedActorId, pt.ModifiedAt = @UtcNow
    FROM dbo.PartyTypes pt INNER JOIN @PartyTypeSeed s ON s.Id = pt.Id;

    INSERT INTO dbo.PartyTypes (Id, Code, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT s.Id, s.Code, 1, 0, @SeedActorId, @UtcNow FROM @PartyTypeSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PartyTypes pt WHERE pt.Id = s.Id);

    UPDATE tr SET tr.Name = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
        tr.Description = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM dbo.PartyTypeTranslations tr INNER JOIN @PartyTypeSeed s ON s.Id = tr.PartyTypeId
    WHERE tr.LanguageCode IN (N'pt-PT', N'en-US');

    INSERT INTO dbo.PartyTypeTranslations (PartyTypeId, LanguageCode, Name, Description)
    SELECT s.Id, l.LanguageCode,
        CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
        CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM @PartyTypeSeed s CROSS JOIN (VALUES (N'pt-PT'), (N'en-US')) l(LanguageCode)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PartyTypeTranslations tr WHERE tr.PartyTypeId = s.Id AND tr.LanguageCode = l.LanguageCode);

    /* =========================================================
       2. ORIGENS DE AQUISICAO
       ========================================================= */

    DECLARE @AcquisitionSourceSeed TABLE (
        Code NVARCHAR(50) PRIMARY KEY, PtName NVARCHAR(100) NOT NULL, PtDescription NVARCHAR(300) NULL,
        EnName NVARCHAR(100) NOT NULL, EnDescription NVARCHAR(300) NULL
    );
    INSERT INTO @AcquisitionSourceSeed (Code, PtName, PtDescription, EnName, EnDescription)
    VALUES
        (N'OTHER', N'Outros', N'Origem nao especificada ou nao classificada.', N'Other', N'Unspecified or unclassified acquisition source.'),
        (N'INSTAGRAM', N'Instagram', N'Cliente ou tenant originado atraves do Instagram.', N'Instagram', N'Client or tenant acquired through Instagram.'),
        (N'FACEBOOK', N'Facebook', N'Cliente ou tenant originado atraves do Facebook.', N'Facebook', N'Client or tenant acquired through Facebook.'),
        (N'LINKEDIN', N'LinkedIn', N'Cliente ou tenant originado atraves do LinkedIn.', N'LinkedIn', N'Client or tenant acquired through LinkedIn.'),
        (N'YOUTUBE', N'YouTube', N'Cliente ou tenant originado atraves do YouTube.', N'YouTube', N'Client or tenant acquired through YouTube.'),
        (N'WHATSAPP', N'WhatsApp', N'Cliente ou tenant originado atraves de contacto por WhatsApp.', N'WhatsApp', N'Client or tenant acquired through WhatsApp.'),
        (N'TIKTOK', N'TikTok', N'Cliente ou tenant originado atraves do TikTok.', N'TikTok', N'Client or tenant acquired through TikTok.'),
        (N'GOOGLE', N'Google', N'Cliente ou tenant originado atraves de pesquisa ou anuncio no Google.', N'Google', N'Client or tenant acquired through Google search or advertising.'),
        (N'REFERRAL', N'Indicacao', N'Cliente ou tenant originado por indicacao de amigos ou conhecidos.', N'Referral', N'Client or tenant acquired through a referral.'),
        (N'TV', N'Televisao', N'Cliente ou tenant originado atraves de publicidade ou mencao em televisao.', N'Television', N'Client or tenant acquired through television.'),
        (N'RADIO', N'Radio', N'Cliente ou tenant originado atraves de radio.', N'Radio', N'Client or tenant acquired through radio.'),
        (N'NEWSPAPER', N'Jornal', N'Cliente ou tenant originado atraves de jornal.', N'Newspaper', N'Client or tenant acquired through a newspaper.'),
        (N'MAGAZINE', N'Revista', N'Cliente ou tenant originado atraves de revista.', N'Magazine', N'Client or tenant acquired through a magazine.'),
        (N'EVENTS', N'Eventos', N'Cliente ou tenant originado atraves de eventos.', N'Events', N'Client or tenant acquired through events.');

    UPDATE t SET t.IsActive = 1, t.ModifiedBy = @SeedActorId, t.ModifiedAt = @UtcNow
    FROM dbo.AcquisitionSourceTypes t INNER JOIN @AcquisitionSourceSeed s ON s.Code = t.Code WHERE t.IsDeleted = 0;

    INSERT INTO dbo.AcquisitionSourceTypes (Code, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT s.Code, 1, 0, @SeedActorId, @UtcNow FROM @AcquisitionSourceSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.AcquisitionSourceTypes t WHERE t.Code = s.Code AND t.IsDeleted = 0);

    UPDATE tr SET tr.Name = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
        tr.Description = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM dbo.AcquisitionSourceTypeTranslations tr
    INNER JOIN dbo.AcquisitionSourceTypes p ON p.Id = tr.AcquisitionSourceTypeId AND p.IsDeleted = 0
    INNER JOIN @AcquisitionSourceSeed s ON s.Code = p.Code
    WHERE tr.LanguageCode IN (N'pt-PT', N'en-US');

    INSERT INTO dbo.AcquisitionSourceTypeTranslations (AcquisitionSourceTypeId, LanguageCode, Name, Description)
    SELECT p.Id, l.LanguageCode,
        CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
        CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM @AcquisitionSourceSeed s
    INNER JOIN dbo.AcquisitionSourceTypes p ON p.Code = s.Code AND p.IsDeleted = 0
    CROSS JOIN (VALUES (N'pt-PT'), (N'en-US')) l(LanguageCode)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.AcquisitionSourceTypeTranslations tr WHERE tr.AcquisitionSourceTypeId = p.Id AND tr.LanguageCode = l.LanguageCode);

    /* =========================================================
       3. TIPOS DE MORADA
       ========================================================= */

    DECLARE @AddressTypeSeed TABLE (
        Code NVARCHAR(50) PRIMARY KEY, PtName NVARCHAR(200) NOT NULL, PtDescription NVARCHAR(500) NULL,
        EnName NVARCHAR(200) NOT NULL, EnDescription NVARCHAR(500) NULL
    );
    INSERT INTO @AddressTypeSeed (Code, PtName, PtDescription, EnName, EnDescription)
    VALUES
        (N'RESIDENTIAL', N'Morada residencial', N'Morada para habitacao, usada como endereco principal.', N'Residential address', N'Address used as a person''s primary residence.'),
        (N'COMMERCIAL', N'Morada comercial', N'Morada de um negocio ou estabelecimento para atividade comercial.', N'Commercial address', N'Business or establishment address used for commercial activity.'),
        (N'INDUSTRIAL', N'Morada industrial', N'Morada associada a fabrica, unidade industrial ou armazem.', N'Industrial address', N'Address associated with a factory, industrial unit, or warehouse.'),
        (N'RURAL', N'Morada rural', N'Morada em area rural.', N'Rural address', N'Address in a rural area.'),
        (N'PUBLIC_SERVICE', N'Morada de servicos publicos', N'Morada de uma entidade ou servico publico.', N'Public service address', N'Address of a public authority or public service.'),
        (N'EDUCATION', N'Morada de educacao', N'Morada de uma instituicao de ensino.', N'Education address', N'Address of an educational institution.'),
        (N'HEALTHCARE', N'Morada de saude', N'Morada de um servico ou instituicao de saude.', N'Healthcare address', N'Address of a healthcare service or institution.'),
        (N'HOSPITALITY', N'Morada de alojamento ou turismo', N'Morada de uma unidade de alojamento ou turismo.', N'Hospitality address', N'Address of a hospitality or tourism establishment.'),
        (N'LOGISTICS', N'Morada logistica ou distribuicao', N'Morada dedicada a logistica, armazenamento ou distribuicao.', N'Logistics address', N'Address dedicated to logistics, warehousing, or distribution.'),
        (N'POSTAL_ALTERNATIVE', N'Morada postal alternativa', N'Morada alternativa para correspondencia ou entregas.', N'Alternative postal address', N'Alternative address for correspondence or deliveries.');

    UPDATE t SET t.IsActive = 1, t.ModifiedBy = @SeedActorId, t.ModifiedAt = @UtcNow
    FROM dbo.AddressTypes t INNER JOIN @AddressTypeSeed s ON s.Code = t.Code WHERE t.IsDeleted = 0;

    INSERT INTO dbo.AddressTypes (Code, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT s.Code, 1, 0, @SeedActorId, @UtcNow FROM @AddressTypeSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.AddressTypes t WHERE t.Code = s.Code AND t.IsDeleted = 0);

    UPDATE tr SET tr.Name = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
        tr.Description = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM dbo.AddressTypeTranslations tr
    INNER JOIN dbo.AddressTypes p ON p.Id = tr.AddressTypeId AND p.IsDeleted = 0
    INNER JOIN @AddressTypeSeed s ON s.Code = p.Code
    WHERE tr.LanguageCode IN (N'pt-PT', N'en-US');

    INSERT INTO dbo.AddressTypeTranslations (AddressTypeId, LanguageCode, Name, Description)
    SELECT p.Id, l.LanguageCode,
        CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
        CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM @AddressTypeSeed s
    INNER JOIN dbo.AddressTypes p ON p.Code = s.Code AND p.IsDeleted = 0
    CROSS JOIN (VALUES (N'pt-PT'), (N'en-US')) l(LanguageCode)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.AddressTypeTranslations tr WHERE tr.AddressTypeId = p.Id AND tr.LanguageCode = l.LanguageCode);

    /* =========================================================
       4. TIPOS DE DOCUMENTO
       ========================================================= */

    DECLARE @DocumentTypeSeed TABLE (
        Code NVARCHAR(50) PRIMARY KEY, PtName NVARCHAR(100) NOT NULL, PtDescription NVARCHAR(300) NULL,
        EnName NVARCHAR(100) NOT NULL, EnDescription NVARCHAR(300) NULL
    );
    INSERT INTO @DocumentTypeSeed (Code, PtName, PtDescription, EnName, EnDescription)
    VALUES
        (N'CITIZEN_CARD', N'Cartao de Cidadao', N'Documento de identificacao civil portugues.', N'Citizen card', N'Portuguese civil identification document.'),
        (N'PASSPORT', N'Passaporte', N'Documento internacional de identificacao e viagem.', N'Passport', N'International identity and travel document.'),
        (N'DRIVING_LICENSE', N'Carta de Conducao', N'Documento que autoriza a conducao de veiculos.', N'Driving licence', N'Document authorizing a person to drive vehicles.'),
        (N'TAX_IDENTIFICATION', N'Identificacao fiscal', N'Documento ou comprovativo de identificacao fiscal.', N'Tax identification', N'Tax identification document or certificate.'),
        (N'COMPANY_REGISTRATION', N'Registo comercial', N'Documento de registo ou constituicao de uma organizacao.', N'Company registration', N'Company incorporation or registration document.'),
        (N'OTHER', N'Outro documento', N'Outro tipo de documento nao classificado.', N'Other document', N'Other unclassified document type.');

    UPDATE t SET t.IsActive = 1, t.ModifiedBy = @SeedActorId, t.ModifiedAt = @UtcNow
    FROM dbo.DocumentTypes t INNER JOIN @DocumentTypeSeed s ON s.Code = t.Code WHERE t.IsDeleted = 0;

    INSERT INTO dbo.DocumentTypes (Code, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT s.Code, 1, 0, @SeedActorId, @UtcNow FROM @DocumentTypeSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.DocumentTypes t WHERE t.Code = s.Code AND t.IsDeleted = 0);

    UPDATE tr SET tr.Name = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
        tr.Description = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM dbo.DocumentTypeTranslations tr
    INNER JOIN dbo.DocumentTypes p ON p.Id = tr.DocumentTypeId AND p.IsDeleted = 0
    INNER JOIN @DocumentTypeSeed s ON s.Code = p.Code
    WHERE tr.LanguageCode IN (N'pt-PT', N'en-US');

    INSERT INTO dbo.DocumentTypeTranslations (DocumentTypeId, LanguageCode, Name, Description)
    SELECT p.Id, l.LanguageCode,
        CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
        CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM @DocumentTypeSeed s
    INNER JOIN dbo.DocumentTypes p ON p.Code = s.Code AND p.IsDeleted = 0
    CROSS JOIN (VALUES (N'pt-PT'), (N'en-US')) l(LanguageCode)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.DocumentTypeTranslations tr WHERE tr.DocumentTypeId = p.Id AND tr.LanguageCode = l.LanguageCode);

    /* =========================================================
       5. TIPOS DE FICHEIRO
       ========================================================= */

    DECLARE @FileTypeSeed TABLE (
        Code NVARCHAR(50) PRIMARY KEY, MimeType NVARCHAR(100) NULL, Extension NVARCHAR(20) NULL,
        PtName NVARCHAR(100) NOT NULL, PtDescription NVARCHAR(300) NULL,
        EnName NVARCHAR(100) NOT NULL, EnDescription NVARCHAR(300) NULL
    );
    INSERT INTO @FileTypeSeed (Code, MimeType, Extension, PtName, PtDescription, EnName, EnDescription)
    VALUES
        (N'JPEG', N'image/jpeg', N'jpg', N'Imagem JPEG', N'Imagem no formato JPEG.', N'JPEG image', N'Image in JPEG format.'),
        (N'PNG', N'image/png', N'png', N'Imagem PNG', N'Imagem no formato PNG.', N'PNG image', N'Image in PNG format.'),
        (N'GIF', N'image/gif', N'gif', N'Imagem GIF', N'Imagem no formato GIF.', N'GIF image', N'Image in GIF format.'),
        (N'WEBP', N'image/webp', N'webp', N'Imagem WebP', N'Imagem no formato WebP.', N'WebP image', N'Image in WebP format.'),
        (N'SVG', N'image/svg+xml', N'svg', N'Imagem SVG', N'Imagem vetorial no formato SVG.', N'SVG image', N'Vector image in SVG format.'),
        (N'PDF', N'application/pdf', N'pdf', N'Documento PDF', N'Documento no formato PDF.', N'PDF document', N'Document in PDF format.'),
        (N'DOC', N'application/msword', N'doc', N'Documento Word 97-2003', N'Documento Microsoft Word no formato DOC.', N'Word 97-2003 document', N'Microsoft Word document in DOC format.'),
        (N'DOCX', N'application/vnd.openxmlformats-officedocument.wordprocessingml.document', N'docx', N'Documento Word', N'Documento Microsoft Word no formato DOCX.', N'Word document', N'Microsoft Word document in DOCX format.'),
        (N'XLS', N'application/vnd.ms-excel', N'xls', N'Folha Excel 97-2003', N'Folha de calculo Microsoft Excel no formato XLS.', N'Excel 97-2003 workbook', N'Microsoft Excel workbook in XLS format.'),
        (N'XLSX', N'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', N'xlsx', N'Folha Excel', N'Folha de calculo Microsoft Excel no formato XLSX.', N'Excel workbook', N'Microsoft Excel workbook in XLSX format.'),
        (N'PPT', N'application/vnd.ms-powerpoint', N'ppt', N'Apresentacao PowerPoint 97-2003', N'Apresentacao Microsoft PowerPoint no formato PPT.', N'PowerPoint 97-2003 presentation', N'Microsoft PowerPoint presentation in PPT format.'),
        (N'PPTX', N'application/vnd.openxmlformats-officedocument.presentationml.presentation', N'pptx', N'Apresentacao PowerPoint', N'Apresentacao Microsoft PowerPoint no formato PPTX.', N'PowerPoint presentation', N'Microsoft PowerPoint presentation in PPTX format.'),
        (N'TXT', N'text/plain', N'txt', N'Ficheiro de texto', N'Ficheiro de texto simples.', N'Text file', N'Plain text file.'),
        (N'CSV', N'text/csv', N'csv', N'Ficheiro CSV', N'Ficheiro de valores separados por virgulas.', N'CSV file', N'Comma-separated values file.'),
        (N'JSON', N'application/json', N'json', N'Ficheiro JSON', N'Ficheiro de dados no formato JSON.', N'JSON file', N'Data file in JSON format.'),
        (N'XML', N'application/xml', N'xml', N'Ficheiro XML', N'Ficheiro de dados no formato XML.', N'XML file', N'Data file in XML format.'),
        (N'ZIP', N'application/zip', N'zip', N'Arquivo ZIP', N'Arquivo comprimido no formato ZIP.', N'ZIP archive', N'Compressed archive in ZIP format.'),
        (N'RAR', N'application/x-rar-compressed', N'rar', N'Arquivo RAR', N'Arquivo comprimido no formato RAR.', N'RAR archive', N'Compressed archive in RAR format.'),
        (N'SEVEN_ZIP', N'application/x-7z-compressed', N'7z', N'Arquivo 7-Zip', N'Arquivo comprimido no formato 7z.', N'7-Zip archive', N'Compressed archive in 7z format.'),
        (N'BINARY', N'application/octet-stream', N'bin', N'Ficheiro binario', N'Ficheiro binario generico.', N'Binary file', N'Generic binary file.');

    UPDATE t SET t.MimeType = s.MimeType, t.Extension = s.Extension, t.IsActive = 1,
        t.ModifiedBy = @SeedActorId, t.ModifiedAt = @UtcNow
    FROM dbo.FileTypes t INNER JOIN @FileTypeSeed s ON s.Code = t.Code WHERE t.IsDeleted = 0;

    INSERT INTO dbo.FileTypes (Code, MimeType, Extension, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT s.Code, s.MimeType, s.Extension, 1, 0, @SeedActorId, @UtcNow FROM @FileTypeSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.FileTypes t WHERE t.Code = s.Code AND t.IsDeleted = 0);

    UPDATE tr SET tr.Name = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
        tr.Description = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM dbo.FileTypeTranslations tr
    INNER JOIN dbo.FileTypes p ON p.Id = tr.FileTypeId AND p.IsDeleted = 0
    INNER JOIN @FileTypeSeed s ON s.Code = p.Code
    WHERE tr.LanguageCode IN (N'pt-PT', N'en-US');

    INSERT INTO dbo.FileTypeTranslations (FileTypeId, LanguageCode, Name, Description)
    SELECT p.Id, l.LanguageCode,
        CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
        CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM @FileTypeSeed s
    INNER JOIN dbo.FileTypes p ON p.Code = s.Code AND p.IsDeleted = 0
    CROSS JOIN (VALUES (N'pt-PT'), (N'en-US')) l(LanguageCode)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.FileTypeTranslations tr WHERE tr.FileTypeId = p.Id AND tr.LanguageCode = l.LanguageCode);

    /* =========================================================
       6. DOMINIOS DE STATUS
       ========================================================= */

    DECLARE @StatusDomainSeed TABLE (
        Code NVARCHAR(50) PRIMARY KEY, PtName NVARCHAR(100) NOT NULL, PtDescription NVARCHAR(300) NULL,
        EnName NVARCHAR(100) NOT NULL, EnDescription NVARCHAR(300) NULL
    );
    INSERT INTO @StatusDomainSeed (Code, PtName, PtDescription, EnName, EnDescription)
    VALUES
        (N'CLIENT', N'Cliente', N'Estados aplicaveis a clientes.', N'Client', N'Statuses applicable to clients.'),
        (N'EMPLOYEE', N'Colaborador', N'Estados aplicaveis a colaboradores.', N'Employee', N'Statuses applicable to employees.'),
        (N'EQUIPMENT', N'Equipamento', N'Estados aplicaveis a equipamentos.', N'Equipment', N'Statuses applicable to equipment.'),
        (N'VEHICLE', N'Veiculo', N'Estados aplicaveis a veiculos.', N'Vehicle', N'Statuses applicable to vehicles.'),
        (N'VISIT', N'Visita', N'Estados aplicaveis a visitas ou intervencoes.', N'Visit', N'Statuses applicable to visits or interventions.'),
        (N'SUBSCRIPTION', N'Assinatura', N'Estados aplicaveis a assinaturas.', N'Subscription', N'Statuses applicable to subscriptions.');

    UPDATE t SET t.IsActive = 1, t.ModifiedBy = @SeedActorId, t.ModifiedAt = @UtcNow
    FROM dbo.StatusDomains t INNER JOIN @StatusDomainSeed s ON s.Code = t.Code WHERE t.IsDeleted = 0;

    INSERT INTO dbo.StatusDomains (Code, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT s.Code, 1, 0, @SeedActorId, @UtcNow FROM @StatusDomainSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.StatusDomains t WHERE t.Code = s.Code AND t.IsDeleted = 0);

    UPDATE tr SET tr.Name = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
        tr.Description = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM dbo.StatusDomainTranslations tr
    INNER JOIN dbo.StatusDomains p ON p.Id = tr.StatusDomainId AND p.IsDeleted = 0
    INNER JOIN @StatusDomainSeed s ON s.Code = p.Code
    WHERE tr.LanguageCode IN (N'pt-PT', N'en-US');

    INSERT INTO dbo.StatusDomainTranslations (StatusDomainId, LanguageCode, Name, Description)
    SELECT p.Id, l.LanguageCode,
        CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
        CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM @StatusDomainSeed s
    INNER JOIN dbo.StatusDomains p ON p.Code = s.Code AND p.IsDeleted = 0
    CROSS JOIN (VALUES (N'pt-PT'), (N'en-US')) l(LanguageCode)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.StatusDomainTranslations tr WHERE tr.StatusDomainId = p.Id AND tr.LanguageCode = l.LanguageCode);

    /* =========================================================
       7. PLANOS E TRADUCOES
       ========================================================= */

    DECLARE @PlanSeed TABLE (
        Code NVARCHAR(50) PRIMARY KEY,
        PricePerHour DECIMAL(19,4) NULL, PricePerDay DECIMAL(19,4) NULL,
        PricePerMonth DECIMAL(19,4) NULL, PricePerYear DECIMAL(19,4) NULL,
        Currency NVARCHAR(3) NOT NULL, MaxUsers INT NOT NULL, MaxPhotosPerVisit INT NOT NULL,
        PtName NVARCHAR(100) NOT NULL, PtDescription NVARCHAR(500) NULL,
        EnName NVARCHAR(100) NOT NULL, EnDescription NVARCHAR(500) NULL
    );
    INSERT INTO @PlanSeed (Code, PricePerHour, PricePerDay, PricePerMonth, PricePerYear, Currency, MaxUsers, MaxPhotosPerVisit, PtName, PtDescription, EnName, EnDescription)
    VALUES
        (N'FREE', NULL, NULL, 0.0, 0.0, N'EUR', 1, 10, N'Gratuito', N'Plano gratuito com funcionalidades basicas.', N'Free', N'Free plan with basic features.'),
        (N'BASIC', NULL, NULL, 19.9, 199.0, N'EUR', 3, 50, N'Basico', N'Plano basico para pequenos negocios.', N'Basic', N'Basic plan for small businesses.'),
        (N'STANDARD', NULL, NULL, 49.9, 499.0, N'EUR', 10, 200, N'Standard', N'Plano intermedio com mais capacidade e funcionalidades avancadas.', N'Standard', N'Intermediate plan with greater capacity and advanced features.'),
        (N'PROFESSIONAL', NULL, NULL, 99.9, 999.0, N'EUR', 25, 500, N'Profissional', N'Plano avancado para equipas com maior volume.', N'Professional', N'Advanced plan for teams with higher volume.'),
        (N'ENTERPRISE', NULL, NULL, 199.9, 1999.0, N'EUR', 100, 2000, N'Enterprise', N'Plano completo para grandes organizacoes.', N'Enterprise', N'Complete plan for large organizations.'),
        (N'PAYG_HOURLY', 5.0, NULL, NULL, NULL, N'EUR', 10, 100, N'Pagamento por hora', N'Plano baseado em consumo por hora.', N'Pay as you go hourly', N'Usage-based plan billed by the hour.'),
        (N'PAYG_DAILY', NULL, 25.0, NULL, NULL, N'EUR', 10, 100, N'Pagamento por dia', N'Plano baseado em consumo por dia.', N'Pay as you go daily', N'Usage-based plan billed by the day.');

    UPDATE p SET p.PricePerHour = s.PricePerHour, p.PricePerDay = s.PricePerDay,
        p.PricePerMonth = s.PricePerMonth, p.PricePerYear = s.PricePerYear,
        p.Currency = s.Currency, p.MaxUsers = s.MaxUsers, p.MaxPhotosPerVisit = s.MaxPhotosPerVisit,
        p.IsActive = 1, p.ModifiedBy = @SeedActorId, p.ModifiedAt = @UtcNow
    FROM dbo.SubscriptionPlans p INNER JOIN @PlanSeed s ON s.Code = p.Code WHERE p.IsDeleted = 0;

    INSERT INTO dbo.SubscriptionPlans (Code, PricePerHour, PricePerDay, PricePerMonth, PricePerYear, Currency, MaxUsers, MaxPhotosPerVisit, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT s.Code, s.PricePerHour, s.PricePerDay, s.PricePerMonth, s.PricePerYear, s.Currency, s.MaxUsers, s.MaxPhotosPerVisit, 1, 0, @SeedActorId, @UtcNow
    FROM @PlanSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.SubscriptionPlans p WHERE p.Code = s.Code AND p.IsDeleted = 0);

    UPDATE tr SET tr.Name = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
        tr.Description = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM dbo.SubscriptionPlanTranslations tr
    INNER JOIN dbo.SubscriptionPlans p ON p.Id = tr.SubscriptionPlanId AND p.IsDeleted = 0
    INNER JOIN @PlanSeed s ON s.Code = p.Code
    WHERE tr.LanguageCode IN (N'pt-PT', N'en-US');

    INSERT INTO dbo.SubscriptionPlanTranslations (SubscriptionPlanId, LanguageCode, Name, Description)
    SELECT p.Id, l.LanguageCode,
        CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
        CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM @PlanSeed s
    INNER JOIN dbo.SubscriptionPlans p ON p.Code = s.Code AND p.IsDeleted = 0
    CROSS JOIN (VALUES (N'pt-PT'), (N'en-US')) l(LanguageCode)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.SubscriptionPlanTranslations tr WHERE tr.SubscriptionPlanId = p.Id AND tr.LanguageCode = l.LanguageCode);

    INSERT INTO dbo.SubscriptionPlanFileRules (SubscriptionPlanId, FileTypeId, MaxFileSizeMB, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT p.Id, f.Id,
        CASE p.Code WHEN N'FREE' THEN 2 WHEN N'BASIC' THEN 10 WHEN N'STANDARD' THEN 25
            WHEN N'PROFESSIONAL' THEN 75 WHEN N'ENTERPRISE' THEN 500
            WHEN N'PAYG_HOURLY' THEN 50 WHEN N'PAYG_DAILY' THEN 50 ELSE 10 END,
        1, 0, @SeedActorId, @UtcNow
    FROM dbo.SubscriptionPlans p CROSS JOIN dbo.FileTypes f
    WHERE p.IsDeleted = 0 AND f.IsDeleted = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.SubscriptionPlanFileRules r WHERE r.SubscriptionPlanId = p.Id AND r.FileTypeId = f.Id AND r.IsDeleted = 0);

    /* =========================================================
       8. ACOES E RECURSOS DE AUTORIZACAO
       ========================================================= */

    DECLARE @ActionSeed TABLE (Name NVARCHAR(50) PRIMARY KEY, Description NVARCHAR(500) NOT NULL);
    INSERT INTO @ActionSeed (Name, Description)
    VALUES
        (N'GetAll', N'Obter todos os registos.'),
        (N'GetBy', N'Obter um registo por identificador ou criterio.'),
        (N'GetPaged', N'Obter uma lista paginada com filtros.'),
        (N'Create', N'Criar um novo registo.'),
        (N'Update', N'Atualizar um registo existente.'),
        (N'Activate', N'Ativar um registo.'),
        (N'Deactivate', N'Desativar um registo.'),
        (N'Delete', N'Eliminar logicamente um registo.'),
        (N'BulkUpload', N'Efetuar cadastro em massa de registos.'),
        (N'Execute', N'Executar uma acao especifica.'),
        (N'GetActive', N'Obter registos ativos.'),
        (N'GetExpiring', N'Obter registos proximos da expiracao.'),
        (N'Cancel', N'Cancelar uma operacao ou entidade.'),
        (N'Renew', N'Renovar um contrato, assinatura ou entidade.');

    UPDATE a SET a.Description = s.Description, a.IsActive = 1, a.ModifiedBy = @SeedActorId, a.ModifiedAt = @UtcNow
    FROM dbo.Actions a INNER JOIN @ActionSeed s ON s.Name = a.Name WHERE a.IsDeleted = 0;

    INSERT INTO dbo.Actions (Name, Description, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT s.Name, s.Description, 1, 0, @SeedActorId, @UtcNow FROM @ActionSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Actions a WHERE a.Name = s.Name AND a.IsDeleted = 0);

    DECLARE @ResourceSeed TABLE (Name NVARCHAR(100) PRIMARY KEY, Description NVARCHAR(500) NOT NULL);
    INSERT INTO @ResourceSeed (Name, Description)
    VALUES
        (N'PartyTypes', N'Tipos de partes: pessoa singular ou organizacao.'),
        (N'AcquisitionSourceTypes', N'Tipos de origem de aquisicao comercial.'),
        (N'AddressTypes', N'Tipos de morada disponiveis.'),
        (N'DocumentTypes', N'Tipos de documentos.'),
        (N'FileTypes', N'Tipos de ficheiros.'),
        (N'StatusDomains', N'Dominios funcionais de status.'),
        (N'SubscriptionPlans', N'Planos de assinatura.'),
        (N'SubscriptionPlanFileRules', N'Regras de ficheiros por plano.'),
        (N'Tenants', N'Tenants do sistema.'),
        (N'TenantContactPersons', N'Pessoas de contacto dos tenants.'),
        (N'TenantAddresses', N'Moradas dos tenants.'),
        (N'TenantFiscalData', N'Dados fiscais dos tenants.'),
        (N'TenantDocuments', N'Documentos dos tenants.'),
        (N'StatusDefinitions', N'Definicoes de status por tenant e dominio.'),
        (N'Subscriptions', N'Assinaturas dos tenants.'),
        (N'Users', N'Utilizadores do sistema.'),
        (N'UserPreferences', N'Preferencias dos utilizadores.'),
        (N'Roles', N'Papeis ou perfis de acesso.'),
        (N'Resources', N'Recursos protegidos pelo sistema de autorizacao.'),
        (N'Actions', N'Acoes disponiveis no sistema de autorizacao.'),
        (N'RolePermissions', N'Permissoes associadas aos papeis.'),
        (N'UserRoles', N'Papeis associados aos utilizadores.'),
        (N'RefreshTokens', N'Tokens de atualizacao.'),
        (N'JwtKeys', N'Chaves de assinatura JWT.'),
        (N'JobDefinitions', N'Definicoes de jobs e processos.'),
        (N'Clients', N'Clientes do sistema.'),
        (N'ClientAddresses', N'Moradas dos clientes.'),
        (N'ClientContactPersons', N'Pessoas de contacto dos clientes.'),
        (N'ClientDocuments', N'Documentos dos clientes.'),
        (N'ClientFiscalData', N'Dados fiscais dos clientes.'),
        (N'Teams', N'Equipas.'),
        (N'Employees', N'Colaboradores.'),
        (N'EmployeeContactPersons', N'Pessoas de contacto dos colaboradores.'),
        (N'EmployeeAddresses', N'Moradas dos colaboradores.'),
        (N'EmployeeFiscalData', N'Dados fiscais dos colaboradores.'),
        (N'EmployeeTeam', N'Relacao historica entre colaboradores e equipas.'),
        (N'EquipmentTypes', N'Tipos de equipamentos.'),
        (N'Equipments', N'Equipamentos.'),
        (N'Vehicles', N'Veiculos.'),
        (N'Visits', N'Visitas ou intervencoes.'),
        (N'VisitContactPersons', N'Pessoas de contacto das visitas.'),
        (N'VisitAddresses', N'Moradas das visitas.'),
        (N'VisitTeams', N'Equipas associadas as visitas.'),
        (N'VisitTeamFunctions', N'Funcoes operacionais nas equipas de visita.'),
        (N'VisitTeamEmployee', N'Colaboradores associados as equipas de visita.'),
        (N'VisitTeamVehicles', N'Veiculos associados as equipas de visita.'),
        (N'VisitTeamEquipments', N'Equipamentos associados as equipas de visita.'),
        (N'VisitAttachments', N'Anexos das visitas.');

    UPDATE r SET r.Description = s.Description, r.IsActive = 1, r.ModifiedBy = @SeedActorId, r.ModifiedAt = @UtcNow
    FROM dbo.Resources r INNER JOIN @ResourceSeed s ON s.Name = r.Name WHERE r.IsDeleted = 0;

    INSERT INTO dbo.Resources (Name, Description, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT s.Name, s.Description, 1, 0, @SeedActorId, @UtcNow FROM @ResourceSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Resources r WHERE r.Name = s.Name AND r.IsDeleted = 0);

    /* =========================================================
       9. TENANT - GERIT DEMO LDA
       ========================================================= */

    INSERT INTO dbo.Tenants (PartyTypeId, AcquisitionSourceTypeId, Name, Email, WebsiteUrl, ImageUrl, Note, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT pt.Id, ast.Id, N'Gerit Demo Lda', N'demo@gerit.pt', N'https://demo.gerit.pt', NULL,
           N'Tenant de demonstracao com dados realistas - Janeiro a Marco 2026',
           1, 0, @SeedActorId, @UtcNow
    FROM dbo.PartyTypes pt
    INNER JOIN dbo.AcquisitionSourceTypes ast ON ast.Code = N'OTHER' AND ast.IsDeleted = 0
    WHERE pt.Code = N'ORGANIZATION' AND pt.IsDeleted = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.Tenants t WHERE t.Email = N'demo@gerit.pt' AND t.IsDeleted = 0);

    UPDATE t SET t.Name = N'Gerit Demo Lda', t.WebsiteUrl = N'https://demo.gerit.pt',
        t.Note = N'Tenant de demonstracao com dados realistas - Janeiro a Marco 2026',
        t.IsActive = 1, t.ModifiedBy = @SeedActorId, t.ModifiedAt = @UtcNow
    FROM dbo.Tenants t WHERE t.Email = N'demo@gerit.pt' AND t.IsDeleted = 0;

    DECLARE @DemoTenantId INT;
    SELECT @DemoTenantId = Id FROM dbo.Tenants WHERE Email = N'demo@gerit.pt' AND IsDeleted = 0;
    IF @DemoTenantId IS NULL THROW 51001, 'Falha ao criar/obter o tenant Gerit Demo Lda.', 1;

    /* =========================================================
       10. TENANT CONTACT PERSONS
       ========================================================= */

    INSERT INTO dbo.TenantContactPersons (TenantId, JobTitle, Department, Name, PhoneNumber, CellPhoneNumber, IsCellPhoneWhatsapp, Email, IsPrimary, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, N'Administrador', NULL, N'Dener Viana',
           N'+351253000001', N'+351960268353', 1, N'viana.dener@gmail.com',
           1, 1, 0, @SeedActorId, @UtcNow
    WHERE NOT EXISTS (SELECT 1 FROM dbo.TenantContactPersons cp WHERE cp.TenantId = @DemoTenantId AND cp.Email = N'viana.dener@gmail.com' AND cp.IsDeleted = 0);

    INSERT INTO dbo.TenantContactPersons (TenantId, JobTitle, Department, Name, PhoneNumber, CellPhoneNumber, IsCellPhoneWhatsapp, Email, IsPrimary, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, N'Gestor de Operacoes', NULL, N'Admin Demo',
           N'+351253000002', N'+351910000001', 1, N'admin@geritapp.com',
           0, 1, 0, @SeedActorId, @UtcNow
    WHERE NOT EXISTS (SELECT 1 FROM dbo.TenantContactPersons cp WHERE cp.TenantId = @DemoTenantId AND cp.Email = N'admin@geritapp.com' AND cp.IsDeleted = 0);

    /* =========================================================
       11. TENANT ADDRESS - ESCRITORIO PORTO
       ========================================================= */

    INSERT INTO dbo.TenantAddresses (TenantId, AddressTypeId, CountryCode, Street, Neighborhood, City, District, PostalCode, StreetNumber, Complement, Latitude, Longitude, Note, IsPrimary, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, atp.Id, 'PT',
           N'Rua de Santa Catarina', N'Baixa', N'Porto', N'Porto', N'4000-447',
           N'200', N'3. Andar', 41.149600, -8.611000,
           N'Escritorio principal - Gerit Demo',
           1, 1, 0, @SeedActorId, @UtcNow
    FROM dbo.AddressTypes atp
    WHERE atp.Code = N'COMMERCIAL' AND atp.IsDeleted = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.TenantAddresses a WHERE a.TenantId = @DemoTenantId AND a.IsPrimary = 1 AND a.IsDeleted = 0);

    /* =========================================================
       12. TENANT FISCAL DATA
       ========================================================= */

    INSERT INTO dbo.TenantFiscalData (TenantId, TaxNumber, VatNumber, FiscalCountry, IsVatRegistered, IBAN, FiscalEmail, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, N'515999888', N'PT515999888', 'PT', 1,
           N'PT50003300004567890123456', N'fiscal@gerit.pt',
           1, 0, @SeedActorId, @UtcNow
    WHERE NOT EXISTS (SELECT 1 FROM dbo.TenantFiscalData fd WHERE fd.TenantId = @DemoTenantId AND fd.IsActive = 1 AND fd.IsDeleted = 0);

    /* =========================================================
       13. TENANT DOCUMENTS
       ========================================================= */

    INSERT INTO dbo.TenantDocuments (TenantId, DocumentTypeId, DocumentNumber, IssuingCountryCode, IssuedAt, ExpiresAt, IsPrimary, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, dt.Id, N'515999888', 'PT', '2020-06-15', NULL, 1, 1, 0, @SeedActorId, @UtcNow
    FROM dbo.DocumentTypes dt
    WHERE dt.Code = N'COMPANY_REGISTRATION' AND dt.IsDeleted = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.TenantDocuments d WHERE d.TenantId = @DemoTenantId AND d.DocumentTypeId = dt.Id AND d.IsPrimary = 1 AND d.IsDeleted = 0);

    /* =========================================================
       14. STATUS DEFINITIONS (POR TENANT)
       ========================================================= */

    DECLARE @StatusDefinitionSeed TABLE (
        DomainCode NVARCHAR(50) NOT NULL, StatusCode NVARCHAR(50) NOT NULL, DisplayOrder INT NOT NULL,
        PtName NVARCHAR(200) NOT NULL, PtDescription NVARCHAR(500) NULL,
        EnName NVARCHAR(200) NOT NULL, EnDescription NVARCHAR(500) NULL,
        PRIMARY KEY (DomainCode, StatusCode)
    );
    INSERT INTO @StatusDefinitionSeed (DomainCode, StatusCode, DisplayOrder, PtName, PtDescription, EnName, EnDescription)
    VALUES
        (N'CLIENT', N'PROSPECT', 10, N'Potencial', N'Cliente potencial ainda nao convertido.', N'Prospect', N'Potential client not yet converted.'),
        (N'CLIENT', N'ACTIVE', 20, N'Ativo', N'Cliente ativo.', N'Active', N'Active client.'),
        (N'CLIENT', N'INACTIVE', 30, N'Inativo', N'Cliente temporariamente inativo.', N'Inactive', N'Temporarily inactive client.'),
        (N'CLIENT', N'SUSPENDED', 40, N'Suspenso', N'Cliente com atividade suspensa.', N'Suspended', N'Client with suspended activity.'),
        (N'CLIENT', N'ARCHIVED', 50, N'Arquivado', N'Cliente arquivado para historico.', N'Archived', N'Client archived for historical purposes.'),
        (N'EMPLOYEE', N'ACTIVE', 10, N'Ativo', N'Colaborador ativo.', N'Active', N'Active employee.'),
        (N'EMPLOYEE', N'ON_LEAVE', 20, N'Ausente', N'Colaborador temporariamente ausente.', N'On leave', N'Employee temporarily on leave.'),
        (N'EMPLOYEE', N'INACTIVE', 30, N'Inativo', N'Colaborador inativo.', N'Inactive', N'Inactive employee.'),
        (N'EMPLOYEE', N'TERMINATED', 40, N'Desvinculado', N'Colaborador sem vinculo ativo.', N'Terminated', N'Employee whose engagement has ended.'),
        (N'EQUIPMENT', N'AVAILABLE', 10, N'Disponivel', N'Equipamento disponivel para utilizacao.', N'Available', N'Equipment available for use.'),
        (N'EQUIPMENT', N'IN_USE', 20, N'Em utilizacao', N'Equipamento atualmente em utilizacao.', N'In use', N'Equipment currently in use.'),
        (N'EQUIPMENT', N'MAINTENANCE', 30, N'Em manutencao', N'Equipamento em manutencao.', N'Under maintenance', N'Equipment undergoing maintenance.'),
        (N'EQUIPMENT', N'RETIRED', 40, N'Retirado', N'Equipamento retirado de operacao.', N'Retired', N'Equipment retired from service.'),
        (N'VEHICLE', N'AVAILABLE', 10, N'Disponivel', N'Veiculo disponivel para utilizacao.', N'Available', N'Vehicle available for use.'),
        (N'VEHICLE', N'IN_USE', 20, N'Em utilizacao', N'Veiculo atualmente em utilizacao.', N'In use', N'Vehicle currently in use.'),
        (N'VEHICLE', N'MAINTENANCE', 30, N'Em manutencao', N'Veiculo em manutencao.', N'Under maintenance', N'Vehicle undergoing maintenance.'),
        (N'VEHICLE', N'RETIRED', 40, N'Retirado', N'Veiculo retirado de operacao.', N'Retired', N'Vehicle retired from service.'),
        (N'VISIT', N'SCHEDULED', 10, N'Agendada', N'Visita criada e agendada para uma data futura.', N'Scheduled', N'Visit created and scheduled for a future date.'),
        (N'VISIT', N'CONFIRMED', 20, N'Confirmada', N'Visita confirmada com o cliente.', N'Confirmed', N'Visit confirmed with the client.'),
        (N'VISIT', N'EN_ROUTE', 30, N'Em deslocacao', N'Equipa em deslocacao para o local.', N'En route', N'Team traveling to the location.'),
        (N'VISIT', N'IN_PROGRESS', 40, N'Em andamento', N'Visita em execucao.', N'In progress', N'Visit currently being performed.'),
        (N'VISIT', N'PAUSED', 50, N'Em pausa', N'Visita temporariamente pausada.', N'Paused', N'Visit temporarily paused.'),
        (N'VISIT', N'WAITING_CLIENT', 60, N'A aguardar cliente', N'Visita parada a aguardar acao do cliente.', N'Waiting for client', N'Visit waiting for client action.'),
        (N'VISIT', N'WAITING_MATERIAL', 70, N'A aguardar material', N'Visita suspensa por falta de material.', N'Waiting for material', N'Visit waiting for required material.'),
        (N'VISIT', N'RESCHEDULED', 80, N'Reagendada', N'Visita reagendada para nova data.', N'Rescheduled', N'Visit rescheduled to a new date.'),
        (N'VISIT', N'COMPLETED', 90, N'Concluida', N'Visita concluida com sucesso.', N'Completed', N'Visit completed successfully.'),
        (N'VISIT', N'COMPLETED_PENDING', 100, N'Concluida com pendencias', N'Visita concluida com itens pendentes.', N'Completed with pending items', N'Visit completed with pending items.'),
        (N'VISIT', N'CANCELED', 110, N'Cancelada', N'Visita cancelada.', N'Canceled', N'Visit canceled.'),
        (N'VISIT', N'NOT_PERFORMED', 120, N'Nao realizada', N'Visita nao realizada.', N'Not performed', N'Visit was not performed.'),
        (N'VISIT', N'VALIDATING', 130, N'Em validacao', N'Visita a aguardar validacao.', N'Under validation', N'Visit awaiting validation.'),
        (N'VISIT', N'INVOICED', 140, N'Faturada', N'Visita ja faturada.', N'Invoiced', N'Visit already invoiced.'),
        (N'VISIT', N'ARCHIVED', 150, N'Arquivada', N'Visita encerrada e arquivada.', N'Archived', N'Visit closed and archived.'),
        (N'SUBSCRIPTION', N'TRIAL', 10, N'Em periodo experimental', N'Assinatura em periodo experimental.', N'Trial', N'Subscription in trial period.'),
        (N'SUBSCRIPTION', N'ACTIVE', 20, N'Ativa', N'Assinatura ativa.', N'Active', N'Active subscription.'),
        (N'SUBSCRIPTION', N'PAST_DUE', 30, N'Pagamento em atraso', N'Assinatura com pagamento em atraso.', N'Past due', N'Subscription with overdue payment.'),
        (N'SUBSCRIPTION', N'CANCELED', 40, N'Cancelada', N'Assinatura cancelada.', N'Canceled', N'Canceled subscription.'),
        (N'SUBSCRIPTION', N'EXPIRED', 50, N'Expirada', N'Assinatura expirada.', N'Expired', N'Expired subscription.');

    UPDATE sd SET sd.DisplayOrder = s.DisplayOrder, sd.IsSystem = 1, sd.IsActive = 1, sd.ModifiedBy = @SeedActorId, sd.ModifiedAt = @UtcNow
    FROM dbo.StatusDefinitions sd
    INNER JOIN dbo.StatusDomains d ON d.Id = sd.StatusDomainId AND d.IsDeleted = 0
    INNER JOIN @StatusDefinitionSeed s ON s.DomainCode = d.Code AND s.StatusCode = sd.Code
    WHERE sd.TenantId = @DemoTenantId AND sd.IsDeleted = 0;

    INSERT INTO dbo.StatusDefinitions (TenantId, StatusDomainId, Code, DisplayOrder, IsSystem, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, d.Id, s.StatusCode, s.DisplayOrder, 1, 1, 0, @SeedActorId, @UtcNow
    FROM @StatusDefinitionSeed s
    INNER JOIN dbo.StatusDomains d ON d.Code = s.DomainCode AND d.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.StatusDefinitions sd WHERE sd.TenantId = @DemoTenantId AND sd.StatusDomainId = d.Id AND sd.Code = s.StatusCode AND sd.IsDeleted = 0);

    UPDATE tr SET tr.Name = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
        tr.Description = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM dbo.StatusDefinitionTranslations tr
    INNER JOIN dbo.StatusDefinitions sd ON sd.Id = tr.StatusDefinitionId AND sd.TenantId = tr.TenantId AND sd.StatusDomainId = tr.StatusDomainId AND sd.IsDeleted = 0
    INNER JOIN dbo.StatusDomains d ON d.Id = sd.StatusDomainId AND d.IsDeleted = 0
    INNER JOIN @StatusDefinitionSeed s ON s.DomainCode = d.Code AND s.StatusCode = sd.Code
    WHERE tr.LanguageCode IN (N'pt-PT', N'en-US');

    INSERT INTO dbo.StatusDefinitionTranslations (TenantId, StatusDomainId, StatusDefinitionId, LanguageCode, Name, Description)
    SELECT sd.TenantId, sd.StatusDomainId, sd.Id, l.LanguageCode,
        CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
        CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM dbo.StatusDefinitions sd
    INNER JOIN dbo.StatusDomains d ON d.Id = sd.StatusDomainId AND d.IsDeleted = 0
    INNER JOIN @StatusDefinitionSeed s ON s.DomainCode = d.Code AND s.StatusCode = sd.Code
    CROSS JOIN (VALUES (N'pt-PT'), (N'en-US')) l(LanguageCode)
    WHERE sd.TenantId = @DemoTenantId AND sd.IsDeleted = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.StatusDefinitionTranslations tr WHERE tr.TenantId = sd.TenantId AND tr.StatusDefinitionId = sd.Id AND tr.LanguageCode = l.LanguageCode);

    /* =========================================================
       15. ASSINATURA - PLANO STANDARD (DESDE JANEIRO 2026)
       ========================================================= */

    INSERT INTO dbo.Subscriptions (TenantId, StatusDefinitionId, StatusDomainId, SubscriptionPlanId,
        StripeId, AgreedAmount, BillingInterval, CurrencyCode,
        CurrentPeriodStart, CurrentPeriodEnd, TrialStart, TrialEnd,
        CancelAtPeriodEnd, CanceledAt, CancellationReason, StripeCustomerId,
        IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, sd.Id, d.Id, p.Id,
           N'sub_demo_std_2026', 49.90, N'MONTHLY', N'EUR',
           DATETIME2FROMPARTS(2026, 1, 1, 0, 0, 0, 0, 0),
           DATETIME2FROMPARTS(2026, 12, 31, 23, 59, 59, 0, 0),
           DATETIME2FROMPARTS(2025, 12, 15, 0, 0, 0, 0, 0),
           DATETIME2FROMPARTS(2026, 1, 14, 23, 59, 59, 0, 0),
           0, NULL, NULL, N'cus_demo_gerit_001',
           1, 0, @SeedActorId, @UtcNow
    FROM dbo.SubscriptionPlans p
    INNER JOIN dbo.StatusDomains d ON d.Code = N'SUBSCRIPTION' AND d.IsDeleted = 0
    INNER JOIN dbo.StatusDefinitions sd ON sd.TenantId = @DemoTenantId AND sd.StatusDomainId = d.Id AND sd.Code = N'ACTIVE' AND sd.IsDeleted = 0
    WHERE p.Code = N'STANDARD' AND p.IsDeleted = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.Subscriptions s WHERE s.TenantId = @DemoTenantId AND s.IsActive = 1 AND s.IsDeleted = 0);

    /* =========================================================
       16. UTILIZADORES
       ========================================================= */

    DECLARE @DemoPasswordHash NVARCHAR(500) = N'AQAAAAIAAYagAAAAEAr88ZwhIrd69foEZO57diA8qdyfk3QkMPoCo9KxZ/CKlP1tFN7QPk6dHdMM2bQCNA==';

    DECLARE @UserSeed TABLE (
        Name NVARCHAR(150) NOT NULL, Email NVARCHAR(256) NOT NULL,
        NormalizedEmail NVARCHAR(256) NOT NULL, EmailConfirmed BIT NOT NULL,
        PhoneNumber NVARCHAR(50) NULL, PhoneNumberConfirmed BIT NOT NULL, UrlImage NVARCHAR(500) NULL
    );
    INSERT INTO @UserSeed (Name, Email, NormalizedEmail, EmailConfirmed, PhoneNumber, PhoneNumberConfirmed, UrlImage)
    VALUES
        (N'Dener Viana', N'viana.dener@gmail.com', N'VIANA.DENER@GMAIL.COM', 1, N'960268353', 0, N'www.gerit.pt/users/viana.dener.jpg'),
        (N'Admin', N'admin@geritapp.com', N'ADMIN@GERITAPP.COM', 1, NULL, 0, NULL);

    UPDATE u SET u.Name = s.Name, u.Email = s.Email, u.EmailConfirmed = s.EmailConfirmed,
        u.PhoneNumber = s.PhoneNumber, u.PhoneNumberConfirmed = s.PhoneNumberConfirmed,
        u.PasswordHash = @DemoPasswordHash, u.UrlImage = s.UrlImage,
        u.IsActive = 1, u.ModifiedBy = @SeedActorId, u.ModifiedAt = @UtcNow
    FROM dbo.Users u INNER JOIN @UserSeed s ON s.NormalizedEmail = u.NormalizedEmail
    WHERE u.TenantId = @DemoTenantId AND u.IsDeleted = 0;

    INSERT INTO dbo.Users (TenantId, Name, Email, NormalizedEmail, EmailConfirmed, PhoneNumber, PhoneNumberConfirmed, LastAccessAt, PasswordHash, UrlImage, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, s.Name, s.Email, s.NormalizedEmail, s.EmailConfirmed, s.PhoneNumber, s.PhoneNumberConfirmed,
           NULL, @DemoPasswordHash, s.UrlImage, 1, 0, @SeedActorId, @UtcNow
    FROM @UserSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Users u WHERE u.TenantId = @DemoTenantId AND u.NormalizedEmail = s.NormalizedEmail AND u.IsDeleted = 0);

    /* =========================================================
       17. USER PREFERENCES
       ========================================================= */

    INSERT INTO dbo.UserPreferences (TenantId, UserId, Appearance, CurrencyCode, Locale, Timezone, DateFormat, TimeFormat, DayStart, DayEnd,
        EmailNewsletter, EmailWeeklyReport, EmailApproval, EmailAlerts, EmailReminders, EmailPlanner, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT u.TenantId, u.Id,
           N'light', N'EUR', N'pt-PT', N'Europe/Lisbon',
           N'DD-MM-YYYY', N'24h', CONVERT(TIME(0), '09:00'), CONVERT(TIME(0), '18:00'),
           0, 0, 0, 1, 1, 1, 1, 0, @SeedActorId, @UtcNow
    FROM dbo.Users u INNER JOIN @UserSeed s ON s.NormalizedEmail = u.NormalizedEmail
    WHERE u.TenantId = @DemoTenantId AND u.IsDeleted = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.UserPreferences p WHERE p.TenantId = u.TenantId AND p.UserId = u.Id AND p.IsActive = 1 AND p.IsDeleted = 0);

    /* =========================================================
       18. ROLES
       ========================================================= */

    DECLARE @RoleSeed TABLE (Name NVARCHAR(100) PRIMARY KEY, Description NVARCHAR(500) NOT NULL);
    INSERT INTO @RoleSeed (Name, Description)
    VALUES
        (N'Admin', N'Acesso administrativo completo ao tenant.'),
        (N'BackOffice', N'Acesso a operacoes internas e administrativas.'),
        (N'Manager', N'Gestao de equipas, clientes e operacoes.'),
        (N'Operator', N'Execucao de tarefas operacionais.'),
        (N'User', N'Acesso basico ao sistema.'),
        (N'SuperAdmin', N'Acesso total ao sistema e configuracoes avancadas.');

    INSERT INTO dbo.Roles (TenantId, Name, Description, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, s.Name, s.Description, 1, 0, @SeedActorId, @UtcNow
    FROM @RoleSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.TenantId = @DemoTenantId AND r.Name = s.Name AND r.IsDeleted = 0);

    /* =========================================================
       19. ROLE PERMISSIONS
       ========================================================= */

    -- BackOffice: acesso total
    ;WITH BackOfficeRole AS (
        SELECT r.Id AS RoleId FROM dbo.Roles r WHERE r.TenantId = @DemoTenantId AND r.Name = N'BackOffice' AND r.IsDeleted = 0
    )
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, ResourceId, ActionId)
    SELECT @DemoTenantId, bor.RoleId, res.Id, act.Id
    FROM BackOfficeRole bor
    CROSS JOIN dbo.Resources res CROSS JOIN dbo.Actions act
    WHERE res.IsActive = 1 AND res.IsDeleted = 0 AND act.IsActive = 1 AND act.IsDeleted = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.TenantId = @DemoTenantId AND rp.RoleId = bor.RoleId AND rp.ResourceId = res.Id AND rp.ActionId = act.Id);

    -- Manager: acesso de gestao (clientes, visitas, equipas, colaboradores, veiculos, equipamentos)
    DECLARE @ManagerResources TABLE (ResourceName NVARCHAR(50) PRIMARY KEY);
    INSERT INTO @ManagerResources (ResourceName)
    VALUES
        (N'Clients'),(N'ClientAddresses'),(N'ClientContactPersons'),(N'ClientDocuments'),(N'ClientFiscalData'),
        (N'Visits'),(N'VisitContactPersons'),(N'VisitAddresses'),
        (N'VisitTeams'),(N'VisitTeamEmployee'),(N'VisitTeamVehicles'),(N'VisitTeamEquipments'),
        (N'VisitTeamFunctions'),(N'VisitAttachments'),
        (N'Teams'),(N'Employees'),(N'EmployeeContactPersons'),(N'EmployeeAddresses'),
        (N'EmployeeFiscalData'),(N'EmployeeTeam'),
        (N'EquipmentTypes'),(N'Equipments'),(N'Vehicles');

    ;WITH ManagerRole AS (
        SELECT r.Id AS RoleId FROM dbo.Roles r WHERE r.TenantId = @DemoTenantId AND r.Name = N'Manager' AND r.IsDeleted = 0
    )
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, ResourceId, ActionId)
    SELECT @DemoTenantId, mr.RoleId, res.Id, act.Id
    FROM ManagerRole mr
    CROSS JOIN @ManagerResources mr2
    INNER JOIN dbo.Resources res ON res.Name = mr2.ResourceName AND res.IsDeleted = 0
    CROSS JOIN dbo.Actions act
    WHERE act.IsActive = 1 AND act.IsDeleted = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.TenantId = @DemoTenantId AND rp.RoleId = mr.RoleId AND rp.ResourceId = res.Id AND rp.ActionId = act.Id);

    /* =========================================================
       20. USER ROLES
       ========================================================= */

    ;WITH TargetUsers AS (
        SELECT u.Id AS UserId, r.Id AS RoleId
        FROM @UserSeed s
        INNER JOIN dbo.Users u ON u.TenantId = @DemoTenantId AND u.NormalizedEmail = s.NormalizedEmail AND u.IsDeleted = 0
        INNER JOIN dbo.Roles r ON r.TenantId = @DemoTenantId AND r.IsDeleted = 0
        WHERE (s.Email = N'viana.dener@gmail.com' AND r.Name = N'BackOffice')
           OR (s.Email = N'admin@geritapp.com' AND r.Name = N'Manager')
    )
    INSERT INTO dbo.UserRoles (TenantId, UserId, RoleId)
    SELECT @DemoTenantId, tu.UserId, tu.RoleId FROM TargetUsers tu
    WHERE NOT EXISTS (SELECT 1 FROM dbo.UserRoles ur WHERE ur.TenantId = @DemoTenantId AND ur.UserId = tu.UserId AND ur.RoleId = tu.RoleId);

    /* =========================================================
       21. CLIENTES (15 - MIX INDIVIDUAIS E ORGANIZACOES)
       ========================================================= */

    DECLARE @ClientSeed TABLE (
        SeedKey NVARCHAR(20) NOT NULL, PartyTypeCode NVARCHAR(50) NOT NULL, Name NVARCHAR(500) NOT NULL,
        PhoneNumber NVARCHAR(50) NULL, CellPhoneNumber NVARCHAR(50) NULL, IsWhatsapp BIT NOT NULL,
        Email NVARCHAR(320) NULL, WebsiteUrl NVARCHAR(500) NULL,
        BirthDate DATE NULL, Gender NVARCHAR(30) NULL, Nationality NVARCHAR(100) NULL,
        CompanyRegistrationNumber NVARCHAR(100) NULL, EconomicActivityCode NVARCHAR(20) NULL, NumberOfEmployees INT NULL,
        Note NVARCHAR(1000) NOT NULL, AcquisitionSourceCode NVARCHAR(50) NOT NULL, StatusCode NVARCHAR(50) NOT NULL,
        TaxNumber NVARCHAR(20) NOT NULL, VatNumber NVARCHAR(20) NULL, IsVatRegistered BIT NOT NULL, FiscalEmail NVARCHAR(255) NULL,
        AddressTypeCode NVARCHAR(50) NOT NULL, Street NVARCHAR(200) NOT NULL, Neighborhood NVARCHAR(100) NULL,
        City NVARCHAR(100) NOT NULL, District NVARCHAR(100) NULL, PostalCode NVARCHAR(20) NOT NULL,
        StreetNumber NVARCHAR(20) NULL, Complement NVARCHAR(100) NULL,
        Latitude DECIMAL(9,6) NULL, Longitude DECIMAL(9,6) NULL,
        ContactName NVARCHAR(150) NOT NULL, ContactJobTitle NVARCHAR(150) NULL,
        DocumentTypeCode NVARCHAR(50) NULL, DocumentNumber NVARCHAR(100) NULL
    );
    INSERT INTO @ClientSeed (SeedKey, PartyTypeCode, Name, PhoneNumber, CellPhoneNumber, IsWhatsapp, Email, WebsiteUrl, BirthDate, Gender, Nationality, CompanyRegistrationNumber, EconomicActivityCode, NumberOfEmployees, Note, AcquisitionSourceCode, StatusCode, TaxNumber, VatNumber, IsVatRegistered, FiscalEmail, AddressTypeCode, Street, Neighborhood, City, District, PostalCode, StreetNumber, Complement, Latitude, Longitude, ContactName, ContactJobTitle, DocumentTypeCode, DocumentNumber)
    VALUES
        (N'CLI-001', N'INDIVIDUAL', N'Maria Joao Ferreira', N'222000001', N'912345001', 1, N'maria.jferreira@email.pt', NULL, '1985-03-15', N'Feminino', N'Portuguesa', NULL, NULL, NULL, N'Cliente residencial - moradia Porto', N'INSTAGRAM', N'ACTIVE', N'220111222', NULL, 0, N'maria.jferreira@email.pt', N'RESIDENTIAL', N'Rua de Cedofeita', N'Cedofeita', N'Porto', N'Porto', N'4050-174', N'45', N'1. Esq.', 41.155000, -8.619000, N'Maria Joao Ferreira', NULL, N'CITIZEN_CARD', N'14523678'),
        (N'CLI-002', N'INDIVIDUAL', N'Pedro Santos', N'211000002', N'912345002', 1, N'pedro.santos@email.pt', NULL, '1990-07-22', N'Masculino', N'Portuguesa', NULL, NULL, NULL, N'Cliente residencial - apartamento Lisboa', N'GOOGLE', N'ACTIVE', N'220111333', NULL, 0, N'pedro.santos@email.pt', N'RESIDENTIAL', N'Avenida da Republica', N'Avenidas Novas', N'Lisboa', N'Lisboa', N'1000-190', N'120', N'5. Dt.', 38.739000, -9.147000, N'Pedro Santos', NULL, N'CITIZEN_CARD', N'14523679'),
        (N'CLI-003', N'INDIVIDUAL', N'Ana Oliveira', N'253000003', N'912345003', 1, N'ana.oliveira@email.pt', NULL, '1992-11-08', N'Feminino', N'Portuguesa', NULL, NULL, NULL, N'Cliente residencial - Braga centro', N'FACEBOOK', N'ACTIVE', N'220111444', NULL, 0, N'ana.oliveira@email.pt', N'RESIDENTIAL', N'Avenida Central', N'Centro', N'Braga', N'Braga', N'4710-229', N'33', N'2. Esq.', 41.551000, -8.422000, N'Ana Oliveira', NULL, N'CITIZEN_CARD', N'14523680'),
        (N'CLI-004', N'INDIVIDUAL', N'Rui Goncalves', N'239000004', N'912345004', 1, N'rui.goncalves@email.pt', NULL, '1988-04-30', N'Masculino', N'Portuguesa', NULL, NULL, NULL, N'Cliente residencial - Coimbra sul', N'REFERRAL', N'ACTIVE', N'220111555', NULL, 0, N'rui.goncalves@email.pt', N'RESIDENTIAL', N'Rua Ferreira Borges', N'Baixa', N'Coimbra', N'Coimbra', N'3000-179', N'76', N'R/C Dt.', 40.207000, -8.428000, N'Rui Goncalves', NULL, N'CITIZEN_CARD', N'14523681'),
        (N'CLI-005', N'INDIVIDUAL', N'Carla Martins', N'289000005', N'912345005', 1, N'carla.martins@email.pt', NULL, '1983-09-12', N'Feminino', N'Portuguesa', NULL, NULL, NULL, N'Cliente residencial - Faro centro', N'INSTAGRAM', N'ACTIVE', N'220111666', NULL, 0, N'carla.martins@email.pt', N'RESIDENTIAL', N'Rua de Santo Antonio', N'Centro', N'Faro', N'Faro', N'8000-283', N'22', N'3.', 37.016000, -7.935000, N'Carla Martins', NULL, N'CITIZEN_CARD', N'14523682'),
        (N'CLI-006', N'INDIVIDUAL', N'Manuel Rodrigues', N'234000006', N'912345006', 1, N'manuel.rodrigues@email.pt', NULL, '1978-01-25', N'Masculino', N'Portuguesa', NULL, NULL, NULL, N'Cliente residencial - Aveiro', N'WHATSAPP', N'ACTIVE', N'220111777', NULL, 0, N'manuel.rodrigues@email.pt', N'RESIDENTIAL', N'Rua Direita', N'Centro', N'Aveiro', N'Aveiro', N'3810-005', N'15', NULL, 40.641000, -8.654000, N'Manuel Rodrigues', NULL, N'CITIZEN_CARD', N'14523683'),
        (N'CLI-007', N'INDIVIDUAL', N'Patricia Mendes', N'265000007', N'912345007', 1, N'patricia.mendes@email.pt', NULL, '1991-06-17', N'Feminino', N'Portuguesa', NULL, NULL, NULL, N'Cliente residencial - Setubal', N'LINKEDIN', N'ACTIVE', N'220111888', NULL, 0, N'patricia.mendes@email.pt', N'RESIDENTIAL', N'Avenida Luisa Todi', N'Centro', N'Setubal', N'Setubal', N'2900-452', N'88', N'1. Dt.', 38.524000, -8.893000, N'Patricia Mendes', NULL, N'CITIZEN_CARD', N'14523684'),
        (N'CLI-008', N'ORGANIZATION', N'Silva & Filhos, Lda', N'222000008', N'912345008', 1, N'geral@silvaefilhos.pt', N'https://silvaefilhos.pt', NULL, NULL, NULL, N'510111222', N'43320', 25, N'Empresa de construcao civil - Porto', N'GOOGLE', N'ACTIVE', N'510111222', N'PT510111222', 1, N'fiscal@silvaefilhos.pt', N'COMMERCIAL', N'Rua de Costa Cabral', N'Paranhos', N'Porto', N'Porto', N'4200-216', N'150', N'Armazem B', 41.169000, -8.601000, N'Antonio Silva', N'Gerente', N'COMPANY_REGISTRATION', N'510111222'),
        (N'CLI-009', N'ORGANIZATION', N'Tech Solutions, Unipessoal Lda', N'211000009', N'912345009', 1, N'info@techsolutions.pt', N'https://techsolutions.pt', NULL, NULL, NULL, N'510111333', N'62010', 12, N'Empresa de desenvolvimento de software - Lisboa', N'LINKEDIN', N'ACTIVE', N'510111333', N'PT510111333', 1, N'fiscal@techsolutions.pt', N'COMMERCIAL', N'Avenida da Liberdade', N'Santo Antonio', N'Lisboa', N'Lisboa', N'1250-096', N'200', N'7. Andar', 38.720000, -9.142000, N'Joao Teixeira', N'CEO', N'COMPANY_REGISTRATION', N'510111333'),
        (N'CLI-010', N'ORGANIZATION', N'Farmacia Central, Lda', N'222000010', N'912345010', 1, N'farmacia.central@email.pt', NULL, NULL, NULL, NULL, N'510111444', N'47730', 8, N'Farmacia - Porto centro', N'EVENTS', N'ACTIVE', N'510111444', N'PT510111444', 1, N'fiscal@farmaciacentral.pt', N'COMMERCIAL', N'Rua de Santa Catarina', N'Santo Ildefonso', N'Porto', N'Porto', N'4000-447', N'350', N'Loja 2', 41.149000, -8.608000, N'Dr. Sofia Almeida', N'Diretora Tecnica', N'COMPANY_REGISTRATION', N'510111444'),
        (N'CLI-011', N'ORGANIZATION', N'Restaurante O Forno, Lda', N'253000011', N'912345011', 1, N'reservas@oforno.pt', N'https://oforno.pt', NULL, NULL, NULL, N'510111555', N'56101', 15, N'Restaurante - Braga centro historico', N'INSTAGRAM', N'ACTIVE', N'510111555', N'PT510111555', 1, N'fiscal@oforno.pt', N'COMMERCIAL', N'Rua do Souto', N'Centro Historico', N'Braga', N'Braga', N'4700-329', N'22', N'R/C', 41.550000, -8.427000, N'Miguel Oliveira', N'Gerente', N'COMPANY_REGISTRATION', N'510111555'),
        (N'CLI-012', N'ORGANIZATION', N'Porto Creative Agency, Lda', N'222000012', N'912345012', 1, N'hello@portocreative.pt', N'https://portocreative.pt', NULL, NULL, NULL, N'510111666', N'73110', 14, N'Agencia de publicidade - Porto', N'TIKTOK', N'ACTIVE', N'510111666', N'PT510111666', 1, N'fiscal@portocreative.pt', N'COMMERCIAL', N'Rua Miguel Bombarda', N'Cedofeita', N'Porto', N'Porto', N'4050-379', N'110', N'Galeria 3', 41.152000, -8.619000, N'Helena Pinto', N'Diretora Criativa', N'COMPANY_REGISTRATION', N'510111666'),
        (N'CLI-013', N'ORGANIZATION', N'Lisboa Digital Services, Lda', N'211000013', N'912345013', 1, N'contacto@lisboadigital.pt', N'https://lisboadigital.pt', NULL, NULL, NULL, N'510111777', N'63110', 32, N'Servicos de transformacao digital - Lisboa', N'GOOGLE', N'ACTIVE', N'510111777', N'PT510111777', 1, N'fiscal@lisboadigital.pt', N'COMMERCIAL', N'Rua Augusta', N'Baixa', N'Lisboa', N'Lisboa', N'1100-048', N'250', N'4. Andar', 38.711000, -9.137000, N'Patricia Ramos', N'COO', N'COMPANY_REGISTRATION', N'510111777'),
        (N'CLI-014', N'ORGANIZATION', N'Coimbra Business Consulting, Lda', N'239000014', N'912345014', 1, N'geral@coimbraconsulting.pt', N'https://coimbraconsulting.pt', NULL, NULL, NULL, N'510111888', N'70220', 22, N'Consultoria empresarial - Coimbra', N'LINKEDIN', N'ACTIVE', N'510111888', N'PT510111888', 1, N'fiscal@coimbraconsulting.pt', N'COMMERCIAL', N'Rua do Brasil', N'Celas', N'Coimbra', N'Coimbra', N'3030-175', N'45', N'2.', 40.213000, -8.418000, N'Miguel Santos', N'Socio-Gerente', N'COMPANY_REGISTRATION', N'510111888'),
        (N'CLI-015', N'INDIVIDUAL', N'Joao Costa', N'282000015', N'912345015', 1, N'joao.costa@email.pt', NULL, '1995-12-03', N'Masculino', N'Portuguesa', NULL, NULL, NULL, N'Lead em prospecao - contactou via site', N'GOOGLE', N'PROSPECT', N'220111999', NULL, 0, N'joao.costa@email.pt', N'RESIDENTIAL', N'Avenida da Republica', N'Centro', N'Portimao', N'Faro', N'8500-300', N'70', N'2. Esq.', 37.138000, -8.538000, N'Joao Costa', NULL, N'CITIZEN_CARD', N'14523685');

    ;WITH ResolvedClients AS (
        SELECT s.*,
               CASE WHEN s.PartyTypeCode = N'INDIVIDUAL' THEN CONVERT(TINYINT, 1) ELSE CONVERT(TINYINT, 2) END AS PartyTypeId,
               ast.Id AS AcquisitionSourceTypeId, d.Id AS StatusDomainId, sd.Id AS StatusDefinitionId
        FROM @ClientSeed s
        INNER JOIN dbo.AcquisitionSourceTypes ast ON ast.Code = s.AcquisitionSourceCode AND ast.IsDeleted = 0
        INNER JOIN dbo.StatusDomains d ON d.Code = N'CLIENT' AND d.IsDeleted = 0
        INNER JOIN dbo.StatusDefinitions sd ON sd.TenantId = @DemoTenantId AND sd.StatusDomainId = d.Id AND sd.Code = s.StatusCode AND sd.IsDeleted = 0
    )
    INSERT INTO dbo.Clients (TenantId, PartyTypeId, StatusDefinitionId, StatusDomainId, AcquisitionSourceTypeId, Name,
        PhoneNumber, CellPhoneNumber, IsCellPhoneWhatsapp, Email, ImageUrl, WebsiteUrl,
        BirthDate, Gender, Nationality,
        CompanyRegistrationNumber, EconomicActivityCode, NumberOfEmployees,
        Note, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, rc.PartyTypeId, rc.StatusDefinitionId, rc.StatusDomainId, rc.AcquisitionSourceTypeId, rc.Name,
           rc.PhoneNumber, rc.CellPhoneNumber, rc.IsWhatsapp, rc.Email, NULL, rc.WebsiteUrl,
           CASE WHEN rc.PartyTypeId = 1 THEN rc.BirthDate ELSE NULL END,
           CASE WHEN rc.PartyTypeId = 1 THEN rc.Gender ELSE NULL END,
           CASE WHEN rc.PartyTypeId = 1 THEN rc.Nationality ELSE NULL END,
           CASE WHEN rc.PartyTypeId = 2 THEN rc.CompanyRegistrationNumber ELSE NULL END,
           CASE WHEN rc.PartyTypeId = 2 THEN rc.EconomicActivityCode ELSE NULL END,
           CASE WHEN rc.PartyTypeId = 2 THEN rc.NumberOfEmployees ELSE NULL END,
           rc.Note, 1, 0, @SeedActorId, @UtcNow
    FROM ResolvedClients rc
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Clients c WHERE c.TenantId = @DemoTenantId AND c.Note = rc.Note AND c.IsDeleted = 0);

    /* =========================================================
       22. CLIENT FISCAL DATA
       ========================================================= */

    INSERT INTO dbo.ClientFiscalData (TenantId, ClientId, TaxNumber, VatNumber, FiscalCountry, IsVatRegistered, IBAN, FiscalEmail, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, c.Id, s.TaxNumber, s.VatNumber, 'PT', s.IsVatRegistered, NULL, s.FiscalEmail, 1, 0, @SeedActorId, @UtcNow
    FROM @ClientSeed s
    INNER JOIN dbo.Clients c ON c.TenantId = @DemoTenantId AND c.Note = s.Note AND c.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.ClientFiscalData fd WHERE fd.TenantId = @DemoTenantId AND fd.ClientId = c.Id AND fd.IsActive = 1 AND fd.IsDeleted = 0);

    /* =========================================================
       23. CLIENT ADDRESSES
       ========================================================= */

    INSERT INTO dbo.ClientAddresses (TenantId, ClientId, AddressTypeId, CountryCode, Street, Neighborhood, City, District, PostalCode, StreetNumber, Complement, Latitude, Longitude, Note, IsPrimary, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, c.Id, atp.Id, 'PT',
           s.Street, s.Neighborhood, s.City, s.District, s.PostalCode,
           s.StreetNumber, s.Complement, s.Latitude, s.Longitude,
           N'Morada principal do cliente.', 1, 1, 0, @SeedActorId, @UtcNow
    FROM @ClientSeed s
    INNER JOIN dbo.Clients c ON c.TenantId = @DemoTenantId AND c.Note = s.Note AND c.IsDeleted = 0
    INNER JOIN dbo.AddressTypes atp ON atp.Code = s.AddressTypeCode AND atp.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.ClientAddresses a WHERE a.TenantId = @DemoTenantId AND a.ClientId = c.Id AND a.IsPrimary = 1 AND a.IsDeleted = 0);

    /* =========================================================
       24. CLIENT CONTACT PERSONS
       ========================================================= */

    INSERT INTO dbo.ClientContactPersons (TenantId, ClientId, JobTitle, Department, Name, PhoneNumber, CellPhoneNumber, IsCellPhoneWhatsapp, Email, IsPrimary, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, c.Id,
           CASE WHEN s.PartyTypeCode = N'ORGANIZATION' THEN s.ContactJobTitle ELSE NULL END,
           NULL, s.ContactName, s.PhoneNumber, s.CellPhoneNumber, s.IsWhatsapp, s.Email,
           1, 1, 0, @SeedActorId, @UtcNow
    FROM @ClientSeed s
    INNER JOIN dbo.Clients c ON c.TenantId = @DemoTenantId AND c.Note = s.Note AND c.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.ClientContactPersons cp WHERE cp.TenantId = @DemoTenantId AND cp.ClientId = c.Id AND cp.Email = s.Email AND cp.IsDeleted = 0);

    /* =========================================================
       25. CLIENT DOCUMENTS
       ========================================================= */

    INSERT INTO dbo.ClientDocuments (TenantId, ClientId, DocumentTypeId, DocumentNumber, IssuingCountryCode, IssuedAt, ExpiresAt, IsPrimary, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, c.Id, dt.Id, s.DocumentNumber, 'PT', NULL, NULL, 1, 1, 0, @SeedActorId, @UtcNow
    FROM @ClientSeed s
    INNER JOIN dbo.Clients c ON c.TenantId = @DemoTenantId AND c.Note = s.Note AND c.IsDeleted = 0
    INNER JOIN dbo.DocumentTypes dt ON dt.Code = s.DocumentTypeCode AND dt.IsDeleted = 0
    WHERE s.DocumentTypeCode IS NOT NULL AND s.DocumentNumber IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.ClientDocuments d WHERE d.TenantId = @DemoTenantId AND d.ClientId = c.Id AND d.DocumentTypeId = dt.Id AND d.IssuingCountryCode = 'PT' AND d.DocumentNumber = s.DocumentNumber AND d.IsDeleted = 0);

    /* =========================================================
       26. EQUIPAS (3 - REGIONAIS)
       ========================================================= */

    DECLARE @TeamSeed TABLE (Name NVARCHAR(150) NOT NULL, Description NVARCHAR(500) NOT NULL);
    INSERT INTO @TeamSeed (Name, Description)
    VALUES
        (N'Equipa Norte', N'Regiao Norte - Porto, Braga, Aveiro. Especialistas em instalacao e manutencao.'),
        (N'Equipa Sul', N'Regiao Sul - Lisboa, Setubal, Faro. Especialistas em consultoria e inspecao.'),
        (N'Equipa Centro', N'Regiao Centro - Coimbra, Aveiro, Leiria. Especialistas em reparacao e suporte.');

    INSERT INTO dbo.Teams (TenantId, Name, Description, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, s.Name, s.Description, 1, 0, @SeedActorId, @UtcNow FROM @TeamSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Teams t WHERE t.TenantId = @DemoTenantId AND t.Name = s.Name AND t.IsDeleted = 0);

    /* =========================================================
        27. COLABORADORES (9)
       ========================================================= */

    DECLARE @EmployeeSeed TABLE (
        SeedKey NVARCHAR(20) NOT NULL, Name NVARCHAR(150) NOT NULL,
        PhoneNumber NVARCHAR(50) NULL, CellPhoneNumber NVARCHAR(50) NULL, IsWhatsapp BIT NOT NULL,
        Email NVARCHAR(320) NULL, StatusCode NVARCHAR(50) NOT NULL, TaxNumber NVARCHAR(20) NOT NULL,
        AddressTypeCode NVARCHAR(50) NOT NULL, Street NVARCHAR(200) NOT NULL, Neighborhood NVARCHAR(100) NULL,
        City NVARCHAR(100) NOT NULL, District NVARCHAR(100) NULL, PostalCode NVARCHAR(20) NOT NULL,
        StreetNumber NVARCHAR(20) NULL, Complement NVARCHAR(100) NULL,
        Latitude DECIMAL(9,6) NULL, Longitude DECIMAL(9,6) NULL,
        ContactJobTitle NVARCHAR(150) NULL, TeamName NVARCHAR(150) NULL, IsLeader BIT NOT NULL
    );
    INSERT INTO @EmployeeSeed (SeedKey, Name, PhoneNumber, CellPhoneNumber, IsWhatsapp, Email, StatusCode, TaxNumber, AddressTypeCode, Street, Neighborhood, City, District, PostalCode, StreetNumber, Complement, Latitude, Longitude, ContactJobTitle, TeamName, IsLeader)
    VALUES
        (N'EMP-001', N'Carlos Mendes', N'253000101', N'916000101', 1, N'carlos.mendes@gerit.pt', N'ACTIVE', N'230111001', N'RESIDENTIAL', N'Rua do Almada', N'Centro', N'Porto', N'Porto', N'4050-036', N'12', N'2. Dt.', 41.148000, -8.614000, N'Tecnico Senior', N'Equipa Norte', 1),
        (N'EMP-002', N'Sofia Pereira', N'222000102', N'916000102', 1, N'sofia.pereira@gerit.pt', N'ACTIVE', N'230111002', N'RESIDENTIAL', N'Rua de Faria Guimaraes', N'Bonfim', N'Porto', N'Porto', N'4000-205', N'34', N'1.', 41.155000, -8.602000, N'Tecnica de Manutencao', N'Equipa Norte', 0),
        (N'EMP-003', N'Rafael Almeida', N'211000103', N'916000103', 1, N'rafael.almeida@gerit.pt', N'ACTIVE', N'230111003', N'RESIDENTIAL', N'Rua Morais Soares', N'Arroios', N'Lisboa', N'Lisboa', N'1900-350', N'56', N'3. Esq.', 38.733000, -9.127000, N'Tecnico de Inspecao', N'Equipa Norte', 0),
        (N'EMP-004', N'Ines Sousa', N'212000104', N'916000104', 1, N'ines.sousa@gerit.pt', N'ACTIVE', N'230111004', N'RESIDENTIAL', N'Avenida de Roma', N'Areeiro', N'Lisboa', N'Lisboa', N'1000-260', N'78', N'6. Dt.', 38.744000, -9.135000, N'Consultora Tecnica', N'Equipa Sul', 0),
        (N'EMP-005', N'Bruno Marques', N'239000105', N'916000105', 1, N'bruno.marques@gerit.pt', N'ON_LEAVE', N'230111005', N'RESIDENTIAL', N'Rua Padre Antonio Vieira', N'Celas', N'Coimbra', N'Coimbra', N'3000-315', N'22', N'R/C Esq.', 40.214000, -8.417000, N'Tecnico de Reparacao', N'Equipa Centro', 1),
        (N'EMP-006', N'Diana Costa', N'239000106', N'916000106', 1, N'diana.costa@gerit.pt', N'ACTIVE', N'230111006', N'RESIDENTIAL', N'Rua da Sofia', N'Baixa', N'Coimbra', N'Coimbra', N'3000-395', N'90', N'4.', 40.210000, -8.429000, N'Tecnica de Suporte', N'Equipa Centro', 0),
        (N'EMP-007', N'Ana Oliveira', N'211000107', N'916000107', 1, N'ana.oliveira@gerit.pt', N'ACTIVE', N'230111007', N'RESIDENTIAL', N'Avenida da Liberdade', N'Santo Antonio', N'Lisboa', N'Lisboa', N'1250-096', N'45', N'2. Dt.', 38.720000, -9.142000, N'Tecnica Senior', N'Equipa Sul', 1),
        (N'EMP-008', N'Pedro Santos', N'211000108', N'916000108', 1, N'pedro.santos@gerit.pt', N'ACTIVE', N'230111008', N'RESIDENTIAL', N'Avenida da Republica', N'Avenidas Novas', N'Lisboa', N'Lisboa', N'1000-190', N'88', N'5. Esq.', 38.739000, -9.147000, N'Tecnico de Manutencao', N'Equipa Sul', 0),
        (N'EMP-009', N'Maria Silva', N'265000109', N'916000109', 1, N'maria.silva@gerit.pt', N'ACTIVE', N'230111009', N'RESIDENTIAL', N'Avenida Luisa Todi', N'Centro', N'Setubal', N'Setubal', N'2900-452', N'55', N'3.', 38.524000, -8.893000, N'Tecnica de Inspecao', N'Equipa Centro', 0);

    INSERT INTO dbo.Employees (TenantId, StatusDefinitionId, StatusDomainId, Name, PhoneNumber, CellPhoneNumber, IsCellPhoneWhatsapp, Email, ImageUrl, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, sd.Id, d.Id, s.Name, s.PhoneNumber, s.CellPhoneNumber, s.IsWhatsapp, s.Email, NULL, 1, 0, @SeedActorId, @UtcNow
    FROM @EmployeeSeed s
    INNER JOIN dbo.StatusDomains d ON d.Code = N'EMPLOYEE' AND d.IsDeleted = 0
    INNER JOIN dbo.StatusDefinitions sd ON sd.TenantId = @DemoTenantId AND sd.StatusDomainId = d.Id AND sd.Code = s.StatusCode AND sd.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Employees e WHERE e.TenantId = @DemoTenantId AND e.Email = s.Email AND e.IsDeleted = 0);

    /* =========================================================
       28. EMPLOYEE FISCAL DATA
       ========================================================= */

    INSERT INTO dbo.EmployeeFiscalData (TenantId, EmployeeId, TaxNumber, VatNumber, FiscalCountry, IsVatRegistered, IBAN, FiscalEmail, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, e.Id, s.TaxNumber, NULL, 'PT', 0,
           CONCAT(N'PT5000330000', RIGHT(CONCAT(N'0000000000', s.TaxNumber), 11)),
           s.Email, 1, 0, @SeedActorId, @UtcNow
    FROM @EmployeeSeed s
    INNER JOIN dbo.Employees e ON e.TenantId = @DemoTenantId AND e.Email = s.Email AND e.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.EmployeeFiscalData fd WHERE fd.TenantId = @DemoTenantId AND fd.EmployeeId = e.Id AND fd.IsActive = 1 AND fd.IsDeleted = 0);

    /* =========================================================
       29. EMPLOYEE CONTACT PERSONS
       ========================================================= */

    INSERT INTO dbo.EmployeeContactPersons (TenantId, EmployeeId, JobTitle, Department, Name, PhoneNumber, CellPhoneNumber, IsCellPhoneWhatsapp, Email, IsPrimary, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, e.Id, s.ContactJobTitle, NULL, s.Name, s.PhoneNumber, s.CellPhoneNumber, s.IsWhatsapp, s.Email, 1, 1, 0, @SeedActorId, @UtcNow
    FROM @EmployeeSeed s
    INNER JOIN dbo.Employees e ON e.TenantId = @DemoTenantId AND e.Email = s.Email AND e.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.EmployeeContactPersons cp WHERE cp.TenantId = @DemoTenantId AND cp.EmployeeId = e.Id AND cp.Email = s.Email AND cp.IsDeleted = 0);

    /* =========================================================
       30. EMPLOYEE ADDRESSES
       ========================================================= */

    INSERT INTO dbo.EmployeeAddresses (TenantId, EmployeeId, AddressTypeId, CountryCode, Street, Neighborhood, City, District, PostalCode, StreetNumber, Complement, Latitude, Longitude, Note, IsPrimary, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, e.Id, atp.Id, 'PT',
           s.Street, s.Neighborhood, s.City, s.District, s.PostalCode,
           s.StreetNumber, s.Complement, s.Latitude, s.Longitude,
           N'Morada principal do colaborador.', 1, 1, 0, @SeedActorId, @UtcNow
    FROM @EmployeeSeed s
    INNER JOIN dbo.Employees e ON e.TenantId = @DemoTenantId AND e.Email = s.Email AND e.IsDeleted = 0
    INNER JOIN dbo.AddressTypes atp ON atp.Code = s.AddressTypeCode AND atp.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.EmployeeAddresses a WHERE a.TenantId = @DemoTenantId AND a.EmployeeId = e.Id AND a.IsPrimary = 1 AND a.IsDeleted = 0);

    /* =========================================================
       31. EMPLOYEE TEAM (ALOCACOES)
       ========================================================= */

    INSERT INTO dbo.EmployeeTeam (TenantId, TeamId, EmployeeId, IsLeader, StartDateTime, EndDateTime, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, t.Id, e.Id, s.IsLeader,
           DATETIME2FROMPARTS(2025, 9, 1, 8, 0, 0, 0, 0), NULL,
           1, 0, @SeedActorId, @UtcNow
    FROM @EmployeeSeed s
    INNER JOIN dbo.Employees e ON e.TenantId = @DemoTenantId AND e.Email = s.Email AND e.IsDeleted = 0
    INNER JOIN dbo.Teams t ON t.TenantId = @DemoTenantId AND t.Name = s.TeamName AND t.IsDeleted = 0
    WHERE s.TeamName IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.EmployeeTeam et WHERE et.TenantId = @DemoTenantId AND et.TeamId = t.Id AND et.EmployeeId = e.Id AND et.EndDateTime IS NULL AND et.IsDeleted = 0);

    /* =========================================================
       32. EQUIPMENT TYPES (6)
       ========================================================= */

    DECLARE @EquipmentTypeSeed TABLE (Name NVARCHAR(200) NOT NULL, Description NVARCHAR(500) NOT NULL);
    INSERT INTO @EquipmentTypeSeed (Name, Description)
    VALUES
        (N'Multimetro Digital', N'Multimetro digital portatil para medicoes eletricas.'),
        (N'Camara Termica', N'Camara de imagem termica para inspecao de equipamentos.'),
        (N'Kit de Ferramentas', N'Conjunto completo de ferramentas manuais para manutencao.'),
        (N'Analisador de Redes', N'Analisador de qualidade de energia e redes eletricas.'),
        (N'Bomba de Vacuo', N'Bomba de vacuo para testes e manutencao de sistemas.'),
        (N'Detetor de Gases', N'Detetor portatil de gases para inspecoes de seguranca.');

    INSERT INTO dbo.EquipmentTypes (TenantId, Name, Description, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, s.Name, s.Description, 1, 0, @SeedActorId, @UtcNow FROM @EquipmentTypeSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.EquipmentTypes et WHERE et.TenantId = @DemoTenantId AND et.Name = s.Name AND et.IsDeleted = 0);

    /* =========================================================
       33. EQUIPMENTS (12)
       ========================================================= */

    DECLARE @EquipmentSeed TABLE (
        SeedKey NVARCHAR(20) NOT NULL, EquipmentTypeName NVARCHAR(200) NOT NULL,
        Name NVARCHAR(150) NOT NULL, SerialNumber NVARCHAR(100) NULL, StatusCode NVARCHAR(50) NOT NULL
    );
    INSERT INTO @EquipmentSeed (SeedKey, EquipmentTypeName, Name, SerialNumber, StatusCode)
    VALUES
        (N'EQP-001', N'Multimetro Digital', N'Multimetro Fluke 179', N'FLK-179-00123', N'AVAILABLE'),
        (N'EQP-002', N'Multimetro Digital', N'Multimetro Fluke 179', N'FLK-179-00124', N'IN_USE'),
        (N'EQP-003', N'Camara Termica', N'Camara Termica FLIR E8', N'FLIR-E8-00456', N'AVAILABLE'),
        (N'EQP-004', N'Camara Termica', N'Camara Termica FLIR E8', N'FLIR-E8-00457', N'IN_USE'),
        (N'EQP-005', N'Kit de Ferramentas', N'Kit Ferramentas Bosch', N'BOSCH-KIT-00789', N'AVAILABLE'),
        (N'EQP-006', N'Kit de Ferramentas', N'Kit Ferramentas Bosch', N'BOSCH-KIT-00790', N'IN_USE'),
        (N'EQP-007', N'Analisador de Redes', N'Analisador Fluke 435-II', N'FLK-435-01011', N'AVAILABLE'),
        (N'EQP-008', N'Analisador de Redes', N'Analisador Fluke 435-II', N'FLK-435-01012', N'IN_USE'),
        (N'EQP-009', N'Bomba de Vacuo', N'Bomba Vacuo Robinair', N'ROB-VAC-01213', N'AVAILABLE'),
        (N'EQP-010', N'Bomba de Vacuo', N'Bomba Vacuo Robinair', N'ROB-VAC-01214', N'MAINTENANCE'),
        (N'EQP-011', N'Detetor de Gases', N'Detetor Gases Honeywell', N'HON-GAS-01415', N'AVAILABLE'),
        (N'EQP-012', N'Detetor de Gases', N'Detetor Gases Honeywell', N'HON-GAS-01416', N'AVAILABLE');

    INSERT INTO dbo.Equipments (TenantId, EquipmentTypeId, StatusDefinitionId, StatusDomainId, Name, SerialNumber, UrlImage, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, et.Id, sd.Id, d.Id, s.Name, s.SerialNumber, NULL, 1, 0, @SeedActorId, @UtcNow
    FROM @EquipmentSeed s
    INNER JOIN dbo.EquipmentTypes et ON et.TenantId = @DemoTenantId AND et.Name = s.EquipmentTypeName AND et.IsDeleted = 0
    INNER JOIN dbo.StatusDomains d ON d.Code = N'EQUIPMENT' AND d.IsDeleted = 0
    INNER JOIN dbo.StatusDefinitions sd ON sd.TenantId = @DemoTenantId AND sd.StatusDomainId = d.Id AND sd.Code = s.StatusCode AND sd.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Equipments eq WHERE eq.TenantId = @DemoTenantId AND eq.SerialNumber = s.SerialNumber AND eq.IsDeleted = 0);

    /* =========================================================
       34. VEHICLES (6)
       ========================================================= */

    DECLARE @VehicleSeed TABLE (
        Plate NVARCHAR(20) NOT NULL, Brand NVARCHAR(100) NOT NULL, Model NVARCHAR(100) NOT NULL, Year INT NOT NULL,
        Color NVARCHAR(50) NULL, FuelType NVARCHAR(50) NULL, StatusCode NVARCHAR(50) NOT NULL
    );
    INSERT INTO @VehicleSeed (Plate, Brand, Model, Year, Color, FuelType, StatusCode)
    VALUES
        (N'AA-01-GT', N'Renault', N'Kangoo', 2022, N'Branco', N'Diesel', N'IN_USE'),
        (N'BB-02-GT', N'Renault', N'Kangoo', 2023, N'Branco', N'Diesel', N'AVAILABLE'),
        (N'CC-03-GT', N'Peugeot', N'Partner', 2023, N'Cinzento', N'Diesel', N'IN_USE'),
        (N'DD-04-GT', N'Toyota', N'ProAce City', 2024, N'Azul', N'Eletrico', N'AVAILABLE'),
        (N'EE-05-GT', N'Citroen', N'Berlingo', 2022, N'Vermelho', N'Diesel', N'MAINTENANCE'),
        (N'FF-06-GT', N'Ford', N'Transit Custom', 2024, N'Prata', N'Hibrido', N'AVAILABLE');

    INSERT INTO dbo.Vehicles (TenantId, StatusDefinitionId, StatusDomainId, Plate, Brand, Model, Year, Color, FuelType, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, sd.Id, d.Id, s.Plate, s.Brand, s.Model, s.Year, s.Color, s.FuelType, 1, 0, @SeedActorId, @UtcNow
    FROM @VehicleSeed s
    INNER JOIN dbo.StatusDomains d ON d.Code = N'VEHICLE' AND d.IsDeleted = 0
    INNER JOIN dbo.StatusDefinitions sd ON sd.TenantId = @DemoTenantId AND sd.StatusDomainId = d.Id AND sd.Code = s.StatusCode AND sd.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Vehicles v WHERE v.TenantId = @DemoTenantId AND v.Plate = s.Plate AND v.IsDeleted = 0);

    /* =========================================================
       35. VISIT TEAM FUNCTIONS (6)
       ========================================================= */

    DECLARE @VisitTeamFunctionSeed TABLE (Name NVARCHAR(150) NOT NULL, Description NVARCHAR(500) NOT NULL);
    INSERT INTO @VisitTeamFunctionSeed (Name, Description)
    VALUES
        (N'Tecnico Responsavel', N'Responsavel tecnico pela execucao da visita.'),
        (N'Tecnico Auxiliar', N'Tecnico de apoio ao responsavel na execucao da visita.'),
        (N'Inspetor', N'Responsavel pela inspecao e diagnostico durante a visita.'),
        (N'Consultor', N'Consultor tecnico para analise e recomendacoes.'),
        (N'Operador de Equipamento', N'Operador especializado de equipamentos especificos.'),
        (N'Motorista', N'Condutor do veiculo de servico durante a visita.');

    INSERT INTO dbo.VisitTeamFunctions (TenantId, Name, Description, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, s.Name, s.Description, 1, 0, @SeedActorId, @UtcNow FROM @VisitTeamFunctionSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.VisitTeamFunctions vtf WHERE vtf.TenantId = @DemoTenantId AND vtf.Name = s.Name AND vtf.IsDeleted = 0);

    /* =========================================================
       36. VISITS (20 - DISTRIBUIDAS JANEIRO-MARCO 2026)
       ========================================================= */

    DECLARE @VisitSeed TABLE (
        SeedKey NVARCHAR(20) NOT NULL, ClientSeedKey NVARCHAR(20) NOT NULL, StatusCode NVARCHAR(50) NOT NULL,
        Title NVARCHAR(200) NOT NULL, Description NVARCHAR(2000) NOT NULL,
        StartDateTime DATETIME2(7) NOT NULL, EndDateTime DATETIME2(7) NULL,
        EstimatedValue DECIMAL(19,4) NOT NULL, RealValue DECIMAL(19,4) NULL
    );
    INSERT INTO @VisitSeed (SeedKey, ClientSeedKey, StatusCode, Title, Description, StartDateTime, EndDateTime, EstimatedValue, RealValue)
    VALUES
        (N'VIS-001', N'CLI-008', N'COMPLETED', N'Manutencao Preventiva - Sistema Eletrico', N'Manutencao preventiva trimestral ao sistema eletrico da oficina Silva & Filhos.', DATETIME2FROMPARTS(2026, 1, 7, 9, 0, 0, 0, 0), DATETIME2FROMPARTS(2026, 1, 7, 13, 0, 0, 0, 0), 350.00, 350.00),
        (N'VIS-002', N'CLI-009', N'COMPLETED', N'Instalacao de Servidores', N'Instalacao e configuracao de dois servidores Dell PowerEdge para a Tech Solutions.', DATETIME2FROMPARTS(2026, 1, 12, 8, 0, 0, 0, 0), DATETIME2FROMPARTS(2026, 1, 12, 17, 0, 0, 0, 0), 1200.00, 1250.00),
        (N'VIS-003', N'CLI-001', N'COMPLETED', N'Avaliacao Tecnica Residencial', N'Avaliacao tecnica para instalacao de paineis solares na residencia de Maria Joao Ferreira.', DATETIME2FROMPARTS(2026, 1, 15, 10, 0, 0, 0, 0), DATETIME2FROMPARTS(2026, 1, 15, 12, 0, 0, 0, 0), 150.00, 150.00),
        (N'VIS-004', N'CLI-010', N'COMPLETED_PENDING', N'Inspecao de Equipamentos Medicos', N'Inspecao periodica aos equipamentos de frio da Farmacia Central. Pendente substituicao de sensor.', DATETIME2FROMPARTS(2026, 1, 19, 9, 0, 0, 0, 0), DATETIME2FROMPARTS(2026, 1, 19, 14, 0, 0, 0, 0), 480.00, 480.00),
        (N'VIS-005', N'CLI-012', N'COMPLETED', N'Manutencao de Equipamento AV', N'Manutencao corretiva ao sistema de videowall da Porto Creative Agency.', DATETIME2FROMPARTS(2026, 1, 22, 9, 0, 0, 0, 0), DATETIME2FROMPARTS(2026, 1, 22, 16, 0, 0, 0, 0), 750.00, 720.00),
        (N'VIS-006', N'CLI-003', N'COMPLETED', N'Reparacao de Equipamento Domestico', N'Reparacao de sistema de climatizacao na residencia de Ana Oliveira em Braga.', DATETIME2FROMPARTS(2026, 1, 26, 14, 0, 0, 0, 0), DATETIME2FROMPARTS(2026, 1, 26, 17, 0, 0, 0, 0), 220.00, 220.00),
        (N'VIS-007', N'CLI-013', N'COMPLETED', N'Consultoria de Infraestrutura TI', N'Consultoria tecnica para upgrade da infraestrutura de rede da Lisboa Digital Services.', DATETIME2FROMPARTS(2026, 1, 28, 9, 0, 0, 0, 0), DATETIME2FROMPARTS(2026, 1, 28, 13, 0, 0, 0, 0), 650.00, 650.00),
        (N'VIS-008', N'CLI-002', N'COMPLETED', N'Instalacao Domestica', N'Instalacao de sistema de seguranca na residencia de Pedro Santos em Lisboa.', DATETIME2FROMPARTS(2026, 2, 3, 8, 0, 0, 0, 0), DATETIME2FROMPARTS(2026, 2, 3, 16, 0, 0, 0, 0), 890.00, 890.00),
        (N'VIS-009', N'CLI-011', N'COMPLETED', N'Inspecao de Cozinha Industrial', N'Inspecao de seguranca alimentar e equipamentos da cozinha do Restaurante O Forno.', DATETIME2FROMPARTS(2026, 2, 5, 7, 0, 0, 0, 0), DATETIME2FROMPARTS(2026, 2, 5, 11, 0, 0, 0, 0), 380.00, 400.00),
        (N'VIS-010', N'CLI-007', N'COMPLETED', N'Manutencao Residencial', N'Manutencao geral e verificacao eletrica na residencia de Patricia Mendes.', DATETIME2FROMPARTS(2026, 2, 10, 14, 0, 0, 0, 0), DATETIME2FROMPARTS(2026, 2, 10, 17, 0, 0, 0, 0), 180.00, 180.00),
        (N'VIS-011', N'CLI-014', N'COMPLETED', N'Consultoria de Processos', N'Analise e otimizacao de processos operacionais na Coimbra Business Consulting.', DATETIME2FROMPARTS(2026, 2, 12, 9, 0, 0, 0, 0), DATETIME2FROMPARTS(2026, 2, 12, 12, 0, 0, 0, 0), 550.00, 550.00),
        (N'VIS-012', N'CLI-004', N'COMPLETED_PENDING', N'Reparacao de Equipamento', N'Reparacao de sistema AVAC na residencia de Rui Goncalves. Aguarda peca de substituicao.', DATETIME2FROMPARTS(2026, 2, 17, 9, 0, 0, 0, 0), DATETIME2FROMPARTS(2026, 2, 17, 11, 30, 0, 0, 0), 310.00, 150.00),
        (N'VIS-013', N'CLI-008', N'COMPLETED', N'Manutencao Preventiva - Geradores', N'Manutencao preventiva aos geradores de emergencia da Silva & Filhos.', DATETIME2FROMPARTS(2026, 2, 24, 8, 0, 0, 0, 0), DATETIME2FROMPARTS(2026, 2, 24, 14, 0, 0, 0, 0), 520.00, 520.00),
        (N'VIS-014', N'CLI-009', N'COMPLETED', N'Manutencao Corretiva - Rede', N'Intervencao urgente para resolucao de falha na rede local da Tech Solutions.', DATETIME2FROMPARTS(2026, 3, 2, 10, 0, 0, 0, 0), DATETIME2FROMPARTS(2026, 3, 2, 15, 0, 0, 0, 0), 780.00, 800.00),
        (N'VIS-015', N'CLI-005', N'COMPLETED', N'Inspecao Residencial', N'Inspecao de instalacao eletrica na residencia de Carla Martins em Faro.', DATETIME2FROMPARTS(2026, 3, 5, 9, 0, 0, 0, 0), DATETIME2FROMPARTS(2026, 3, 5, 11, 0, 0, 0, 0), 200.00, 200.00),
        (N'VIS-016', N'CLI-013', N'IN_PROGRESS', N'Upgrade de Infraestrutura', N'Upgrade faseado da infraestrutura de servidores da Lisboa Digital Services - Fase 2 de 3.', DATETIME2FROMPARTS(2026, 3, 9, 9, 0, 0, 0, 0), NULL, 2500.00, NULL),
        (N'VIS-017', N'CLI-006', N'CONFIRMED', N'Manutencao Preventiva', N'Manutencao preventiva agendada para sistema de climatizacao na residencia de Manuel Rodrigues.', DATETIME2FROMPARTS(2026, 3, 16, 10, 0, 0, 0, 0), NULL, 180.00, NULL),
        (N'VIS-018', N'CLI-012', N'CANCELED', N'Instalacao de Equipamento', N'Instalacao de equipamento de producao audiovisual na Porto Creative Agency. Cancelado pelo cliente.', DATETIME2FROMPARTS(2026, 3, 19, 9, 0, 0, 0, 0), NULL, 950.00, 0.00),
        (N'VIS-019', N'CLI-010', N'SCHEDULED', N'Inspecao Semestral', N'Inspecao semestral aos equipamentos de climatizacao da Farmacia Central.', DATETIME2FROMPARTS(2026, 3, 25, 9, 0, 0, 0, 0), NULL, 380.00, NULL),
        (N'VIS-020', N'CLI-001', N'SCHEDULED', N'Instalacao de Paineis Solares', N'Instalacao completa de sistema de paineis solares na residencia de Maria Joao Ferreira.', DATETIME2FROMPARTS(2026, 3, 30, 8, 0, 0, 0, 0), NULL, 3500.00, NULL);

    INSERT INTO dbo.Visits (TenantId, ClientId, StatusDefinitionId, StatusDomainId, Title, Description, CurrencyCode, StartDateTime, EndDateTime, EstimatedValue, RealValue, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, c.Id, sd.Id, d.Id, s.Title, s.Description,
           N'EUR', s.StartDateTime, s.EndDateTime, s.EstimatedValue, s.RealValue,
           1, 0, @SeedActorId, @UtcNow
    FROM @VisitSeed s
    INNER JOIN @ClientSeed cs ON cs.SeedKey = s.ClientSeedKey
    INNER JOIN dbo.Clients c ON c.TenantId = @DemoTenantId AND c.Note = cs.Note AND c.IsDeleted = 0
    INNER JOIN dbo.StatusDomains d ON d.Code = N'VISIT' AND d.IsDeleted = 0
    INNER JOIN dbo.StatusDefinitions sd ON sd.TenantId = @DemoTenantId AND sd.StatusDomainId = d.Id AND sd.Code = s.StatusCode AND sd.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Visits v WHERE v.TenantId = @DemoTenantId AND v.ClientId = c.Id AND v.Title = s.Title AND v.StartDateTime = s.StartDateTime AND v.IsDeleted = 0);

    /* =========================================================
       37. VISIT CONTACT PERSONS
       ========================================================= */

    INSERT INTO dbo.VisitContactPersons (TenantId, VisitId, JobTitle, Department, Name, PhoneNumber, CellPhoneNumber, IsCellPhoneWhatsapp, Email, IsPrimary, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, v.Id, cs.ContactJobTitle, NULL, cs.ContactName,
           cs.PhoneNumber, cs.CellPhoneNumber, cs.IsWhatsapp, cs.Email,
           1, 1, 0, @SeedActorId, @UtcNow
    FROM @VisitSeed s
    INNER JOIN @ClientSeed cs ON cs.SeedKey = s.ClientSeedKey
    INNER JOIN dbo.Clients c ON c.TenantId = @DemoTenantId AND c.Note = cs.Note AND c.IsDeleted = 0
    INNER JOIN dbo.Visits v ON v.TenantId = @DemoTenantId AND v.ClientId = c.Id AND v.Title = s.Title AND v.StartDateTime = s.StartDateTime AND v.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.VisitContactPersons vcp WHERE vcp.TenantId = @DemoTenantId AND vcp.VisitId = v.Id AND vcp.IsPrimary = 1 AND vcp.IsDeleted = 0);

    /* =========================================================
       38. VISIT ADDRESSES
       ========================================================= */

    INSERT INTO dbo.VisitAddresses (TenantId, VisitId, AddressTypeId, CountryCode, Street, Neighborhood, City, District, PostalCode, StreetNumber, Complement, Latitude, Longitude, Note, IsPrimary, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, v.Id, atp.Id, 'PT',
           cs.Street, cs.Neighborhood, cs.City, cs.District, cs.PostalCode,
           cs.StreetNumber, cs.Complement, cs.Latitude, cs.Longitude,
           N'Morada do local da visita.', 1, 1, 0, @SeedActorId, @UtcNow
    FROM @VisitSeed s
    INNER JOIN @ClientSeed cs ON cs.SeedKey = s.ClientSeedKey
    INNER JOIN dbo.Clients c ON c.TenantId = @DemoTenantId AND c.Note = cs.Note AND c.IsDeleted = 0
    INNER JOIN dbo.Visits v ON v.TenantId = @DemoTenantId AND v.ClientId = c.Id AND v.Title = s.Title AND v.StartDateTime = s.StartDateTime AND v.IsDeleted = 0
    INNER JOIN dbo.AddressTypes atp ON atp.Code = cs.AddressTypeCode AND atp.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.VisitAddresses va WHERE va.TenantId = @DemoTenantId AND va.VisitId = v.Id AND va.IsPrimary = 1 AND va.IsDeleted = 0);

    /* =========================================================
       39. VISIT TEAM ASSIGNMENTS
       ========================================================= */

    -- Mapeamento: cada visita -> qual equipa regional
    DECLARE @VisitTeamMapping TABLE (VisitSeedKey NVARCHAR(20) NOT NULL, TeamName NVARCHAR(150) NOT NULL);
    INSERT INTO @VisitTeamMapping (VisitSeedKey, TeamName)
    VALUES
        (N'VIS-001', N'Equipa Norte'), (N'VIS-004', N'Equipa Norte'), (N'VIS-005', N'Equipa Norte'),
        (N'VIS-006', N'Equipa Norte'), (N'VIS-009', N'Equipa Norte'), (N'VIS-013', N'Equipa Norte'),
        (N'VIS-018', N'Equipa Norte'), (N'VIS-019', N'Equipa Norte'), (N'VIS-020', N'Equipa Norte'),
        (N'VIS-002', N'Equipa Sul'), (N'VIS-007', N'Equipa Sul'), (N'VIS-008', N'Equipa Sul'),
        (N'VIS-010', N'Equipa Sul'), (N'VIS-014', N'Equipa Sul'), (N'VIS-015', N'Equipa Sul'), (N'VIS-016', N'Equipa Sul'),
        (N'VIS-003', N'Equipa Centro'), (N'VIS-011', N'Equipa Centro'), (N'VIS-012', N'Equipa Centro'), (N'VIS-017', N'Equipa Centro');

    -- Criar VisitTeam para cada visita
    INSERT INTO dbo.VisitTeam (TenantId, VisitId, TeamId, StartDateTime, EndDateTime, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, v.Id, t.Id, v.StartDateTime, v.EndDateTime, 1, 0, @SeedActorId, @UtcNow
    FROM @VisitTeamMapping vtm
    INNER JOIN @VisitSeed s ON s.SeedKey = vtm.VisitSeedKey
    INNER JOIN @ClientSeed cs ON cs.SeedKey = s.ClientSeedKey
    INNER JOIN dbo.Clients c ON c.TenantId = @DemoTenantId AND c.Note = cs.Note AND c.IsDeleted = 0
    INNER JOIN dbo.Visits v ON v.TenantId = @DemoTenantId AND v.ClientId = c.Id AND v.Title = s.Title AND v.StartDateTime = s.StartDateTime AND v.IsDeleted = 0
    INNER JOIN dbo.Teams t ON t.TenantId = @DemoTenantId AND t.Name = vtm.TeamName AND t.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.VisitTeam vt WHERE vt.TenantId = @DemoTenantId AND vt.VisitId = v.Id AND vt.TeamId = t.Id AND vt.IsDeleted = 0);

    /* =========================================================
       40. VISIT TEAM EMPLOYEES
       ========================================================= */

    -- Atribuir colaboradores a cada visita com funcoes
    INSERT INTO dbo.VisitTeamEmployee (TenantId, VisitTeamId, EmployeeId, VisitTeamFunctionId, IsLeader, StartDateTime, EndDateTime, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, vt.Id, e.Id, vtf.Id,
           CASE WHEN et.IsLeader = 1 THEN 1 ELSE 0 END,
           v.StartDateTime, v.EndDateTime, 1, 0, @SeedActorId, @UtcNow
    FROM dbo.VisitTeam vt
    INNER JOIN dbo.Visits v ON v.Id = vt.VisitId AND v.TenantId = vt.TenantId AND v.IsDeleted = 0
    INNER JOIN dbo.Teams t ON t.Id = vt.TeamId AND t.TenantId = vt.TenantId
    INNER JOIN dbo.EmployeeTeam et ON et.TeamId = t.Id AND et.TenantId = t.TenantId AND et.EndDateTime IS NULL AND et.IsDeleted = 0
    INNER JOIN dbo.Employees e ON e.Id = et.EmployeeId AND e.TenantId = et.TenantId AND e.IsDeleted = 0
    INNER JOIN dbo.VisitTeamFunctions vtf ON vtf.TenantId = @DemoTenantId AND vtf.IsDeleted = 0
    WHERE vt.TenantId = @DemoTenantId AND vt.IsDeleted = 0
      AND ((et.IsLeader = 1 AND vtf.Name = N'Tecnico Responsavel') OR (et.IsLeader = 0 AND vtf.Name = N'Tecnico Auxiliar'))
      AND NOT EXISTS (SELECT 1 FROM dbo.VisitTeamEmployee vte WHERE vte.TenantId = @DemoTenantId AND vte.VisitTeamId = vt.Id AND vte.EmployeeId = e.Id AND vte.EndDateTime IS NULL AND vte.IsDeleted = 0);

    /* =========================================================
       41. VISIT TEAM VEHICLES
       ========================================================= */

    -- Atribuir um veiculo disponivel aleatoriamente a cada equipa de visita
    INSERT INTO dbo.VisitTeamVehicle (TenantId, VisitTeamId, VehicleId, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT @DemoTenantId, vt.Id, veh.Id, 1, 0, @SeedActorId, @UtcNow
    FROM dbo.VisitTeam vt
    INNER JOIN dbo.Teams t ON t.Id = vt.TeamId AND t.TenantId = vt.TenantId
    CROSS APPLY (
        SELECT TOP 1 vh.Id FROM dbo.Vehicles vh
        WHERE vh.TenantId = @DemoTenantId AND vh.IsDeleted = 0
        ORDER BY NEWID()
    ) veh
    WHERE vt.TenantId = @DemoTenantId AND vt.IsDeleted = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.VisitTeamVehicle vtv WHERE vtv.TenantId = @DemoTenantId AND vtv.VisitTeamId = vt.Id AND vtv.VehicleId = veh.Id AND vtv.IsDeleted = 0);

    /* =========================================================
       42. VISIT TEAM EQUIPMENT
       ========================================================= */

    -- Atribuir 2 equipamentos aleatorios a cada equipa de visita
    INSERT INTO dbo.VisitTeamEquipment (TenantId, VisitTeamId, EquipmentId, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT TOP 40 @DemoTenantId, vt.Id, eq.Id, 1, 0, @SeedActorId, @UtcNow
    FROM dbo.VisitTeam vt
    INNER JOIN dbo.Equipments eq ON eq.TenantId = @DemoTenantId AND eq.IsDeleted = 0
    WHERE vt.TenantId = @DemoTenantId AND vt.IsDeleted = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.VisitTeamEquipment vte WHERE vte.TenantId = @DemoTenantId AND vte.VisitTeamId = vt.Id AND vte.EquipmentId = eq.Id AND vte.IsDeleted = 0)
    ORDER BY NEWID();

    /* =========================================================
       43. TRADUCOES MULTI-IDIOMA
       ========================================================= */

    DECLARE @SupportedLanguages TABLE (LanguageCode NVARCHAR(10) PRIMARY KEY, IsDefault BIT NOT NULL);
    INSERT INTO @SupportedLanguages (LanguageCode, IsDefault) VALUES (N'pt-PT', 1), (N'pt-BR', 0), (N'en-US', 0), (N'es-ES', 0);

    -- PartyTypeTranslations para 4 idiomas
    DECLARE @PartyTypeTranslationSeed4 TABLE (PartyTypeId TINYINT NOT NULL, LanguageCode NVARCHAR(10) NOT NULL, Name NVARCHAR(100) NOT NULL, Description NVARCHAR(300) NULL, PRIMARY KEY (PartyTypeId, LanguageCode));
    INSERT INTO @PartyTypeTranslationSeed4 (PartyTypeId, LanguageCode, Name, Description)
    VALUES
        (1, N'pt-PT', N'Pessoa singular', N'Representa uma pessoa singular.'), (1, N'pt-BR', N'Pessoa fisica', N'Representa uma pessoa fisica.'),
        (1, N'en-US', N'Individual', N'Represents a natural person.'), (1, N'es-ES', N'Persona fisica', N'Representa a una persona fisica.'),
        (2, N'pt-PT', N'Organizacao', N'Representa uma empresa, associacao ou outra pessoa coletiva.'), (2, N'pt-BR', N'Organizacao', N'Representa uma empresa, associacao ou outra pessoa juridica.'),
        (2, N'en-US', N'Organization', N'Represents a company, association, or other legal entity.'), (2, N'es-ES', N'Organizacion', N'Representa una empresa, asociacion u otra persona juridica.');

    UPDATE tr SET tr.Name = s.Name, tr.Description = s.Description
    FROM dbo.PartyTypeTranslations tr INNER JOIN @PartyTypeTranslationSeed4 s ON s.PartyTypeId = tr.PartyTypeId AND s.LanguageCode = tr.LanguageCode;

    INSERT INTO dbo.PartyTypeTranslations (PartyTypeId, LanguageCode, Name, Description)
    SELECT s.PartyTypeId, s.LanguageCode, s.Name, s.Description
    FROM @PartyTypeTranslationSeed4 s INNER JOIN dbo.PartyTypes p ON p.Id = s.PartyTypeId AND p.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PartyTypeTranslations tr WHERE tr.PartyTypeId = s.PartyTypeId AND tr.LanguageCode = s.LanguageCode);

    -- AcquisitionSourceTypeTranslations 4 idiomas
    DECLARE @AcqSrcTr4 TABLE (Code NVARCHAR(50) NOT NULL, LanguageCode NVARCHAR(10) NOT NULL, Name NVARCHAR(100) NOT NULL, Description NVARCHAR(300) NULL, PRIMARY KEY (Code, LanguageCode));
    INSERT INTO @AcqSrcTr4 (Code, LanguageCode, Name, Description)
    VALUES
        (N'OTHER', N'pt-PT', N'Outros', N'Origem nao especificada.'), (N'OTHER', N'pt-BR', N'Outros', N'Origem nao especificada.'),
        (N'OTHER', N'en-US', N'Other', N'Unspecified source.'), (N'OTHER', N'es-ES', N'Otros', N'Origen no especificado.'),
        (N'INSTAGRAM', N'pt-PT', N'Instagram', N'Cliente originado pelo Instagram.'), (N'INSTAGRAM', N'pt-BR', N'Instagram', N'Cliente originado pelo Instagram.'),
        (N'INSTAGRAM', N'en-US', N'Instagram', N'Client acquired through Instagram.'), (N'INSTAGRAM', N'es-ES', N'Instagram', N'Cliente adquirido via Instagram.'),
        (N'FACEBOOK', N'pt-PT', N'Facebook', N'Cliente originado pelo Facebook.'), (N'FACEBOOK', N'pt-BR', N'Facebook', N'Cliente originado pelo Facebook.'),
        (N'FACEBOOK', N'en-US', N'Facebook', N'Client acquired through Facebook.'), (N'FACEBOOK', N'es-ES', N'Facebook', N'Cliente adquirido via Facebook.'),
        (N'LINKEDIN', N'pt-PT', N'LinkedIn', N'Cliente originado pelo LinkedIn.'), (N'LINKEDIN', N'pt-BR', N'LinkedIn', N'Cliente originado pelo LinkedIn.'),
        (N'LINKEDIN', N'en-US', N'LinkedIn', N'Client acquired through LinkedIn.'), (N'LINKEDIN', N'es-ES', N'LinkedIn', N'Cliente adquirido via LinkedIn.'),
        (N'YOUTUBE', N'pt-PT', N'YouTube', N'Cliente originado pelo YouTube.'), (N'YOUTUBE', N'pt-BR', N'YouTube', N'Cliente originado pelo YouTube.'),
        (N'YOUTUBE', N'en-US', N'YouTube', N'Client acquired through YouTube.'), (N'YOUTUBE', N'es-ES', N'YouTube', N'Cliente adquirido via YouTube.'),
        (N'WHATSAPP', N'pt-PT', N'WhatsApp', N'Cliente originado pelo WhatsApp.'), (N'WHATSAPP', N'pt-BR', N'WhatsApp', N'Cliente originado pelo WhatsApp.'),
        (N'WHATSAPP', N'en-US', N'WhatsApp', N'Client acquired through WhatsApp.'), (N'WHATSAPP', N'es-ES', N'WhatsApp', N'Cliente adquirido via WhatsApp.'),
        (N'TIKTOK', N'pt-PT', N'TikTok', N'Cliente originado pelo TikTok.'), (N'TIKTOK', N'pt-BR', N'TikTok', N'Cliente originado pelo TikTok.'),
        (N'TIKTOK', N'en-US', N'TikTok', N'Client acquired through TikTok.'), (N'TIKTOK', N'es-ES', N'TikTok', N'Cliente adquirido via TikTok.'),
        (N'GOOGLE', N'pt-PT', N'Google', N'Cliente originado pelo Google.'), (N'GOOGLE', N'pt-BR', N'Google', N'Cliente originado pelo Google.'),
        (N'GOOGLE', N'en-US', N'Google', N'Client acquired through Google.'), (N'GOOGLE', N'es-ES', N'Google', N'Cliente adquirido via Google.'),
        (N'REFERRAL', N'pt-PT', N'Indicacao', N'Cliente originado por indicacao.'), (N'REFERRAL', N'pt-BR', N'Indicacao', N'Cliente originado por indicacao.'),
        (N'REFERRAL', N'en-US', N'Referral', N'Client acquired through referral.'), (N'REFERRAL', N'es-ES', N'Recomendacion', N'Cliente adquirido por recomendacion.'),
        (N'TV', N'pt-PT', N'Televisao', N'Cliente originado pela TV.'), (N'TV', N'pt-BR', N'Televisao', N'Cliente originado pela TV.'),
        (N'TV', N'en-US', N'Television', N'Client acquired through TV.'), (N'TV', N'es-ES', N'Television', N'Cliente adquirido via television.'),
        (N'RADIO', N'pt-PT', N'Radio', N'Cliente originado pela radio.'), (N'RADIO', N'pt-BR', N'Radio', N'Cliente originado pela radio.'),
        (N'RADIO', N'en-US', N'Radio', N'Client acquired through radio.'), (N'RADIO', N'es-ES', N'Radio', N'Cliente adquirido via radio.'),
        (N'NEWSPAPER', N'pt-PT', N'Jornal', N'Cliente originado por jornal.'), (N'NEWSPAPER', N'pt-BR', N'Jornal', N'Cliente originado por jornal.'),
        (N'NEWSPAPER', N'en-US', N'Newspaper', N'Client acquired through newspaper.'), (N'NEWSPAPER', N'es-ES', N'Periodico', N'Cliente adquirido via periodico.'),
        (N'MAGAZINE', N'pt-PT', N'Revista', N'Cliente originado por revista.'), (N'MAGAZINE', N'pt-BR', N'Revista', N'Cliente originado por revista.'),
        (N'MAGAZINE', N'en-US', N'Magazine', N'Client acquired through magazine.'), (N'MAGAZINE', N'es-ES', N'Revista', N'Cliente adquirido via revista.'),
        (N'EVENTS', N'pt-PT', N'Eventos', N'Cliente originado por eventos.'), (N'EVENTS', N'pt-BR', N'Eventos', N'Cliente originado por eventos.'),
        (N'EVENTS', N'en-US', N'Events', N'Client acquired through events.'), (N'EVENTS', N'es-ES', N'Eventos', N'Cliente adquirido via eventos.');

    INSERT INTO dbo.AcquisitionSourceTypeTranslations (AcquisitionSourceTypeId, LanguageCode, Name, Description)
    SELECT p.Id, s.LanguageCode, s.Name, s.Description
    FROM @AcqSrcTr4 s INNER JOIN dbo.AcquisitionSourceTypes p ON p.Code = s.Code AND p.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.AcquisitionSourceTypeTranslations tr WHERE tr.AcquisitionSourceTypeId = p.Id AND tr.LanguageCode = s.LanguageCode);

    -- AddressTypeTranslations 4 idiomas (principais)
    DECLARE @AddrTr4 TABLE (Code NVARCHAR(50) NOT NULL, LanguageCode NVARCHAR(10) NOT NULL, Name NVARCHAR(200) NOT NULL, Description NVARCHAR(500) NULL, PRIMARY KEY (Code, LanguageCode));
    INSERT INTO @AddrTr4 (Code, LanguageCode, Name, Description)
    VALUES
        (N'RESIDENTIAL', N'pt-PT', N'Morada residencial', N'Morada para habitacao.'), (N'RESIDENTIAL', N'pt-BR', N'Endereco residencial', N'Endereco para habitacao.'),
        (N'RESIDENTIAL', N'en-US', N'Residential address', N'Address for housing.'), (N'RESIDENTIAL', N'es-ES', N'Direccion residencial', N'Direccion para vivienda.'),
        (N'COMMERCIAL', N'pt-PT', N'Morada comercial', N'Morada de negocio ou estabelecimento comercial.'), (N'COMMERCIAL', N'pt-BR', N'Endereco comercial', N'Endereco de negocio.'),
        (N'COMMERCIAL', N'en-US', N'Commercial address', N'Business address.'), (N'COMMERCIAL', N'es-ES', N'Direccion comercial', N'Direccion comercial.'),
        (N'INDUSTRIAL', N'pt-PT', N'Morada industrial', N'Morada de fabrica ou armazem.'), (N'INDUSTRIAL', N'pt-BR', N'Endereco industrial', N'Endereco de fabrica.'),
        (N'INDUSTRIAL', N'en-US', N'Industrial address', N'Factory or warehouse address.'), (N'INDUSTRIAL', N'es-ES', N'Direccion industrial', N'Direccion de fabrica.');

    INSERT INTO dbo.AddressTypeTranslations (AddressTypeId, LanguageCode, Name, Description)
    SELECT p.Id, s.LanguageCode, s.Name, s.Description
    FROM @AddrTr4 s INNER JOIN dbo.AddressTypes p ON p.Code = s.Code AND p.IsDeleted = 0
    WHERE NOT EXISTS (SELECT 1 FROM dbo.AddressTypeTranslations tr WHERE tr.AddressTypeId = p.Id AND tr.LanguageCode = s.LanguageCode);

    /* =========================================================
       44. FINALIZACAO
       ========================================================= */

    COMMIT TRANSACTION;

    PRINT '========================================';
    PRINT 'Initial_BackOffice-v2 executado com sucesso!';
    PRINT 'Tenant: Gerit Demo Lda (demo@gerit.pt)';
    PRINT 'Utilizadores: Dener Viana (BackOffice) + Admin (Manager)';
    PRINT 'Clientes: 15 | Colaboradores: 6 | Equipas: 3';
    PRINT 'Visitas: 20 (Janeiro-Marco 2026)';
    PRINT 'Plano: STANDARD | Assinatura: Ativa';
    PRINT '========================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    PRINT '========================================';
    PRINT 'ERRO durante a execucao do script v2';
    PRINT 'Mensagem: ' + @ErrorMessage;
    PRINT 'Severidade: ' + CAST(@ErrorSeverity AS NVARCHAR(10));
    PRINT '========================================';

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH
