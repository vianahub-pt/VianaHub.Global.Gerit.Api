/* =========================================================
   INITIAL BACKOFFICE SEED - IDEMPOTENT
   Atualizado para o schema multi-tenant atual.

   Principais características:
   - Usa Code como chave estável dos catálogos e Name canónico para autorização.
   - Carrega traduções pt-PT, pt-BR, en-US e es-ES.
   - Define pt-PT como idioma padrão nas preferências iniciais.
   - Cria StatusDomains e StatusDefinitions por tenant.
   - Usa PartyTypeId para clientes individuais/organizações.
   - Não referencia tabelas removidas do modelo anterior.
   - Preserva os tenants, utilizadores, permissões e clientes demo.
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
        THROW 51000, 'O schema ainda não foi criado. Execute primeiro o Create-Tables.sql.', 1;

    -- A política RLS do schema permite esta chave para operações administrativas.
    EXEC sys.sp_set_session_context @key = N'IsSuperAdmin', @value = 1;

    DECLARE @UtcNow DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @SeedActorId INT = 1;

    /* =========================================================
       1. PARTY TYPES E TRADUÇÕES
       ========================================================= */

    DECLARE @PartyTypeSeed TABLE (
        Id TINYINT PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL,
        PtName NVARCHAR(100) NOT NULL,
        PtDescription NVARCHAR(300) NULL,
        EnName NVARCHAR(100) NOT NULL,
        EnDescription NVARCHAR(300) NULL
    );

    INSERT INTO @PartyTypeSeed (Id, Code, PtName, PtDescription, EnName, EnDescription)
    VALUES
        (1, N'INDIVIDUAL', N'Pessoa singular', N'Representa uma pessoa singular.', N'Individual', N'Represents a natural person.'),
        (2, N'ORGANIZATION', N'Organização', N'Representa uma empresa, associação ou outra pessoa coletiva.', N'Organization', N'Represents a company, association, or other legal entity.');

    UPDATE pt
       SET pt.Code = s.Code,
           pt.IsActive = 1,
           pt.IsDeleted = 0,
           pt.ModifiedBy = @SeedActorId,
           pt.ModifiedAt = @UtcNow
    FROM dbo.PartyTypes pt
    INNER JOIN @PartyTypeSeed s ON s.Id = pt.Id;

    INSERT INTO dbo.PartyTypes (Id, Code, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT s.Id, s.Code, 1, 0, @SeedActorId, @UtcNow
    FROM @PartyTypeSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PartyTypes pt WHERE pt.Id = s.Id);

    UPDATE tr
       SET tr.Name = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
           tr.Description = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM dbo.PartyTypeTranslations tr
    INNER JOIN @PartyTypeSeed s ON s.Id = tr.PartyTypeId
    WHERE tr.LanguageCode IN (N'pt-PT', N'en-US');

    INSERT INTO dbo.PartyTypeTranslations (PartyTypeId, LanguageCode, Name, Description)
    SELECT s.Id, l.LanguageCode,
           CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
           CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM @PartyTypeSeed s
    CROSS JOIN (VALUES (N'pt-PT'), (N'en-US')) l(LanguageCode)
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.PartyTypeTranslations tr
        WHERE tr.PartyTypeId = s.Id
          AND tr.LanguageCode = l.LanguageCode
    );

    /* =========================================================
       2. ORIGENS DE AQUISIÇÃO
       ========================================================= */

    DECLARE @AcquisitionSourceSeed TABLE (
        Code NVARCHAR(50) PRIMARY KEY,
        PtName NVARCHAR(100) NOT NULL,
        PtDescription NVARCHAR(300) NULL,
        EnName NVARCHAR(100) NOT NULL,
        EnDescription NVARCHAR(300) NULL
    );

    INSERT INTO @AcquisitionSourceSeed (Code, PtName, PtDescription, EnName, EnDescription)
    VALUES
        (N'OTHER', N'Outros', N'Origem não especificada ou não classificada.', N'Other', N'Unspecified or unclassified acquisition source.'),
        (N'INSTAGRAM', N'Instagram', N'Cliente ou tenant originado através do Instagram.', N'Instagram', N'Client or tenant acquired through Instagram.'),
        (N'FACEBOOK', N'Facebook', N'Cliente ou tenant originado através do Facebook.', N'Facebook', N'Client or tenant acquired through Facebook.'),
        (N'LINKEDIN', N'LinkedIn', N'Cliente ou tenant originado através do LinkedIn.', N'LinkedIn', N'Client or tenant acquired through LinkedIn.'),
        (N'YOUTUBE', N'YouTube', N'Cliente ou tenant originado através do YouTube.', N'YouTube', N'Client or tenant acquired through YouTube.'),
        (N'WHATSAPP', N'WhatsApp', N'Cliente ou tenant originado através de contacto por WhatsApp.', N'WhatsApp', N'Client or tenant acquired through WhatsApp.'),
        (N'TIKTOK', N'TikTok', N'Cliente ou tenant originado através do TikTok.', N'TikTok', N'Client or tenant acquired through TikTok.'),
        (N'GOOGLE', N'Google', N'Cliente ou tenant originado através de pesquisa ou anúncio no Google.', N'Google', N'Client or tenant acquired through Google search or advertising.'),
        (N'REFERRAL', N'Indicação', N'Cliente ou tenant originado por indicação de amigos ou conhecidos.', N'Referral', N'Client or tenant acquired through a referral.'),
        (N'TV', N'Televisão', N'Cliente ou tenant originado através de publicidade ou menção em televisão.', N'Television', N'Client or tenant acquired through television.'),
        (N'RADIO', N'Rádio', N'Cliente ou tenant originado através de rádio.', N'Radio', N'Client or tenant acquired through radio.'),
        (N'NEWSPAPER', N'Jornal', N'Cliente ou tenant originado através de jornal.', N'Newspaper', N'Client or tenant acquired through a newspaper.'),
        (N'MAGAZINE', N'Revista', N'Cliente ou tenant originado através de revista.', N'Magazine', N'Client or tenant acquired through a magazine.'),
        (N'EVENTS', N'Eventos', N'Cliente ou tenant originado através de eventos.', N'Events', N'Client or tenant acquired through events.');

    UPDATE t
       SET 
           t.IsActive = 1,
           t.ModifiedBy = @SeedActorId,
           t.ModifiedAt = @UtcNow
    FROM dbo.AcquisitionSourceTypes t
    INNER JOIN @AcquisitionSourceSeed s ON s.Code = t.Code
    WHERE t.IsDeleted = 0;

    INSERT INTO dbo.AcquisitionSourceTypes (Code, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT s.Code, 1, 0, @SeedActorId, @UtcNow
    FROM @AcquisitionSourceSeed s
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.AcquisitionSourceTypes t WHERE t.Code = s.Code AND t.IsDeleted = 0
    );

    UPDATE tr
       SET tr.Name = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
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
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.AcquisitionSourceTypeTranslations tr
        WHERE tr.AcquisitionSourceTypeId = p.Id
          AND tr.LanguageCode = l.LanguageCode
    );

    /* =========================================================
       3. TIPOS DE MORADA
       ========================================================= */

    DECLARE @AddressTypeSeed TABLE (
        Code NVARCHAR(50) PRIMARY KEY,
        PtName NVARCHAR(200) NOT NULL,
        PtDescription NVARCHAR(500) NULL,
        EnName NVARCHAR(200) NOT NULL,
        EnDescription NVARCHAR(500) NULL
    );

    INSERT INTO @AddressTypeSeed (Code, PtName, PtDescription, EnName, EnDescription)
    VALUES
        (N'RESIDENTIAL', N'Morada residencial', N'Morada para habitação, usada como endereço principal de pessoas.', N'Residential address', N'Address used as a person''s primary residence.'),
        (N'COMMERCIAL', N'Morada comercial', N'Morada de um negócio ou estabelecimento para atividade comercial e atendimento.', N'Commercial address', N'Business or establishment address used for commercial activity.'),
        (N'INDUSTRIAL', N'Morada industrial', N'Morada associada a fábrica, unidade industrial ou armazém.', N'Industrial address', N'Address associated with a factory, industrial unit, or warehouse.'),
        (N'RURAL', N'Morada rural', N'Morada em área rural, ligada a atividade agrícola ou residência isolada.', N'Rural address', N'Address in a rural area, related to agriculture or an isolated residence.'),
        (N'PUBLIC_SERVICE', N'Morada de serviços públicos', N'Morada de uma entidade ou serviço público.', N'Public service address', N'Address of a public authority or public service.'),
        (N'EDUCATION', N'Morada de educação', N'Morada de uma instituição de ensino.', N'Education address', N'Address of an educational institution.'),
        (N'HEALTHCARE', N'Morada de saúde', N'Morada de um serviço ou instituição de saúde.', N'Healthcare address', N'Address of a healthcare service or institution.'),
        (N'HOSPITALITY', N'Morada de alojamento ou turismo', N'Morada de uma unidade de alojamento ou turismo.', N'Hospitality address', N'Address of a hospitality or tourism establishment.'),
        (N'LOGISTICS', N'Morada logística ou distribuição', N'Morada dedicada a logística, armazenamento ou distribuição.', N'Logistics address', N'Address dedicated to logistics, warehousing, or distribution.'),
        (N'POSTAL_ALTERNATIVE', N'Morada postal alternativa', N'Morada alternativa para correspondência ou entregas.', N'Alternative postal address', N'Alternative address for correspondence or deliveries.');

    UPDATE t
       SET 
           t.IsActive = 1,
           t.ModifiedBy = @SeedActorId,
           t.ModifiedAt = @UtcNow
    FROM dbo.AddressTypes t
    INNER JOIN @AddressTypeSeed s ON s.Code = t.Code
    WHERE t.IsDeleted = 0;

    INSERT INTO dbo.AddressTypes (Code, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT s.Code, 1, 0, @SeedActorId, @UtcNow
    FROM @AddressTypeSeed s
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.AddressTypes t WHERE t.Code = s.Code AND t.IsDeleted = 0
    );

    UPDATE tr
       SET tr.Name = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
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
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.AddressTypeTranslations tr
        WHERE tr.AddressTypeId = p.Id
          AND tr.LanguageCode = l.LanguageCode
    );

    /* =========================================================
       4. TIPOS DE DOCUMENTO
       ========================================================= */

    DECLARE @DocumentTypeSeed TABLE (
        Code NVARCHAR(50) PRIMARY KEY,
        PtName NVARCHAR(100) NOT NULL,
        PtDescription NVARCHAR(300) NULL,
        EnName NVARCHAR(100) NOT NULL,
        EnDescription NVARCHAR(300) NULL
    );

    INSERT INTO @DocumentTypeSeed (Code, PtName, PtDescription, EnName, EnDescription)
    VALUES
        (N'CITIZEN_CARD', N'Cartão de Cidadão', N'Documento de identificação civil português.', N'Citizen card', N'Portuguese civil identification document.'),
        (N'PASSPORT', N'Passaporte', N'Documento internacional de identificação e viagem.', N'Passport', N'International identity and travel document.'),
        (N'DRIVING_LICENSE', N'Carta de Condução', N'Documento que autoriza a condução de veículos.', N'Driving licence', N'Document authorizing a person to drive vehicles.'),
        (N'TAX_IDENTIFICATION', N'Identificação fiscal', N'Documento ou comprovativo de identificação fiscal.', N'Tax identification', N'Tax identification document or certificate.'),
        (N'COMPANY_REGISTRATION', N'Registo comercial', N'Documento de registo ou constituição de uma organização.', N'Company registration', N'Company incorporation or registration document.'),
        (N'OTHER', N'Outro documento', N'Outro tipo de documento não classificado.', N'Other document', N'Other unclassified document type.');

    UPDATE t
       SET 
           t.IsActive = 1,
           t.ModifiedBy = @SeedActorId,
           t.ModifiedAt = @UtcNow
    FROM dbo.DocumentTypes t
    INNER JOIN @DocumentTypeSeed s ON s.Code = t.Code
    WHERE t.IsDeleted = 0;

    INSERT INTO dbo.DocumentTypes (Code, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT s.Code, 1, 0, @SeedActorId, @UtcNow
    FROM @DocumentTypeSeed s
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.DocumentTypes t WHERE t.Code = s.Code AND t.IsDeleted = 0
    );

    UPDATE tr
       SET tr.Name = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
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
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.DocumentTypeTranslations tr
        WHERE tr.DocumentTypeId = p.Id
          AND tr.LanguageCode = l.LanguageCode
    );

    /* =========================================================
       5. TIPOS DE FICHEIRO
       ========================================================= */

    DECLARE @FileTypeSeed TABLE (
        Code NVARCHAR(50) PRIMARY KEY,
        MimeType NVARCHAR(100) NULL,
        Extension NVARCHAR(20) NULL,
        PtName NVARCHAR(100) NOT NULL,
        PtDescription NVARCHAR(300) NULL,
        EnName NVARCHAR(100) NOT NULL,
        EnDescription NVARCHAR(300) NULL
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
        (N'XLS', N'application/vnd.ms-excel', N'xls', N'Folha Excel 97-2003', N'Folha de cálculo Microsoft Excel no formato XLS.', N'Excel 97-2003 workbook', N'Microsoft Excel workbook in XLS format.'),
        (N'XLSX', N'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', N'xlsx', N'Folha Excel', N'Folha de cálculo Microsoft Excel no formato XLSX.', N'Excel workbook', N'Microsoft Excel workbook in XLSX format.'),
        (N'PPT', N'application/vnd.ms-powerpoint', N'ppt', N'Apresentação PowerPoint 97-2003', N'Apresentação Microsoft PowerPoint no formato PPT.', N'PowerPoint 97-2003 presentation', N'Microsoft PowerPoint presentation in PPT format.'),
        (N'PPTX', N'application/vnd.openxmlformats-officedocument.presentationml.presentation', N'pptx', N'Apresentação PowerPoint', N'Apresentação Microsoft PowerPoint no formato PPTX.', N'PowerPoint presentation', N'Microsoft PowerPoint presentation in PPTX format.'),
        (N'TXT', N'text/plain', N'txt', N'Ficheiro de texto', N'Ficheiro de texto simples.', N'Text file', N'Plain text file.'),
        (N'CSV', N'text/csv', N'csv', N'Ficheiro CSV', N'Ficheiro de valores separados por vírgulas.', N'CSV file', N'Comma-separated values file.'),
        (N'JSON', N'application/json', N'json', N'Ficheiro JSON', N'Ficheiro de dados no formato JSON.', N'JSON file', N'Data file in JSON format.'),
        (N'XML', N'application/xml', N'xml', N'Ficheiro XML', N'Ficheiro de dados no formato XML.', N'XML file', N'Data file in XML format.'),
        (N'ZIP', N'application/zip', N'zip', N'Arquivo ZIP', N'Arquivo comprimido no formato ZIP.', N'ZIP archive', N'Compressed archive in ZIP format.'),
        (N'RAR', N'application/x-rar-compressed', N'rar', N'Arquivo RAR', N'Arquivo comprimido no formato RAR.', N'RAR archive', N'Compressed archive in RAR format.'),
        (N'SEVEN_ZIP', N'application/x-7z-compressed', N'7z', N'Arquivo 7-Zip', N'Arquivo comprimido no formato 7z.', N'7-Zip archive', N'Compressed archive in 7z format.'),
        (N'BINARY', N'application/octet-stream', N'bin', N'Ficheiro binário', N'Ficheiro binário genérico.', N'Binary file', N'Generic binary file.');

    UPDATE t
       SET            t.MimeType = s.MimeType,
           t.Extension = s.Extension,
           t.IsActive = 1,
           t.ModifiedBy = @SeedActorId,
           t.ModifiedAt = @UtcNow
    FROM dbo.FileTypes t
    INNER JOIN @FileTypeSeed s ON s.Code = t.Code
    WHERE t.IsDeleted = 0;

    INSERT INTO dbo.FileTypes (Code, MimeType, Extension, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT s.Code, s.MimeType, s.Extension, 1, 0, @SeedActorId, @UtcNow
    FROM @FileTypeSeed s
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.FileTypes t WHERE t.Code = s.Code AND t.IsDeleted = 0
    );

    UPDATE tr
       SET tr.Name = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
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
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.FileTypeTranslations tr
        WHERE tr.FileTypeId = p.Id
          AND tr.LanguageCode = l.LanguageCode
    );

    /* =========================================================
       6. DOMÍNIOS DE STATUS
       ========================================================= */

    DECLARE @StatusDomainSeed TABLE (
        Code NVARCHAR(50) PRIMARY KEY,
        PtName NVARCHAR(100) NOT NULL,
        PtDescription NVARCHAR(300) NULL,
        EnName NVARCHAR(100) NOT NULL,
        EnDescription NVARCHAR(300) NULL
    );

    INSERT INTO @StatusDomainSeed (Code, PtName, PtDescription, EnName, EnDescription)
    VALUES
        (N'CLIENT', N'Cliente', N'Estados aplicáveis a clientes.', N'Client', N'Statuses applicable to clients.'),
        (N'EMPLOYEE', N'Colaborador', N'Estados aplicáveis a colaboradores.', N'Employee', N'Statuses applicable to employees.'),
        (N'EQUIPMENT', N'Equipamento', N'Estados aplicáveis a equipamentos.', N'Equipment', N'Statuses applicable to equipment.'),
        (N'VEHICLE', N'Veículo', N'Estados aplicáveis a veículos.', N'Vehicle', N'Statuses applicable to vehicles.'),
        (N'VISIT', N'Visita', N'Estados aplicáveis a visitas ou intervenções.', N'Visit', N'Statuses applicable to visits or interventions.'),
        (N'SUBSCRIPTION', N'Assinatura', N'Estados aplicáveis a assinaturas.', N'Subscription', N'Statuses applicable to subscriptions.');

    UPDATE t
       SET 
           t.IsActive = 1,
           t.ModifiedBy = @SeedActorId,
           t.ModifiedAt = @UtcNow
    FROM dbo.StatusDomains t
    INNER JOIN @StatusDomainSeed s ON s.Code = t.Code
    WHERE t.IsDeleted = 0;

    INSERT INTO dbo.StatusDomains (Code, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT s.Code, 1, 0, @SeedActorId, @UtcNow
    FROM @StatusDomainSeed s
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.StatusDomains t WHERE t.Code = s.Code AND t.IsDeleted = 0
    );

    UPDATE tr
       SET tr.Name = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
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
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.StatusDomainTranslations tr
        WHERE tr.StatusDomainId = p.Id
          AND tr.LanguageCode = l.LanguageCode
    );

    /* =========================================================
       7. PLANOS E TRADUÇÕES
       ========================================================= */

    DECLARE @PlanSeed TABLE (
        Code NVARCHAR(50) PRIMARY KEY,
        PricePerHour DECIMAL(19,4) NULL,
        PricePerDay DECIMAL(19,4) NULL,
        PricePerMonth DECIMAL(19,4) NULL,
        PricePerYear DECIMAL(19,4) NULL,
        Currency NVARCHAR(3) NOT NULL,
        MaxUsers INT NOT NULL,
        MaxPhotosPerVisit INT NOT NULL,
        PtName NVARCHAR(100) NOT NULL,
        PtDescription NVARCHAR(500) NULL,
        EnName NVARCHAR(100) NOT NULL,
        EnDescription NVARCHAR(500) NULL
    );

    INSERT INTO @PlanSeed (
        Code, PricePerHour, PricePerDay, PricePerMonth, PricePerYear,
        Currency, MaxUsers, MaxPhotosPerVisit,
        PtName, PtDescription, EnName, EnDescription
    )
    VALUES
        (N'FREE', NULL, NULL, 0.0, 0.0, N'EUR', 1, 10, N'Gratuito', N'Plano gratuito com funcionalidades básicas para testes e uso inicial.', N'Free', N'Free plan with basic features for testing and initial use.'),
        (N'BASIC', NULL, NULL, 19.9, 199.0, N'EUR', 3, 50, N'Básico', N'Plano básico para pequenos negócios com funcionalidades essenciais.', N'Basic', N'Basic plan for small businesses with essential features.'),
        (N'STANDARD', NULL, NULL, 49.9, 499.0, N'EUR', 10, 200, N'Standard', N'Plano intermédio com mais capacidade e funcionalidades avançadas.', N'Standard', N'Intermediate plan with greater capacity and advanced features.'),
        (N'PROFESSIONAL', NULL, NULL, 99.9, 999.0, N'EUR', 25, 500, N'Profissional', N'Plano avançado para equipas com maior volume de intervenções.', N'Professional', N'Advanced plan for teams with a higher volume of interventions.'),
        (N'ENTERPRISE', NULL, NULL, 199.9, 1999.0, N'EUR', 100, 2000, N'Enterprise', N'Plano completo para grandes organizações com necessidades complexas.', N'Enterprise', N'Complete plan for large organizations with complex requirements.'),
        (N'PAYG_HOURLY', 5.0, NULL, NULL, NULL, N'EUR', 10, 100, N'Pagamento por hora', N'Plano baseado em consumo por hora.', N'Pay as you go hourly', N'Usage-based plan billed by the hour.'),
        (N'PAYG_DAILY', NULL, 25.0, NULL, NULL, N'EUR', 10, 100, N'Pagamento por dia', N'Plano baseado em consumo por dia.', N'Pay as you go daily', N'Usage-based plan billed by the day.');

    UPDATE p
       SET p.PricePerHour = s.PricePerHour,
           p.PricePerDay = s.PricePerDay,
           p.PricePerMonth = s.PricePerMonth,
           p.PricePerYear = s.PricePerYear,
           p.Currency = s.Currency,
           p.MaxUsers = s.MaxUsers,
           p.MaxPhotosPerVisit = s.MaxPhotosPerVisit,
           p.IsActive = 1,
           p.ModifiedBy = @SeedActorId,
           p.ModifiedAt = @UtcNow
    FROM dbo.SubscriptionPlans p
    INNER JOIN @PlanSeed s ON s.Code = p.Code
    WHERE p.IsDeleted = 0;

    INSERT INTO dbo.SubscriptionPlans (
        Code, PricePerHour, PricePerDay, PricePerMonth, PricePerYear,
        Currency, MaxUsers, MaxPhotosPerVisit,
        IsActive, IsDeleted, CreatedBy, CreatedAt
    )
    SELECT s.Code, s.PricePerHour, s.PricePerDay, s.PricePerMonth, s.PricePerYear,
           s.Currency, s.MaxUsers, s.MaxPhotosPerVisit,
           1, 0, @SeedActorId, @UtcNow
    FROM @PlanSeed s
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.SubscriptionPlans p WHERE p.Code = s.Code AND p.IsDeleted = 0
    );

    UPDATE tr
       SET tr.Name = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
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
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.SubscriptionPlanTranslations tr
        WHERE tr.SubscriptionPlanId = p.Id
          AND tr.LanguageCode = l.LanguageCode
    );

    INSERT INTO dbo.SubscriptionPlanFileRules (
        SubscriptionPlanId, FileTypeId, MaxFileSizeMB,
        IsActive, IsDeleted, CreatedBy, CreatedAt
    )
    SELECT p.Id,
           f.Id,
           CASE p.Code
               WHEN N'FREE' THEN 2
               WHEN N'BASIC' THEN 10
               WHEN N'STANDARD' THEN 25
               WHEN N'PROFESSIONAL' THEN 75
               WHEN N'ENTERPRISE' THEN 500
               WHEN N'PAYG_HOURLY' THEN 50
               WHEN N'PAYG_DAILY' THEN 50
               ELSE 10
           END,
           1, 0, @SeedActorId, @UtcNow
    FROM dbo.SubscriptionPlans p
    CROSS JOIN dbo.FileTypes f
    WHERE p.IsDeleted = 0
      AND f.IsDeleted = 0
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.SubscriptionPlanFileRules r
          WHERE r.SubscriptionPlanId = p.Id
            AND r.FileTypeId = f.Id
            AND r.IsDeleted = 0
      );

    /* =========================================================
       8. AÇÕES E RECURSOS DE AUTORIZAÇÃO
       ========================================================= */

    DECLARE @ActionSeed TABLE (
        Name NVARCHAR(50) PRIMARY KEY,
        Description NVARCHAR(500) NOT NULL
    );

    INSERT INTO @ActionSeed (Name, Description)
    VALUES
        (N'GetAll', N'Obter todos os registos.'),
        (N'GetBy', N'Obter um registo por identificador ou critério.'),
        (N'GetPaged', N'Obter uma lista paginada com filtros.'),
        (N'Create', N'Criar um novo registo.'),
        (N'Update', N'Atualizar um registo existente.'),
        (N'Activate', N'Ativar um registo.'),
        (N'Deactivate', N'Desativar um registo.'),
        (N'Delete', N'Eliminar logicamente um registo.'),
        (N'BulkUpload', N'Efetuar cadastro em massa de registos.'),
        (N'Execute', N'Executar uma ação específica.'),
        (N'GetActive', N'Obter registos ativos.'),
        (N'GetExpiring', N'Obter registos próximos da expiração.'),
        (N'Cancel', N'Cancelar uma operação ou entidade.'),
        (N'Renew', N'Renovar um contrato, assinatura ou entidade.');

    UPDATE a
       SET a.Description = s.Description,
           a.IsActive = 1,
           a.ModifiedBy = @SeedActorId,
           a.ModifiedAt = @UtcNow
    FROM dbo.Actions a
    INNER JOIN @ActionSeed s ON s.Name = a.Name
    WHERE a.IsDeleted = 0;

    INSERT INTO dbo.Actions (Name, Description, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT s.Name, s.Description, 1, 0, @SeedActorId, @UtcNow
    FROM @ActionSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Actions a WHERE a.Name = s.Name AND a.IsDeleted = 0);

    DECLARE @ResourceSeed TABLE (
        Name NVARCHAR(100) PRIMARY KEY,
        Description NVARCHAR(500) NOT NULL
    );

    INSERT INTO @ResourceSeed (Name, Description)
    VALUES
        (N'PartyTypes', N'Tipos de partes: pessoa singular ou organização.'),
        (N'AcquisitionSourceTypes', N'Tipos de origem de aquisição comercial.'),
        (N'AddressTypes', N'Tipos de morada disponíveis.'),
        (N'DocumentTypes', N'Tipos de documentos.'),
        (N'FileTypes', N'Tipos de ficheiros.'),
        (N'StatusDomains', N'Domínios funcionais de status.'),
        (N'SubscriptionPlans', N'Planos de assinatura.'),
        (N'SubscriptionPlanFileRules', N'Regras de ficheiros por plano.'),
        (N'Tenants', N'Tenants do sistema.'),
        (N'TenantContactPersons', N'Pessoas de contacto dos tenants.'),
        (N'TenantAddresses', N'Moradas dos tenants.'),
        (N'TenantFiscalData', N'Dados fiscais dos tenants.'),
        (N'TenantDocuments', N'Documentos dos tenants.'),
        (N'StatusDefinitions', N'Definições de status por tenant e domínio.'),
        (N'Subscriptions', N'Assinaturas dos tenants.'),
        (N'Users', N'Utilizadores do sistema.'),
        (N'UserPreferences', N'Preferências dos utilizadores.'),
        (N'Roles', N'Papéis ou perfis de acesso.'),
        (N'Resources', N'Recursos protegidos pelo sistema de autorização.'),
        (N'Actions', N'Ações disponíveis no sistema de autorização.'),
        (N'RolePermissions', N'Permissões associadas aos papéis.'),
        (N'UserRoles', N'Papéis associados aos utilizadores.'),
        (N'RefreshTokens', N'Tokens de atualização.'),
        (N'JwtKeys', N'Chaves de assinatura JWT.'),
        (N'JobDefinitions', N'Definições de jobs e processos.'),
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
        (N'EmployeeTeam', N'Relação histórica entre colaboradores e equipas.'),
        (N'EquipmentTypes', N'Tipos de equipamentos.'),
        (N'Equipments', N'Equipamentos.'),
        (N'Vehicles', N'Veículos.'),
        (N'Visits', N'Visitas ou intervenções.'),
        (N'VisitContactPersons', N'Pessoas de contacto das visitas.'),
        (N'VisitAddresses', N'Moradas das visitas.'),
        (N'VisitTeams', N'Equipas associadas às visitas.'),
        (N'VisitTeamFunctions', N'Funções operacionais nas equipas de visita.'),
        (N'VisitTeamEmployee', N'Colaboradores associados às equipas de visita.'),
        (N'VisitTeamVehicles', N'Veículos associados às equipas de visita.'),
        (N'VisitTeamEquipments', N'Equipamentos associados às equipas de visita.'),
        (N'VisitAttachments', N'Anexos das visitas.');

    UPDATE r
       SET r.Description = s.Description,
           r.IsActive = 1,
           r.ModifiedBy = @SeedActorId,
           r.ModifiedAt = @UtcNow
    FROM dbo.Resources r
    INNER JOIN @ResourceSeed s ON s.Name = r.Name
    WHERE r.IsDeleted = 0;

    INSERT INTO dbo.Resources (Name, Description, IsActive, IsDeleted, CreatedBy, CreatedAt)
    SELECT s.Name, s.Description, 1, 0, @SeedActorId, @UtcNow
    FROM @ResourceSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Resources r WHERE r.Name = s.Name AND r.IsDeleted = 0);

    /* =========================================================
       9. TENANTS, CONTACTOS E MORADAS
       ========================================================= */

    DECLARE @TenantSeed TABLE (
        Email NVARCHAR(255) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        WebsiteUrl NVARCHAR(255) NULL,
        ImageUrl NVARCHAR(500) NULL,
        Note NVARCHAR(1000) NULL
    );

    INSERT INTO @TenantSeed (Email, Name, WebsiteUrl, ImageUrl, Note)
    VALUES
        (N'contact@vianahub.pt', N'VianaHub Lda', N'https://vianahub.pt', NULL, N'Tenant principal'),
        (N'demo@gerit.pt', N'Gerit Demo Lda', N'https://demo.gerit.pt', NULL, N'Ambiente de demonstração'),
        (N'teste@teste.pt', N'Teste Lda', NULL, NULL, N'Tenant para testes internos');

    UPDATE t
       SET t.Name = s.Name,
           t.WebsiteUrl = s.WebsiteUrl,
           t.ImageUrl = s.ImageUrl,
           t.Note = s.Note,
           t.IsActive = 1,
           t.ModifiedBy = @SeedActorId,
           t.ModifiedAt = @UtcNow
    FROM dbo.Tenants t
    INNER JOIN @TenantSeed s ON s.Email = t.Email
    WHERE t.IsDeleted = 0;

    INSERT INTO dbo.Tenants (
        PartyTypeId, AcquisitionSourceTypeId,
        Name, Email, WebsiteUrl, ImageUrl, Note,
        IsActive, IsDeleted, CreatedBy, CreatedAt
    )
    SELECT pt.Id,
           ast.Id,
           s.Name, s.Email, s.WebsiteUrl, s.ImageUrl, s.Note,
           1, 0, @SeedActorId, @UtcNow
    FROM @TenantSeed s
    INNER JOIN dbo.PartyTypes pt ON pt.Code = N'ORGANIZATION' AND pt.IsDeleted = 0
    INNER JOIN dbo.AcquisitionSourceTypes ast ON ast.Code = N'OTHER' AND ast.IsDeleted = 0
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.Tenants t WHERE t.Email = s.Email AND t.IsDeleted = 0
    );

    DECLARE @TenantContactSeed TABLE (
        TenantEmail NVARCHAR(255) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        JobTitle NVARCHAR(150) NULL,
        Department NVARCHAR(150) NULL,
        PhoneNumber NVARCHAR(50) NULL,
        CellPhoneNumber NVARCHAR(50) NULL,
        IsCellPhoneWhatsapp BIT NOT NULL,
        Email NVARCHAR(255) NULL
    );

    INSERT INTO @TenantContactSeed (
        TenantEmail, Name, JobTitle, Department,
        PhoneNumber, CellPhoneNumber, IsCellPhoneWhatsapp, Email
    )
    VALUES
        (N'contact@vianahub.pt', N'João Silva', N'Administrador', NULL, N'+351210000001', N'+351910000001', 1, N'contact@vianahub.pt'),
        (N'demo@gerit.pt', N'Maria Costa', N'Gestora', NULL, N'+351210000002', N'+351920000002', 1, N'demo@gerit.pt'),
        (N'teste@teste.pt', N'Carlos Teste', N'Responsável de testes', NULL, NULL, N'+351930000003', 1, N'teste@teste.pt');

    INSERT INTO dbo.TenantContactPersons (
        TenantId, JobTitle, Department, Name,
        PhoneNumber, CellPhoneNumber, IsCellPhoneWhatsapp, Email,
        IsPrimary, IsActive, IsDeleted, CreatedBy, CreatedAt
    )
    SELECT t.Id, s.JobTitle, s.Department, s.Name,
           s.PhoneNumber, s.CellPhoneNumber, s.IsCellPhoneWhatsapp, s.Email,
           1, 1, 0, @SeedActorId, @UtcNow
    FROM @TenantContactSeed s
    INNER JOIN dbo.Tenants t ON t.Email = s.TenantEmail AND t.IsDeleted = 0
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.TenantContactPersons cp
        WHERE cp.TenantId = t.Id
          AND cp.Email = s.Email
          AND cp.IsDeleted = 0
    );

    DECLARE @TenantAddressSeed TABLE (
        TenantEmail NVARCHAR(255) NOT NULL,
        AddressTypeCode NVARCHAR(50) NOT NULL,
        CountryCode CHAR(2) NOT NULL,
        Street NVARCHAR(200) NOT NULL,
        Neighborhood NVARCHAR(100) NULL,
        City NVARCHAR(100) NOT NULL,
        District NVARCHAR(100) NULL,
        PostalCode NVARCHAR(20) NOT NULL,
        StreetNumber NVARCHAR(20) NULL,
        Complement NVARCHAR(100) NULL,
        Latitude DECIMAL(9,6) NULL,
        Longitude DECIMAL(9,6) NULL,
        Note NVARCHAR(500) NULL
    );

    INSERT INTO @TenantAddressSeed (
        TenantEmail, AddressTypeCode, CountryCode, Street, Neighborhood,
        City, District, PostalCode, StreetNumber, Complement,
        Latitude, Longitude, Note
    )
    VALUES
        (N'contact@vianahub.pt', N'COMMERCIAL', 'PT', N'Avenida da Liberdade', N'Santo António', N'Lisboa', N'Lisboa', N'1250-140', N'100', N'3.º Andar', 38.722300, -9.139300, N'Sede principal'),
        (N'demo@gerit.pt', N'COMMERCIAL', 'PT', N'Rua de Santa Catarina', N'Baixa', N'Porto', N'Porto', N'4000-447', N'200', NULL, 41.149600, -8.611000, N'Escritório demo'),
        (N'teste@teste.pt', N'COMMERCIAL', 'PT', N'Avenida Central', N'Sé', N'Braga', N'Braga', N'4700-000', N'50', NULL, 41.545400, -8.426500, N'Endereço de teste');

    INSERT INTO dbo.TenantAddresses (
        TenantId, AddressTypeId, CountryCode,
        Street, Neighborhood, City, District, PostalCode,
        StreetNumber, Complement, Latitude, Longitude, Note,
        IsPrimary, IsActive, IsDeleted, CreatedBy, CreatedAt
    )
    SELECT t.Id, atp.Id, s.CountryCode,
           s.Street, s.Neighborhood, s.City, s.District, s.PostalCode,
           s.StreetNumber, s.Complement, s.Latitude, s.Longitude, s.Note,
           1, 1, 0, @SeedActorId, @UtcNow
    FROM @TenantAddressSeed s
    INNER JOIN dbo.Tenants t ON t.Email = s.TenantEmail AND t.IsDeleted = 0
    INNER JOIN dbo.AddressTypes atp ON atp.Code = s.AddressTypeCode AND atp.IsDeleted = 0
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.TenantAddresses a
        WHERE a.TenantId = t.Id
          AND a.IsPrimary = 1
          AND a.IsDeleted = 0
    );

    /* =========================================================
       10. ROLES POR TENANT
       ========================================================= */

    DECLARE @RoleSeed TABLE (
        Name NVARCHAR(100) PRIMARY KEY,
        Description NVARCHAR(500) NOT NULL
    );

    INSERT INTO @RoleSeed (Name, Description)
    VALUES
        (N'Admin', N'Acesso administrativo completo ao tenant.'),
        (N'BackOffice', N'Acesso a operações internas e administrativas.'),
        (N'Manager', N'Gestão de equipas, clientes e operações.'),
        (N'Operator', N'Execução de tarefas operacionais.'),
        (N'User', N'Acesso básico ao sistema.'),
        (N'SuperAdmin', N'Acesso total ao sistema e configurações avançadas.');

    UPDATE r
       SET r.Description = s.Description,
           r.IsActive = 1,
           r.ModifiedBy = @SeedActorId,
           r.ModifiedAt = @UtcNow
    FROM dbo.Roles r
    INNER JOIN dbo.Tenants t ON t.Id = r.TenantId AND t.IsDeleted = 0
    INNER JOIN @RoleSeed s ON s.Name = r.Name
    WHERE r.IsDeleted = 0;

    INSERT INTO dbo.Roles (
        TenantId, Name, Description,
        IsActive, IsDeleted, CreatedBy, CreatedAt
    )
    SELECT t.Id, s.Name, s.Description,
           1, 0, @SeedActorId, @UtcNow
    FROM dbo.Tenants t
    CROSS JOIN @RoleSeed s
    WHERE t.IsDeleted = 0
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.Roles r
          WHERE r.TenantId = t.Id
            AND r.Name = s.Name
            AND r.IsDeleted = 0
      );

    /* =========================================================
       11. STATUS DEFINITIONS POR TENANT E TRADUÇÕES
       ========================================================= */

    DECLARE @StatusDefinitionSeed TABLE (
        DomainCode NVARCHAR(50) NOT NULL,
        StatusCode NVARCHAR(50) NOT NULL,
        DisplayOrder INT NOT NULL,
        PtName NVARCHAR(200) NOT NULL,
        PtDescription NVARCHAR(500) NULL,
        EnName NVARCHAR(200) NOT NULL,
        EnDescription NVARCHAR(500) NULL,
        PRIMARY KEY (DomainCode, StatusCode)
    );

    INSERT INTO @StatusDefinitionSeed (
        DomainCode, StatusCode, DisplayOrder,
        PtName, PtDescription, EnName, EnDescription
    )
    VALUES
        (N'CLIENT', N'PROSPECT', 10, N'Potencial', N'Cliente potencial ainda não convertido.', N'Prospect', N'Potential client not yet converted.'),
        (N'CLIENT', N'ACTIVE', 20, N'Ativo', N'Cliente ativo.', N'Active', N'Active client.'),
        (N'CLIENT', N'INACTIVE', 30, N'Inativo', N'Cliente temporariamente inativo.', N'Inactive', N'Temporarily inactive client.'),
        (N'CLIENT', N'SUSPENDED', 40, N'Suspenso', N'Cliente com atividade suspensa.', N'Suspended', N'Client with suspended activity.'),
        (N'CLIENT', N'ARCHIVED', 50, N'Arquivado', N'Cliente arquivado para histórico.', N'Archived', N'Client archived for historical purposes.'),
        (N'EMPLOYEE', N'ACTIVE', 10, N'Ativo', N'Colaborador ativo.', N'Active', N'Active employee.'),
        (N'EMPLOYEE', N'ON_LEAVE', 20, N'Ausente', N'Colaborador temporariamente ausente.', N'On leave', N'Employee temporarily on leave.'),
        (N'EMPLOYEE', N'INACTIVE', 30, N'Inativo', N'Colaborador inativo.', N'Inactive', N'Inactive employee.'),
        (N'EMPLOYEE', N'TERMINATED', 40, N'Desvinculado', N'Colaborador sem vínculo ativo.', N'Terminated', N'Employee whose engagement has ended.'),
        (N'EQUIPMENT', N'AVAILABLE', 10, N'Disponível', N'Equipamento disponível para utilização.', N'Available', N'Equipment available for use.'),
        (N'EQUIPMENT', N'IN_USE', 20, N'Em utilização', N'Equipamento atualmente em utilização.', N'In use', N'Equipment currently in use.'),
        (N'EQUIPMENT', N'MAINTENANCE', 30, N'Em manutenção', N'Equipamento em manutenção.', N'Under maintenance', N'Equipment undergoing maintenance.'),
        (N'EQUIPMENT', N'RETIRED', 40, N'Retirado', N'Equipamento retirado de operação.', N'Retired', N'Equipment retired from service.'),
        (N'VEHICLE', N'AVAILABLE', 10, N'Disponível', N'Veículo disponível para utilização.', N'Available', N'Vehicle available for use.'),
        (N'VEHICLE', N'IN_USE', 20, N'Em utilização', N'Veículo atualmente em utilização.', N'In use', N'Vehicle currently in use.'),
        (N'VEHICLE', N'MAINTENANCE', 30, N'Em manutenção', N'Veículo em manutenção.', N'Under maintenance', N'Vehicle undergoing maintenance.'),
        (N'VEHICLE', N'RETIRED', 40, N'Retirado', N'Veículo retirado de operação.', N'Retired', N'Vehicle retired from service.'),
        (N'VISIT', N'SCHEDULED', 10, N'Agendada', N'Visita criada e agendada para uma data futura.', N'Scheduled', N'Visit created and scheduled for a future date.'),
        (N'VISIT', N'CONFIRMED', 20, N'Confirmada', N'Visita confirmada com o cliente.', N'Confirmed', N'Visit confirmed with the client.'),
        (N'VISIT', N'EN_ROUTE', 30, N'Em deslocação', N'Equipa em deslocação para o local.', N'En route', N'Team traveling to the location.'),
        (N'VISIT', N'IN_PROGRESS', 40, N'Em andamento', N'Visita em execução.', N'In progress', N'Visit currently being performed.'),
        (N'VISIT', N'PAUSED', 50, N'Em pausa', N'Visita temporariamente pausada.', N'Paused', N'Visit temporarily paused.'),
        (N'VISIT', N'WAITING_CLIENT', 60, N'A aguardar cliente', N'Visita parada a aguardar ação do cliente.', N'Waiting for client', N'Visit waiting for client action.'),
        (N'VISIT', N'WAITING_MATERIAL', 70, N'A aguardar material', N'Visita suspensa por falta de material.', N'Waiting for material', N'Visit waiting for required material.'),
        (N'VISIT', N'RESCHEDULED', 80, N'Reagendada', N'Visita reagendada para nova data.', N'Rescheduled', N'Visit rescheduled to a new date.'),
        (N'VISIT', N'COMPLETED', 90, N'Concluída', N'Visita concluída com sucesso.', N'Completed', N'Visit completed successfully.'),
        (N'VISIT', N'COMPLETED_PENDING', 100, N'Concluída com pendências', N'Visita concluída com itens pendentes.', N'Completed with pending items', N'Visit completed with pending items.'),
        (N'VISIT', N'CANCELED', 110, N'Cancelada', N'Visita cancelada.', N'Canceled', N'Visit canceled.'),
        (N'VISIT', N'NOT_PERFORMED', 120, N'Não realizada', N'Visita não realizada.', N'Not performed', N'Visit was not performed.'),
        (N'VISIT', N'VALIDATING', 130, N'Em validação', N'Visita a aguardar validação.', N'Under validation', N'Visit awaiting validation.'),
        (N'VISIT', N'INVOICED', 140, N'Faturada', N'Visita já faturada.', N'Invoiced', N'Visit already invoiced.'),
        (N'VISIT', N'ARCHIVED', 150, N'Arquivada', N'Visita encerrada e arquivada.', N'Archived', N'Visit closed and archived.'),
        (N'SUBSCRIPTION', N'TRIAL', 10, N'Em período experimental', N'Assinatura em período experimental.', N'Trial', N'Subscription in trial period.'),
        (N'SUBSCRIPTION', N'ACTIVE', 20, N'Ativa', N'Assinatura ativa.', N'Active', N'Active subscription.'),
        (N'SUBSCRIPTION', N'PAST_DUE', 30, N'Pagamento em atraso', N'Assinatura com pagamento em atraso.', N'Past due', N'Subscription with overdue payment.'),
        (N'SUBSCRIPTION', N'CANCELED', 40, N'Cancelada', N'Assinatura cancelada.', N'Canceled', N'Canceled subscription.'),
        (N'SUBSCRIPTION', N'EXPIRED', 50, N'Expirada', N'Assinatura expirada.', N'Expired', N'Expired subscription.');

    UPDATE sd
       SET sd.DisplayOrder = s.DisplayOrder,
           sd.IsSystem = 1,
           sd.IsActive = 1,
           sd.ModifiedBy = @SeedActorId,
           sd.ModifiedAt = @UtcNow
    FROM dbo.StatusDefinitions sd
    INNER JOIN dbo.Tenants t ON t.Id = sd.TenantId AND t.IsDeleted = 0
    INNER JOIN dbo.StatusDomains d ON d.Id = sd.StatusDomainId AND d.IsDeleted = 0
    INNER JOIN @StatusDefinitionSeed s ON s.DomainCode = d.Code AND s.StatusCode = sd.Code
    WHERE sd.IsDeleted = 0;

    INSERT INTO dbo.StatusDefinitions (
        TenantId, StatusDomainId, Code, DisplayOrder, IsSystem,
        IsActive, IsDeleted, CreatedBy, CreatedAt
    )
    SELECT t.Id, d.Id, s.StatusCode, s.DisplayOrder, 1,
           1, 0, @SeedActorId, @UtcNow
    FROM dbo.Tenants t
    CROSS JOIN @StatusDefinitionSeed s
    INNER JOIN dbo.StatusDomains d ON d.Code = s.DomainCode AND d.IsDeleted = 0
    WHERE t.IsDeleted = 0
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.StatusDefinitions sd
          WHERE sd.TenantId = t.Id
            AND sd.StatusDomainId = d.Id
            AND sd.Code = s.StatusCode
            AND sd.IsDeleted = 0
      );

    UPDATE tr
       SET tr.Name = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
           tr.Description = CASE WHEN tr.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM dbo.StatusDefinitionTranslations tr
    INNER JOIN dbo.StatusDefinitions sd
        ON sd.Id = tr.StatusDefinitionId
       AND sd.TenantId = tr.TenantId
       AND sd.StatusDomainId = tr.StatusDomainId
       AND sd.IsDeleted = 0
    INNER JOIN dbo.StatusDomains d ON d.Id = sd.StatusDomainId AND d.IsDeleted = 0
    INNER JOIN @StatusDefinitionSeed s ON s.DomainCode = d.Code AND s.StatusCode = sd.Code
    WHERE tr.LanguageCode IN (N'pt-PT', N'en-US');

    INSERT INTO dbo.StatusDefinitionTranslations (
        TenantId, StatusDomainId, StatusDefinitionId,
        LanguageCode, Name, Description
    )
    SELECT sd.TenantId, sd.StatusDomainId, sd.Id,
           l.LanguageCode,
           CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtName ELSE s.EnName END,
           CASE WHEN l.LanguageCode = N'pt-PT' THEN s.PtDescription ELSE s.EnDescription END
    FROM dbo.StatusDefinitions sd
    INNER JOIN dbo.StatusDomains d ON d.Id = sd.StatusDomainId AND d.IsDeleted = 0
    INNER JOIN @StatusDefinitionSeed s ON s.DomainCode = d.Code AND s.StatusCode = sd.Code
    CROSS JOIN (VALUES (N'pt-PT'), (N'en-US')) l(LanguageCode)
    WHERE sd.IsDeleted = 0
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.StatusDefinitionTranslations tr
          WHERE tr.TenantId = sd.TenantId
            AND tr.StatusDefinitionId = sd.Id
            AND tr.LanguageCode = l.LanguageCode
      );

    /* =========================================================
       12. ASSINATURAS INICIAIS
       ========================================================= */

    INSERT INTO dbo.Subscriptions (
        TenantId, StatusDefinitionId, StatusDomainId, SubscriptionPlanId,
        StripeId, AgreedAmount, BillingInterval, CurrencyCode,
        CurrentPeriodStart, CurrentPeriodEnd,
        TrialStart, TrialEnd, CancelAtPeriodEnd,
        CanceledAt, CancellationReason, StripeCustomerId,
        IsActive, IsDeleted, CreatedBy, CreatedAt
    )
    SELECT t.Id,
           sd.Id,
           d.Id,
           p.Id,
           NULL,
           COALESCE(p.PricePerMonth, p.PricePerYear, p.PricePerDay, p.PricePerHour, 0),
           N'MONTHLY',
           CONVERT(CHAR(3), p.Currency),
           @UtcNow,
           DATEADD(MONTH, 1, @UtcNow),
           @UtcNow,
           DATEADD(DAY, 30, @UtcNow),
           0,
           NULL, NULL, NULL,
           1, 0, @SeedActorId, @UtcNow
    FROM dbo.Tenants t
    INNER JOIN dbo.SubscriptionPlans p ON p.Code = N'FREE' AND p.IsDeleted = 0
    INNER JOIN dbo.StatusDomains d ON d.Code = N'SUBSCRIPTION' AND d.IsDeleted = 0
    INNER JOIN dbo.StatusDefinitions sd
        ON sd.TenantId = t.Id
       AND sd.StatusDomainId = d.Id
       AND sd.Code = N'ACTIVE'
       AND sd.IsDeleted = 0
    WHERE t.IsDeleted = 0
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.Subscriptions s
          WHERE s.TenantId = t.Id
            AND s.IsActive = 1
            AND s.IsDeleted = 0
      );

    /* =========================================================
       13. UTILIZADORES E PREFERÊNCIAS
       ========================================================= */

    DECLARE @UserSeed TABLE (
        TenantEmail NVARCHAR(255) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Email NVARCHAR(256) NOT NULL,
        NormalizedEmail NVARCHAR(256) NOT NULL,
        EmailConfirmed BIT NOT NULL,
        PhoneNumber NVARCHAR(50) NULL,
        PhoneNumberConfirmed BIT NOT NULL,
        PasswordHash NVARCHAR(500) NOT NULL,
        UrlImage NVARCHAR(500) NULL
    );

    INSERT INTO @UserSeed (
        TenantEmail, Name, Email, NormalizedEmail,
        EmailConfirmed, PhoneNumber, PhoneNumberConfirmed,
        PasswordHash, UrlImage
    )
    VALUES
        (N'contact@vianahub.pt', N'Dener Viana', N'viana.dener@gmail.com', N'VIANA.DENER@GMAIL.COM', 1, N'960268353', 0, N'AQAAAAIAAYagAAAAEAr88ZwhIrd69foEZO57diA8qdyfk3QkMPoCo9KxZ/CKlP1tFN7QPk6dHdMM2bQCNA==', N'www.gerit.pt/users/viana.dener.jpg'),
        (N'demo@gerit.pt', N'Admin', N'admin@geritapp.com', N'ADMIN@GERITAPP.COM', 0, NULL, 0, N'AQAAAAIAAYagAAAAEAr88ZwhIrd69foEZO57diA8qdyfk3QkMPoCo9KxZ/CKlP1tFN7QPk6dHdMM2bQCNA==', NULL);

    UPDATE u
       SET u.Name = s.Name,
           u.Email = s.Email,
           u.EmailConfirmed = s.EmailConfirmed,
           u.PhoneNumber = s.PhoneNumber,
           u.PhoneNumberConfirmed = s.PhoneNumberConfirmed,
           u.PasswordHash = s.PasswordHash,
           u.UrlImage = s.UrlImage,
           u.IsActive = 1,
           u.ModifiedBy = @SeedActorId,
           u.ModifiedAt = @UtcNow
    FROM dbo.Users u
    INNER JOIN dbo.Tenants t ON t.Id = u.TenantId AND t.IsDeleted = 0
    INNER JOIN @UserSeed s ON s.TenantEmail = t.Email AND s.NormalizedEmail = u.NormalizedEmail
    WHERE u.IsDeleted = 0;

    INSERT INTO dbo.Users (
        TenantId, Name, Email, NormalizedEmail,
        EmailConfirmed, PhoneNumber, PhoneNumberConfirmed,
        LastAccessAt, PasswordHash, UrlImage,
        IsActive, IsDeleted, CreatedBy, CreatedAt
    )
    SELECT t.Id, s.Name, s.Email, s.NormalizedEmail,
           s.EmailConfirmed, s.PhoneNumber, s.PhoneNumberConfirmed,
           NULL, s.PasswordHash, s.UrlImage,
           1, 0, @SeedActorId, @UtcNow
    FROM @UserSeed s
    INNER JOIN dbo.Tenants t ON t.Email = s.TenantEmail AND t.IsDeleted = 0
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Users u
        WHERE u.TenantId = t.Id
          AND u.NormalizedEmail = s.NormalizedEmail
          AND u.IsDeleted = 0
    );

    INSERT INTO dbo.UserPreferences (
        TenantId, UserId, Appearance, CurrencyCode, Locale, Timezone,
        DateFormat, TimeFormat, DayStart, DayEnd,
        EmailNewsletter, EmailWeeklyReport, EmailApproval,
        EmailAlerts, EmailReminders, EmailPlanner,
        IsActive, IsDeleted, CreatedBy, CreatedAt
    )
    SELECT u.TenantId, u.Id,
           N'light', N'EUR', N'pt-PT', N'Europe/Lisbon',
           N'DD-MM-YYYY', N'24h', CONVERT(TIME(0), '09:00'), CONVERT(TIME(0), '18:00'),
           0, 0, 0, 1, 1, 1,
           1, 0, @SeedActorId, @UtcNow
    FROM dbo.Users u
    INNER JOIN @UserSeed s ON s.NormalizedEmail = u.NormalizedEmail
    INNER JOIN dbo.Tenants t ON t.Id = u.TenantId AND t.Email = s.TenantEmail
    WHERE u.IsDeleted = 0
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.UserPreferences p
          WHERE p.TenantId = u.TenantId
            AND p.UserId = u.Id
            AND p.IsActive = 1
            AND p.IsDeleted = 0
      );

    /* =========================================================
       14. PERMISSÕES BACKOFFICE E USER ROLES
       ========================================================= */

    ;WITH BackOfficeRoles AS (
        SELECT t.Id AS TenantId, r.Id AS RoleId
        FROM dbo.Tenants t
        INNER JOIN dbo.Roles r
            ON r.TenantId = t.Id
           AND r.Name = N'BackOffice'
           AND r.IsDeleted = 0
        WHERE t.Email IN (N'contact@vianahub.pt', N'demo@gerit.pt')
          AND t.IsDeleted = 0
    )
    INSERT INTO dbo.RolePermissions (TenantId, RoleId, ResourceId, ActionId)
    SELECT bor.TenantId, bor.RoleId, res.Id, act.Id
    FROM BackOfficeRoles bor
    CROSS JOIN dbo.Resources res
    CROSS JOIN dbo.Actions act
    WHERE res.IsActive = 1 AND res.IsDeleted = 0
      AND act.IsActive = 1 AND act.IsDeleted = 0
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.RolePermissions rp
          WHERE rp.TenantId = bor.TenantId
            AND rp.RoleId = bor.RoleId
            AND rp.ResourceId = res.Id
            AND rp.ActionId = act.Id
      );

    ;WITH TargetUsers AS (
        SELECT t.Id AS TenantId, u.Id AS UserId, r.Id AS BackOfficeRoleId
        FROM @UserSeed s
        INNER JOIN dbo.Tenants t ON t.Email = s.TenantEmail AND t.IsDeleted = 0
        INNER JOIN dbo.Users u
            ON u.TenantId = t.Id
           AND u.NormalizedEmail = s.NormalizedEmail
           AND u.IsDeleted = 0
        INNER JOIN dbo.Roles r
            ON r.TenantId = t.Id
           AND r.Name = N'BackOffice'
           AND r.IsDeleted = 0
    )
    DELETE ur
    FROM dbo.UserRoles ur
    INNER JOIN TargetUsers tu
        ON tu.TenantId = ur.TenantId
       AND tu.UserId = ur.UserId
    WHERE ur.RoleId <> tu.BackOfficeRoleId;

    ;WITH TargetUsers AS (
        SELECT t.Id AS TenantId, u.Id AS UserId, r.Id AS RoleId
        FROM @UserSeed s
        INNER JOIN dbo.Tenants t ON t.Email = s.TenantEmail AND t.IsDeleted = 0
        INNER JOIN dbo.Users u
            ON u.TenantId = t.Id
           AND u.NormalizedEmail = s.NormalizedEmail
           AND u.IsDeleted = 0
        INNER JOIN dbo.Roles r
            ON r.TenantId = t.Id
           AND r.Name = N'BackOffice'
           AND r.IsDeleted = 0
    )
    INSERT INTO dbo.UserRoles (TenantId, UserId, RoleId)
    SELECT tu.TenantId, tu.UserId, tu.RoleId
    FROM TargetUsers tu
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.UserRoles ur
        WHERE ur.TenantId = tu.TenantId
          AND ur.UserId = tu.UserId
          AND ur.RoleId = tu.RoleId
    );

    /* =========================================================
       15. EQUIPAS INICIAIS
       ========================================================= */

    DECLARE @TeamSeed TABLE (
        Name NVARCHAR(150) PRIMARY KEY,
        Description NVARCHAR(500) NOT NULL
    );

    INSERT INTO @TeamSeed (Name, Description)
    VALUES
        (N'Primavera', N'Equipa responsável pelo período da Primavera'),
        (N'Verão', N'Equipa responsável pelo período do Verão'),
        (N'Outono', N'Equipa responsável pelo período do Outono'),
        (N'Inverno', N'Equipa responsável pelo período do Inverno');

    INSERT INTO dbo.Teams (
        TenantId, Name, Description,
        IsActive, IsDeleted, CreatedBy, CreatedAt
    )
    SELECT t.Id, s.Name, s.Description,
           1, 0, @SeedActorId, @UtcNow
    FROM dbo.Tenants t
    CROSS JOIN @TeamSeed s
    WHERE t.IsDeleted = 0
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.Teams tm
          WHERE tm.TenantId = t.Id
            AND tm.Name = s.Name
            AND tm.IsDeleted = 0
      );

    /* =========================================================
       16. CLIENTES DE DEMONSTRAÇÃO
       ========================================================= */

    DECLARE @ClientSeed TABLE (
        TenantEmail NVARCHAR(255) NOT NULL,
        SeedKey NVARCHAR(50) NOT NULL,
        LegacyClientType INT NOT NULL,
        AcquisitionSourceCode NVARCHAR(50) NOT NULL,
        Note NVARCHAR(1000) NOT NULL,

        FullName NVARCHAR(500) NULL,
        FirstName NVARCHAR(100) NULL,
        LastName NVARCHAR(100) NULL,
        LegalName NVARCHAR(500) NULL,
        TradeName NVARCHAR(200) NULL,

        PhoneNumber NVARCHAR(50) NULL,
        CellPhoneNumber NVARCHAR(50) NULL,
        IsWhatsapp BIT NOT NULL,
        Email NVARCHAR(320) NULL,

        BirthDate DATE NULL,
        Gender NVARCHAR(30) NULL,
        DocumentTypeCode NVARCHAR(50) NULL,
        DocumentNumber NVARCHAR(100) NULL,
        Nationality NVARCHAR(100) NULL,

        Site NVARCHAR(500) NULL,
        CompanyRegistration NVARCHAR(100) NULL,
        CAE NVARCHAR(20) NULL,
        NumberOfEmployee INT NULL,
        LegalRepresentative NVARCHAR(150) NULL,

        TaxNumber NVARCHAR(20) NOT NULL,
        VatNumber NVARCHAR(20) NULL,
        FiscalEmail NVARCHAR(255) NULL,
        IsVatRegistered BIT NOT NULL,

        AddressTypeCode NVARCHAR(50) NOT NULL,
        Street NVARCHAR(200) NOT NULL,
        Neighborhood NVARCHAR(100) NULL,
        City NVARCHAR(100) NOT NULL,
        District NVARCHAR(100) NULL,
        PostalCode NVARCHAR(20) NOT NULL,
        StreetNumber NVARCHAR(20) NULL,
        Complement NVARCHAR(100) NULL,
        ContactName NVARCHAR(150) NOT NULL,
        ClientId INT NULL,

        PRIMARY KEY (TenantEmail, SeedKey)
    );

    INSERT INTO @ClientSeed (
        TenantEmail, SeedKey, LegacyClientType, AcquisitionSourceCode, Note,
        FullName, FirstName, LastName, LegalName, TradeName,
        PhoneNumber, CellPhoneNumber, IsWhatsapp, Email,
        BirthDate, Gender, DocumentTypeCode, DocumentNumber, Nationality,
        Site, CompanyRegistration, CAE, NumberOfEmployee, LegalRepresentative,
        TaxNumber, VatNumber, FiscalEmail, IsVatRegistered,
        AddressTypeCode, Street, Neighborhood, City, District, PostalCode,
        StreetNumber, Complement, ContactName
    )
    VALUES
        (N'contact@vianahub.pt', N'Client-001', 1, N'INSTAGRAM', N'[InitialSeed:Client-001] Pessoa Singular - Mariana Costa', N'Mariana Costa', N'Mariana', N'Costa', NULL, NULL, N'289123401', N'934123401', 1, N'mariana.costa@example.pt', '1992-04-18', N'Feminino', N'CITIZEN_CARD', N'12345678', N'Portugal', NULL, NULL, NULL, NULL, NULL, N'245789321', NULL, N'mariana.costa@example.pt', 0, N'RESIDENTIAL', N'Rua de São Luís', N'Centro', N'Faro', N'Faro', N'8000-285', N'14', N'2.º Esq.', N'Mariana Costa'),
        (N'contact@vianahub.pt', N'Client-002', 1, N'GOOGLE', N'[InitialSeed:Client-002] Pessoa Singular - João Martins', N'João Martins', N'João', N'Martins', NULL, NULL, N'282123402', N'935123402', 1, N'joao.martins@example.pt', '1987-09-03', N'Masculino', N'CITIZEN_CARD', N'23456789', N'Portugal', NULL, NULL, NULL, NULL, NULL, N'246789322', NULL, N'joao.martins@example.pt', 0, N'RESIDENTIAL', N'Avenida da República', N'Centro', N'Portimão', N'Faro', N'8500-300', N'52', N'1.º Dt.', N'João Martins'),
        (N'contact@vianahub.pt', N'Client-003', 2, N'WHATSAPP', N'[InitialSeed:Client-003] Recibos Verdes - Sofia Almeida', N'Sofia Almeida', N'Sofia', N'Almeida', NULL, NULL, N'213123403', N'936123403', 1, N'sofia.almeida@example.pt', '1990-01-26', N'Feminino', N'CITIZEN_CARD', N'34567890', N'Portugal', NULL, NULL, NULL, NULL, NULL, N'247789323', N'PT247789323', N'sofia.almeida@example.pt', 1, N'RESIDENTIAL', N'Rua do Alecrim', N'Misericórdia', N'Lisboa', N'Lisboa', N'1200-014', N'21', N'3.º', N'Sofia Almeida'),
        (N'contact@vianahub.pt', N'Client-004', 3, N'LINKEDIN', N'[InitialSeed:Client-004] Freelancer - Tiago Ferreira', N'Tiago Ferreira', N'Tiago', N'Ferreira', NULL, NULL, N'222123404', N'937123404', 1, N'tiago.ferreira@example.pt', '1985-11-12', N'Masculino', N'CITIZEN_CARD', N'45678901', N'Portugal', NULL, NULL, NULL, NULL, NULL, N'248789324', N'PT248789324', N'tiago.ferreira@example.pt', 1, N'RESIDENTIAL', N'Rua de Cedofeita', N'Cedofeita', N'Porto', N'Porto', N'4050-174', N'88', N'4.º Esq.', N'Tiago Ferreira'),
        (N'contact@vianahub.pt', N'Client-005', 4, N'FACEBOOK', N'[InitialSeed:Client-005] Pessoa Jurídica - Algarve Tech Solutions Lda', NULL, NULL, NULL, N'Algarve Tech Solutions Lda', N'Algarve Tech', N'289123405', N'938123405', 1, N'geral@algarvetech.example.pt', NULL, NULL, NULL, NULL, NULL, N'https://algarvetech.example.pt', N'514789325', N'62010', 18, N'Ricardo Neves', N'514789325', N'PT514789325', N'fiscal@algarvetech.example.pt', 1, N'COMMERCIAL', N'Rua do Comércio', N'Baixa', N'Loulé', N'Faro', N'8100-536', N'7', N'Loja A', N'Ricardo Neves'),
        (N'contact@vianahub.pt', N'Client-006', 4, N'YOUTUBE', N'[InitialSeed:Client-006] Pessoa Jurídica - Lisboa Digital Services Lda', NULL, NULL, NULL, N'Lisboa Digital Services Lda', N'Lisboa Digital', N'211123406', N'939123406', 1, N'contacto@lisboadigital.example.pt', NULL, NULL, NULL, NULL, NULL, N'https://lisboadigital.example.pt', N'514789326', N'62020', 32, N'Patrícia Ramos', N'514789326', N'PT514789326', N'fiscal@lisboadigital.example.pt', 1, N'COMMERCIAL', N'Avenida da Liberdade', N'Santo António', N'Lisboa', N'Lisboa', N'1250-096', N'110', N'5.º', N'Patrícia Ramos'),
        (N'contact@vianahub.pt', N'Client-007', 4, N'TIKTOK', N'[InitialSeed:Client-007] Pessoa Jurídica - Porto Creative Agency Lda', NULL, NULL, NULL, N'Porto Creative Agency Lda', N'Porto Creative', N'222123407', N'930123407', 1, N'hello@portocreative.example.pt', NULL, NULL, NULL, NULL, NULL, N'https://portocreative.example.pt', N'514789327', N'73110', 14, N'Helena Pinto', N'514789327', N'PT514789327', N'fiscal@portocreative.example.pt', 1, N'COMMERCIAL', N'Rua de Santa Catarina', N'Santo Ildefonso', N'Porto', N'Porto', N'4000-447', N'312', N'Sala 2', N'Helena Pinto'),
        (N'contact@vianahub.pt', N'Client-008', 4, N'EVENTS', N'[InitialSeed:Client-008] Pessoa Jurídica - Coimbra Business Consulting Lda', NULL, NULL, NULL, N'Coimbra Business Consulting Lda', N'Coimbra Consulting', N'239123408', N'931123408', 1, N'geral@coimbraconsulting.example.pt', NULL, NULL, NULL, NULL, NULL, N'https://coimbraconsulting.example.pt', N'514789328', N'70220', 22, N'Miguel Santos', N'514789328', N'PT514789328', N'fiscal@coimbraconsulting.example.pt', 1, N'COMMERCIAL', N'Rua Ferreira Borges', N'Baixa', N'Coimbra', N'Coimbra', N'3000-179', N'45', N'2.º', N'Miguel Santos'),
        (N'contact@vianahub.pt', N'Client-009', 5, N'INSTAGRAM', N'[InitialSeed:Client-009] Sociedade Unipessoal por Quotas - Braga Web Studio Unipessoal Lda', NULL, NULL, NULL, N'Braga Web Studio Unipessoal Lda', N'Braga Web Studio', N'253123409', N'932123409', 1, N'geral@bragawebstudio.example.pt', NULL, NULL, NULL, NULL, NULL, N'https://bragawebstudio.example.pt', N'516789329', N'62010', 5, N'Inês Carvalho', N'516789329', N'PT516789329', N'fiscal@bragawebstudio.example.pt', 1, N'COMMERCIAL', N'Avenida Central', N'Centro', N'Braga', N'Braga', N'4710-229', N'91', N'Escritório 3', N'Inês Carvalho'),
        (N'contact@vianahub.pt', N'Client-010', 5, N'LINKEDIN', N'[InitialSeed:Client-010] Sociedade Unipessoal por Quotas - Aveiro Automation Unipessoal Lda', NULL, NULL, NULL, N'Aveiro Automation Unipessoal Lda', N'Aveiro Automation', N'234123410', N'933123410', 1, N'geral@aveiroautomation.example.pt', NULL, NULL, NULL, NULL, NULL, N'https://aveiroautomation.example.pt', N'516789330', N'71120', 7, N'Pedro Lima', N'516789330', N'PT516789330', N'fiscal@aveiroautomation.example.pt', 1, N'COMMERCIAL', N'Rua do Clube dos Galitos', N'Centro', N'Aveiro', N'Aveiro', N'3810-164', N'18', N'Sala 4', N'Pedro Lima'),
        (N'contact@vianahub.pt', N'Client-011', 5, N'GOOGLE', N'[InitialSeed:Client-011] Sociedade Unipessoal por Quotas - Évora Design Lab Unipessoal Lda', NULL, NULL, NULL, N'Évora Design Lab Unipessoal Lda', N'Évora Design Lab', N'266123411', N'934123411', 1, N'geral@evoradesignlab.example.pt', NULL, NULL, NULL, NULL, NULL, N'https://evoradesignlab.example.pt', N'516789331', N'74100', 4, N'Carla Mendes', N'516789331', N'PT516789331', N'fiscal@evoradesignlab.example.pt', 1, N'COMMERCIAL', N'Rua da República', N'Centro Histórico', N'Évora', N'Évora', N'7000-656', N'27', N'Loja 1', N'Carla Mendes'),
        (N'contact@vianahub.pt', N'Client-012', 5, N'REFERRAL', N'[InitialSeed:Client-012] Sociedade Unipessoal por Quotas - Viseu Data Services Unipessoal Lda', NULL, NULL, NULL, N'Viseu Data Services Unipessoal Lda', N'Viseu Data', N'232123412', N'935123412', 1, N'geral@viseudata.example.pt', NULL, NULL, NULL, NULL, NULL, N'https://viseudata.example.pt', N'516789332', N'63110', 6, N'André Rocha', N'516789332', N'PT516789332', N'fiscal@viseudata.example.pt', 1, N'COMMERCIAL', N'Rua Formosa', N'Centro', N'Viseu', N'Viseu', N'3500-135', N'60', N'1.º', N'André Rocha'),
        (N'demo@gerit.pt', N'GD-001', 1, N'INSTAGRAM', N'[GeritDemoSeed:GD-001] Pessoa Singular - Ana Ribeiro', N'Ana Ribeiro', N'Ana', N'Ribeiro', NULL, NULL, N'218100001', N'910100001', 1, N'ana.ribeiro.demo@gerit.pt', '1991-03-14', N'Feminino', N'CITIZEN_CARD', N'GD100001', N'Portugal', NULL, NULL, NULL, NULL, NULL, N'245900001', NULL, N'ana.ribeiro.demo@gerit.pt', 0, N'RESIDENTIAL', N'Rua das Flores', N'Centro', N'Lisboa', N'Lisboa', N'1200-195', N'10', N'2.º Esq.', N'Ana Ribeiro'),
        (N'demo@gerit.pt', N'GD-002', 1, N'GOOGLE', N'[GeritDemoSeed:GD-002] Pessoa Singular - Miguel Correia', N'Miguel Correia', N'Miguel', N'Correia', NULL, NULL, N'222100002', N'910100002', 1, N'miguel.correia.demo@gerit.pt', '1986-07-22', N'Masculino', N'CITIZEN_CARD', N'GD100002', N'Portugal', NULL, NULL, NULL, NULL, NULL, N'245900002', NULL, N'miguel.correia.demo@gerit.pt', 0, N'RESIDENTIAL', N'Rua de Cedofeita', N'Cedofeita', N'Porto', N'Porto', N'4050-174', N'84', N'3.º Dt.', N'Miguel Correia'),
        (N'demo@gerit.pt', N'GD-003', 2, N'WHATSAPP', N'[GeritDemoSeed:GD-003] Recibos Verdes - Catarina Lopes', N'Catarina Lopes', N'Catarina', N'Lopes', NULL, NULL, N'239100003', N'910100003', 1, N'catarina.lopes.demo@gerit.pt', '1990-11-05', N'Feminino', N'CITIZEN_CARD', N'GD100003', N'Portugal', NULL, NULL, NULL, NULL, NULL, N'245900003', N'PT245900003', N'catarina.lopes.demo@gerit.pt', 1, N'RESIDENTIAL', N'Rua Ferreira Borges', N'Baixa', N'Coimbra', N'Coimbra', N'3000-179', N'36', N'1.º', N'Catarina Lopes'),
        (N'demo@gerit.pt', N'GD-004', 3, N'LINKEDIN', N'[GeritDemoSeed:GD-004] Freelancer - Rui Fernandes', N'Rui Fernandes', N'Rui', N'Fernandes', NULL, NULL, N'253100004', N'910100004', 1, N'rui.fernandes.demo@gerit.pt', '1988-01-19', N'Masculino', N'CITIZEN_CARD', N'GD100004', N'Portugal', NULL, NULL, NULL, NULL, NULL, N'245900004', N'PT245900004', N'rui.fernandes.demo@gerit.pt', 1, N'RESIDENTIAL', N'Avenida Central', N'Centro', N'Braga', N'Braga', N'4710-229', N'75', N'Sala 2', N'Rui Fernandes'),
        (N'demo@gerit.pt', N'GD-005', 1, N'REFERRAL', N'[GeritDemoSeed:GD-005] Pessoa Singular - Beatriz Sousa', N'Beatriz Sousa', N'Beatriz', N'Sousa', NULL, NULL, N'289100005', N'910100005', 1, N'beatriz.sousa.demo@gerit.pt', '1995-05-30', N'Feminino', N'CITIZEN_CARD', N'GD100005', N'Portugal', NULL, NULL, NULL, NULL, NULL, N'245900005', NULL, N'beatriz.sousa.demo@gerit.pt', 0, N'RESIDENTIAL', N'Rua de Santo António', N'Centro', N'Faro', N'Faro', N'8000-283', N'18', N'R/C', N'Beatriz Sousa'),
        (N'demo@gerit.pt', N'GD-006', 2, N'FACEBOOK', N'[GeritDemoSeed:GD-006] Recibos Verdes - Diogo Matos', N'Diogo Matos', N'Diogo', N'Matos', NULL, NULL, N'234100006', N'910100006', 1, N'diogo.matos.demo@gerit.pt', '1984-10-11', N'Masculino', N'CITIZEN_CARD', N'GD100006', N'Portugal', NULL, NULL, NULL, NULL, NULL, N'245900006', N'PT245900006', N'diogo.matos.demo@gerit.pt', 1, N'RESIDENTIAL', N'Rua Direita', N'Centro', N'Aveiro', N'Aveiro', N'3810-005', N'25', N'2.º', N'Diogo Matos'),
        (N'demo@gerit.pt', N'GD-007', 3, N'TIKTOK', N'[GeritDemoSeed:GD-007] Freelancer - Inês Carvalho', N'Inês Carvalho', N'Inês', N'Carvalho', NULL, NULL, N'266100007', N'910100007', 1, N'ines.carvalho.demo@gerit.pt', '1993-12-08', N'Feminino', N'CITIZEN_CARD', N'GD100007', N'Portugal', NULL, NULL, NULL, NULL, NULL, N'245900007', N'PT245900007', N'ines.carvalho.demo@gerit.pt', 1, N'RESIDENTIAL', N'Rua da República', N'Centro Histórico', N'Évora', N'Évora', N'7000-656', N'42', N'1.º Esq.', N'Inês Carvalho'),
        (N'demo@gerit.pt', N'GD-008', 1, N'YOUTUBE', N'[GeritDemoSeed:GD-008] Pessoa Singular - Pedro Nunes', N'Pedro Nunes', N'Pedro', N'Nunes', NULL, NULL, N'232100008', N'910100008', 1, N'pedro.nunes.demo@gerit.pt', '1982-06-27', N'Masculino', N'CITIZEN_CARD', N'GD100008', N'Portugal', NULL, NULL, NULL, NULL, NULL, N'245900008', NULL, N'pedro.nunes.demo@gerit.pt', 0, N'RESIDENTIAL', N'Rua Formosa', N'Centro', N'Viseu', N'Viseu', N'3500-135', N'61', N'4.º', N'Pedro Nunes'),
        (N'demo@gerit.pt', N'GD-009', 4, N'LINKEDIN', N'[GeritDemoSeed:GD-009] Empresa real em Portugal - EDP - Energias de Portugal, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'EDP - Energias de Portugal, S.A.', N'EDP', N'351210009', N'911100009', 1, N'demo+edp@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.edp.com', N'500900009', N'35110', 12000, N'Responsável Demo EDP', N'507900009', N'PT507900009', N'fiscal+edp@gerit.pt', 1, N'COMMERCIAL', N'Avenida 24 de Julho', N'Santos', N'Lisboa', N'Lisboa', N'1200-868', N'12', N'Edifício Demo', N'Responsável Demo EDP'),
        (N'demo@gerit.pt', N'GD-010', 4, N'GOOGLE', N'[GeritDemoSeed:GD-010] Empresa real em Portugal - Galp Energia, SGPS, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Galp Energia, SGPS, S.A.', N'Galp', N'351210010', N'911100010', 1, N'demo+galp@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.galp.com', N'500900010', N'46711', 6000, N'Responsável Demo Galp', N'507900010', N'PT507900010', N'fiscal+galp@gerit.pt', 1, N'COMMERCIAL', N'Rua Tomás da Fonseca', N'São Domingos de Benfica', N'Lisboa', N'Lisboa', N'1600-209', N'15', N'Torre Demo', N'Responsável Demo Galp'),
        (N'demo@gerit.pt', N'GD-011', 4, N'EVENTS', N'[GeritDemoSeed:GD-011] Empresa real em Portugal - Jerónimo Martins, SGPS, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Jerónimo Martins, SGPS, S.A.', N'Jerónimo Martins', N'351210011', N'911100011', 1, N'demo+jeronimo.martins@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.jeronimomartins.com', N'500900011', N'70100', 30000, N'Responsável Demo JM', N'507900011', N'PT507900011', N'fiscal+jeronimo.martins@gerit.pt', 1, N'COMMERCIAL', N'Rua Actor António Silva', N'Alta de Lisboa', N'Lisboa', N'Lisboa', N'1600-404', N'7', N'Piso Demo', N'Responsável Demo JM'),
        (N'demo@gerit.pt', N'GD-012', 4, N'LINKEDIN', N'[GeritDemoSeed:GD-012] Empresa real em Portugal - Sonae, SGPS, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Sonae, SGPS, S.A.', N'Sonae', N'351220012', N'911100012', 1, N'demo+sonae@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.sonae.pt', N'500900012', N'70100', 45000, N'Responsável Demo Sonae', N'507900012', N'PT507900012', N'fiscal+sonae@gerit.pt', 1, N'COMMERCIAL', N'Lugar do Espido', N'Via Norte', N'Maia', N'Porto', N'4470-177', N'0', N'Bloco Demo', N'Responsável Demo Sonae'),
        (N'demo@gerit.pt', N'GD-013', 4, N'GOOGLE', N'[GeritDemoSeed:GD-013] Empresa real em Portugal - NOS, SGPS, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'NOS, SGPS, S.A.', N'NOS', N'351210013', N'911100013', 1, N'demo+nos@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.nos.pt', N'500900013', N'64202', 2000, N'Responsável Demo NOS', N'507900013', N'PT507900013', N'fiscal+nos@gerit.pt', 1, N'COMMERCIAL', N'Rua Actor António Silva', N'Lumiar', N'Lisboa', N'Lisboa', N'1600-404', N'9', N'Edifício Demo', N'Responsável Demo NOS'),
        (N'demo@gerit.pt', N'GD-014', 4, N'EVENTS', N'[GeritDemoSeed:GD-014] Empresa real em Portugal - Mota-Engil, SGPS, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Mota-Engil, SGPS, S.A.', N'Mota-Engil', N'351220014', N'911100014', 1, N'demo+mota.engil@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.mota-engil.com', N'500900014', N'42990', 10000, N'Responsável Demo Mota-Engil', N'507900014', N'PT507900014', N'fiscal+mota.engil@gerit.pt', 1, N'COMMERCIAL', N'Rua do Rego Lameiro', N'Campanhã', N'Porto', N'Porto', N'4300-454', N'38', N'Piso Demo', N'Responsável Demo Mota-Engil'),
        (N'demo@gerit.pt', N'GD-015', 4, N'GOOGLE', N'[GeritDemoSeed:GD-015] Empresa real em Portugal - REN - Redes Energéticas Nacionais, SGPS, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'REN - Redes Energéticas Nacionais, SGPS, S.A.', N'REN', N'351210015', N'911100015', 1, N'demo+ren@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.ren.pt', N'500900015', N'35120', 700, N'Responsável Demo REN', N'507900015', N'PT507900015', N'fiscal+ren@gerit.pt', 1, N'COMMERCIAL', N'Avenida dos Estados Unidos da América', N'Alvalade', N'Lisboa', N'Lisboa', N'1749-061', N'55', N'Piso Demo', N'Responsável Demo REN'),
        (N'demo@gerit.pt', N'GD-016', 4, N'WHATSAPP', N'[GeritDemoSeed:GD-016] Empresa real em Portugal - CTT - Correios de Portugal, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'CTT - Correios de Portugal, S.A.', N'CTT', N'351210016', N'911100016', 1, N'demo+ctt@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.ctt.pt', N'500900016', N'53100', 12000, N'Responsável Demo CTT', N'507900016', N'PT507900016', N'fiscal+ctt@gerit.pt', 1, N'COMMERCIAL', N'Avenida D. João II', N'Parque das Nações', N'Lisboa', N'Lisboa', N'1999-001', N'13', N'Torre Demo', N'Responsável Demo CTT'),
        (N'demo@gerit.pt', N'GD-017', 4, N'FACEBOOK', N'[GeritDemoSeed:GD-017] Empresa real em Portugal - Banco Comercial Português, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Banco Comercial Português, S.A.', N'Millennium bcp', N'351210017', N'911100017', 1, N'demo+bcp@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.millenniumbcp.pt', N'500900017', N'64190', 7000, N'Responsável Demo BCP', N'507900017', N'PT507900017', N'fiscal+bcp@gerit.pt', 1, N'COMMERCIAL', N'Praça D. João I', N'Baixa', N'Porto', N'Porto', N'4000-295', N'28', N'Agência Demo', N'Responsável Demo BCP'),
        (N'demo@gerit.pt', N'GD-018', 4, N'LINKEDIN', N'[GeritDemoSeed:GD-018] Empresa real em Portugal - Corticeira Amorim, SGPS, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Corticeira Amorim, SGPS, S.A.', N'Corticeira Amorim', N'351220018', N'911100018', 1, N'demo+amorim@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.amorim.com', N'500900018', N'16294', 4000, N'Responsável Demo Amorim', N'507900018', N'PT507900018', N'fiscal+amorim@gerit.pt', 1, N'COMMERCIAL', N'Rua de Meladas', N'Mozelos', N'Santa Maria da Feira', N'Aveiro', N'4535-186', N'260', N'Unidade Demo', N'Responsável Demo Amorim'),
        (N'demo@gerit.pt', N'GD-019', 4, N'GOOGLE', N'[GeritDemoSeed:GD-019] Empresa real em Portugal - The Navigator Company, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'The Navigator Company, S.A.', N'The Navigator Company', N'351210019', N'911100019', 1, N'demo+navigator@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.thenavigatorcompany.com', N'500900019', N'17120', 3000, N'Responsável Demo Navigator', N'507900019', N'PT507900019', N'fiscal+navigator@gerit.pt', 1, N'COMMERCIAL', N'Avenida Fontes Pereira de Melo', N'Avenidas Novas', N'Lisboa', N'Lisboa', N'1050-120', N'27', N'Escritório Demo', N'Responsável Demo Navigator'),
        (N'demo@gerit.pt', N'GD-020', 4, N'EVENTS', N'[GeritDemoSeed:GD-020] Empresa real em Portugal - Semapa - Sociedade de Investimento e Gestão, SGPS, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Semapa - Sociedade de Investimento e Gestão, SGPS, S.A.', N'Semapa', N'351210020', N'911100020', 1, N'demo+semapa@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.semapa.pt', N'500900020', N'64202', 200, N'Responsável Demo Semapa', N'507900020', N'PT507900020', N'fiscal+semapa@gerit.pt', 1, N'COMMERCIAL', N'Avenida Fontes Pereira de Melo', N'Avenidas Novas', N'Lisboa', N'Lisboa', N'1050-121', N'14', N'Piso Demo', N'Responsável Demo Semapa'),
        (N'demo@gerit.pt', N'GD-021', 4, N'LINKEDIN', N'[GeritDemoSeed:GD-021] Empresa real em Portugal - Altri, SGPS, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Altri, SGPS, S.A.', N'Altri', N'351220021', N'911100021', 1, N'demo+altri@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.altri.pt', N'500900021', N'17110', 800, N'Responsável Demo Altri', N'507900021', N'PT507900021', N'fiscal+altri@gerit.pt', 1, N'COMMERCIAL', N'Rua Manuel Pinto de Azevedo', N'Ramalde', N'Porto', N'Porto', N'4100-320', N'818', N'Escritório Demo', N'Responsável Demo Altri'),
        (N'demo@gerit.pt', N'GD-022', 4, N'GOOGLE', N'[GeritDemoSeed:GD-022] Empresa real em Portugal - Ibersol, SGPS, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Ibersol, SGPS, S.A.', N'Ibersol', N'351220022', N'911100022', 1, N'demo+ibersol@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.ibersol.pt', N'500900022', N'56101', 6000, N'Responsável Demo Ibersol', N'507900022', N'PT507900022', N'fiscal+ibersol@gerit.pt', 1, N'COMMERCIAL', N'Praça do Bom Sucesso', N'Boavista', N'Porto', N'Porto', N'4150-146', N'105', N'Loja Demo', N'Responsável Demo Ibersol'),
        (N'demo@gerit.pt', N'GD-023', 4, N'WHATSAPP', N'[GeritDemoSeed:GD-023] Empresa real em Portugal - EDP Renováveis, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'EDP Renováveis, S.A.', N'EDP Renováveis', N'351210023', N'911100023', 1, N'demo+edpr@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.edpr.com', N'500900023', N'35113', 2500, N'Responsável Demo EDPR', N'507900023', N'PT507900023', N'fiscal+edpr@gerit.pt', 1, N'COMMERCIAL', N'Avenida da Boavista', N'Boavista', N'Porto', N'Porto', N'4100-130', N'3433', N'Piso Demo', N'Responsável Demo EDPR'),
        (N'demo@gerit.pt', N'GD-024', 4, N'EVENTS', N'[GeritDemoSeed:GD-024] Empresa real em Portugal - TAP - Transportes Aéreos Portugueses, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'TAP - Transportes Aéreos Portugueses, S.A.', N'TAP Air Portugal', N'351210024', N'911100024', 1, N'demo+tap@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.flytap.com', N'500900024', N'51100', 8000, N'Responsável Demo TAP', N'507900024', N'PT507900024', N'fiscal+tap@gerit.pt', 1, N'COMMERCIAL', N'Aeroporto Humberto Delgado', N'Olivais', N'Lisboa', N'Lisboa', N'1700-008', N'0', N'Terminal Demo', N'Responsável Demo TAP'),
        (N'demo@gerit.pt', N'GD-025', 4, N'GOOGLE', N'[GeritDemoSeed:GD-025] Empresa real em Portugal - Caixa Geral de Depósitos, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Caixa Geral de Depósitos, S.A.', N'CGD', N'351210025', N'911100025', 1, N'demo+cgd@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.cgd.pt', N'500900025', N'64190', 6000, N'Responsável Demo CGD', N'507900025', N'PT507900025', N'fiscal+cgd@gerit.pt', 1, N'COMMERCIAL', N'Avenida João XXI', N'Areeiro', N'Lisboa', N'Lisboa', N'1000-300', N'63', N'Sede Demo', N'Responsável Demo CGD'),
        (N'demo@gerit.pt', N'GD-026', 4, N'LINKEDIN', N'[GeritDemoSeed:GD-026] Empresa real em Portugal - Vodafone Portugal - Comunicações Pessoais, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Vodafone Portugal - Comunicações Pessoais, S.A.', N'Vodafone Portugal', N'351210026', N'911100026', 1, N'demo+vodafone@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.vodafone.pt', N'500900026', N'61200', 1500, N'Responsável Demo Vodafone', N'507900026', N'PT507900026', N'fiscal+vodafone@gerit.pt', 1, N'COMMERCIAL', N'Avenida D. João II', N'Parque das Nações', N'Lisboa', N'Lisboa', N'1998-017', N'36', N'Piso Demo', N'Responsável Demo Vodafone'),
        (N'demo@gerit.pt', N'GD-027', 4, N'GOOGLE', N'[GeritDemoSeed:GD-027] Empresa real em Portugal - Altice Portugal, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Altice Portugal, S.A.', N'Altice Portugal', N'351210027', N'911100027', 1, N'demo+altice@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.telecom.pt', N'500900027', N'61100', 8000, N'Responsável Demo Altice', N'507900027', N'PT507900027', N'fiscal+altice@gerit.pt', 1, N'COMMERCIAL', N'Avenida Fontes Pereira de Melo', N'Avenidas Novas', N'Lisboa', N'Lisboa', N'1069-300', N'40', N'Escritório Demo', N'Responsável Demo Altice'),
        (N'demo@gerit.pt', N'GD-028', 4, N'FACEBOOK', N'[GeritDemoSeed:GD-028] Empresa real em Portugal - Super Bock Bebidas, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Super Bock Bebidas, S.A.', N'Super Bock Group', N'351220028', N'911100028', 1, N'demo+superbock@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.superbockgroup.com', N'500900028', N'11050', 1200, N'Responsável Demo Super Bock', N'507900028', N'PT507900028', N'fiscal+superbock@gerit.pt', 1, N'COMMERCIAL', N'Via Norte', N'Leça do Balio', N'Matosinhos', N'Porto', N'4465-764', N'0', N'Unidade Demo', N'Responsável Demo Super Bock'),
        (N'demo@gerit.pt', N'GD-029', 4, N'EVENTS', N'[GeritDemoSeed:GD-029] Empresa real em Portugal - Delta Cafés, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Delta Cafés, S.A.', N'Delta Cafés', N'351245029', N'911100029', 1, N'demo+delta@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.deltacafes.pt', N'500900029', N'10830', 3000, N'Responsável Demo Delta', N'507900029', N'PT507900029', N'fiscal+delta@gerit.pt', 1, N'COMMERCIAL', N'Avenida Calouste Gulbenkian', N'Campo Maior', N'Campo Maior', N'Portalegre', N'7370-025', N'0', N'Unidade Demo', N'Responsável Demo Delta'),
        (N'demo@gerit.pt', N'GD-030', 4, N'REFERRAL', N'[GeritDemoSeed:GD-030] Empresa real em Portugal - Sumol+Compal Marcas, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Sumol+Compal Marcas, S.A.', N'Sumol Compal', N'351210030', N'911100030', 1, N'demo+sumolcompal@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.sumolcompal.pt', N'500900030', N'11070', 1200, N'Responsável Demo Sumol Compal', N'507900030', N'PT507900030', N'fiscal+sumolcompal@gerit.pt', 1, N'COMMERCIAL', N'Estrada da Portela', N'Carnaxide', N'Oeiras', N'Lisboa', N'2790-124', N'9', N'Escritório Demo', N'Responsável Demo Sumol Compal'),
        (N'demo@gerit.pt', N'GD-031', 4, N'LINKEDIN', N'[GeritDemoSeed:GD-031] Empresa real em Portugal - Vista Alegre Atlantis, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Vista Alegre Atlantis, S.A.', N'Vista Alegre', N'351234031', N'911100031', 1, N'demo+vistaalegre@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://vistaalegre.com', N'500900031', N'23410', 900, N'Responsável Demo Vista Alegre', N'507900031', N'PT507900031', N'fiscal+vistaalegre@gerit.pt', 1, N'COMMERCIAL', N'Lugar da Vista Alegre', N'Ílhavo', N'Ílhavo', N'Aveiro', N'3830-292', N'0', N'Fábrica Demo', N'Responsável Demo Vista Alegre'),
        (N'demo@gerit.pt', N'GD-032', 4, N'GOOGLE', N'[GeritDemoSeed:GD-032] Empresa real em Portugal - Critical Software, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Critical Software, S.A.', N'Critical Software', N'351239032', N'911100032', 1, N'demo+criticalsoftware@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.criticalsoftware.com', N'500900032', N'62010', 1200, N'Responsável Demo Critical', N'507900032', N'PT507900032', N'fiscal+criticalsoftware@gerit.pt', 1, N'COMMERCIAL', N'Parque Industrial de Taveiro', N'Taveiro', N'Coimbra', N'Coimbra', N'3045-504', N'0', N'Edifício Demo', N'Responsável Demo Critical'),
        (N'demo@gerit.pt', N'GD-033', 4, N'EVENTS', N'[GeritDemoSeed:GD-033] Empresa real em Portugal - Farfetch Portugal, Unipessoal Lda (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Farfetch Portugal, Unipessoal Lda', N'Farfetch Portugal', N'351220033', N'911100033', 1, N'demo+farfetch@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.farfetch.com', N'500900033', N'62010', 2500, N'Responsável Demo Farfetch', N'507900033', N'PT507900033', N'fiscal+farfetch@gerit.pt', 1, N'COMMERCIAL', N'Rua da Lionesa', N'Leça do Balio', N'Matosinhos', N'Porto', N'4465-671', N'446', N'Hub Demo', N'Responsável Demo Farfetch'),
        (N'demo@gerit.pt', N'GD-034', 4, N'LINKEDIN', N'[GeritDemoSeed:GD-034] Empresa real em Portugal - OutSystems - Software em Rede, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'OutSystems - Software em Rede, S.A.', N'OutSystems', N'351210034', N'911100034', 1, N'demo+outsystems@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://www.outsystems.com', N'500900034', N'62010', 2000, N'Responsável Demo OutSystems', N'507900034', N'PT507900034', N'fiscal+outsystems@gerit.pt', 1, N'COMMERCIAL', N'Rua Central Park', N'Linda-a-Velha', N'Oeiras', N'Lisboa', N'2795-242', N'2', N'Escritório Demo', N'Responsável Demo OutSystems'),
        (N'demo@gerit.pt', N'GD-035', 4, N'GOOGLE', N'[GeritDemoSeed:GD-035] Empresa real em Portugal - Feedzai - Consultadoria e Inovação Tecnológica, S.A. (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Feedzai - Consultadoria e Inovação Tecnológica, S.A.', N'Feedzai', N'351239035', N'911100035', 1, N'demo+feedzai@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://feedzai.com', N'500900035', N'62010', 600, N'Responsável Demo Feedzai', N'507900035', N'PT507900035', N'fiscal+feedzai@gerit.pt', 1, N'COMMERCIAL', N'Rua Pedro Nunes', N'Coimbra', N'Coimbra', N'Coimbra', N'3030-199', N'0', N'Escritório Demo', N'Responsável Demo Feedzai'),
        (N'demo@gerit.pt', N'GD-036', 4, N'WHATSAPP', N'[GeritDemoSeed:GD-036] Empresa real em Portugal - Unbabel, Unipessoal Lda (dados de contacto/fiscais sintéticos para demo)', NULL, NULL, NULL, N'Unbabel, Unipessoal Lda', N'Unbabel', N'351210036', N'911100036', 1, N'demo+unbabel@gerit.pt', NULL, NULL, NULL, NULL, NULL, N'https://unbabel.com', N'500900036', N'62020', 400, N'Responsável Demo Unbabel', N'507900036', N'PT507900036', N'fiscal+unbabel@gerit.pt', 1, N'COMMERCIAL', N'Avenida da República', N'Avenidas Novas', N'Lisboa', N'Lisboa', N'1050-191', N'45', N'Escritório Demo', N'Responsável Demo Unbabel');

    ;WITH ResolvedClientSeed AS (
        SELECT s.*,
               t.Id AS TenantId,
               ast.Id AS AcquisitionSourceTypeId,
               CASE WHEN s.LegacyClientType IN (1,2,3) THEN CONVERT(TINYINT, 1) ELSE CONVERT(TINYINT, 2) END AS PartyTypeId,
               d.Id AS StatusDomainId,
               sd.Id AS StatusDefinitionId,
               COALESCE(s.FullName, s.LegalName, s.TradeName) AS ClientName
        FROM @ClientSeed s
        INNER JOIN dbo.Tenants t ON t.Email = s.TenantEmail AND t.IsDeleted = 0
        INNER JOIN dbo.AcquisitionSourceTypes ast ON ast.Code = s.AcquisitionSourceCode AND ast.IsDeleted = 0
        INNER JOIN dbo.StatusDomains d ON d.Code = N'CLIENT' AND d.IsDeleted = 0
        INNER JOIN dbo.StatusDefinitions sd
            ON sd.TenantId = t.Id
           AND sd.StatusDomainId = d.Id
           AND sd.Code = N'ACTIVE'
           AND sd.IsDeleted = 0
    )
    UPDATE c
       SET c.PartyTypeId = s.PartyTypeId,
           c.StatusDefinitionId = s.StatusDefinitionId,
           c.StatusDomainId = s.StatusDomainId,
           c.AcquisitionSourceTypeId = s.AcquisitionSourceTypeId,
           c.Name = s.ClientName,
           c.PhoneNumber = s.PhoneNumber,
           c.CellPhoneNumber = s.CellPhoneNumber,
           c.IsCellPhoneWhatsapp = s.IsWhatsapp,
           c.Email = s.Email,
           c.WebsiteUrl = s.Site,
           c.BirthDate = CASE WHEN s.PartyTypeId = 1 THEN s.BirthDate ELSE NULL END,
           c.Gender = CASE WHEN s.PartyTypeId = 1 THEN s.Gender ELSE NULL END,
           c.Nationality = CASE WHEN s.PartyTypeId = 1 THEN s.Nationality ELSE NULL END,
           c.CompanyRegistrationNumber = CASE WHEN s.PartyTypeId = 2 THEN s.CompanyRegistration ELSE NULL END,
           c.EconomicActivityCode = CASE WHEN s.PartyTypeId = 2 THEN s.CAE ELSE NULL END,
           c.NumberOfEmployees = CASE WHEN s.PartyTypeId = 2 THEN s.NumberOfEmployee ELSE NULL END,
           c.IsActive = 1,
           c.ModifiedBy = @SeedActorId,
           c.ModifiedAt = @UtcNow
    FROM dbo.Clients c
    INNER JOIN ResolvedClientSeed s
        ON s.TenantId = c.TenantId
       AND s.Note = c.Note
    WHERE c.IsDeleted = 0;

    ;WITH ResolvedClientSeed AS (
        SELECT s.*,
               t.Id AS TenantId,
               ast.Id AS AcquisitionSourceTypeId,
               CASE WHEN s.LegacyClientType IN (1,2,3) THEN CONVERT(TINYINT, 1) ELSE CONVERT(TINYINT, 2) END AS PartyTypeId,
               d.Id AS StatusDomainId,
               sd.Id AS StatusDefinitionId,
               COALESCE(s.FullName, s.LegalName, s.TradeName) AS ClientName
        FROM @ClientSeed s
        INNER JOIN dbo.Tenants t ON t.Email = s.TenantEmail AND t.IsDeleted = 0
        INNER JOIN dbo.AcquisitionSourceTypes ast ON ast.Code = s.AcquisitionSourceCode AND ast.IsDeleted = 0
        INNER JOIN dbo.StatusDomains d ON d.Code = N'CLIENT' AND d.IsDeleted = 0
        INNER JOIN dbo.StatusDefinitions sd
            ON sd.TenantId = t.Id
           AND sd.StatusDomainId = d.Id
           AND sd.Code = N'ACTIVE'
           AND sd.IsDeleted = 0
    )
    INSERT INTO dbo.Clients (
        TenantId, PartyTypeId, StatusDefinitionId, StatusDomainId,
        AcquisitionSourceTypeId, Name,
        PhoneNumber, CellPhoneNumber, IsCellPhoneWhatsapp, Email,
        ImageUrl, WebsiteUrl,
        BirthDate, Gender, Nationality,
        CompanyRegistrationNumber, EconomicActivityCode, NumberOfEmployees,
        Note, IsActive, IsDeleted, CreatedBy, CreatedAt
    )
    SELECT s.TenantId, s.PartyTypeId, s.StatusDefinitionId, s.StatusDomainId,
           s.AcquisitionSourceTypeId, s.ClientName,
           s.PhoneNumber, s.CellPhoneNumber, s.IsWhatsapp, s.Email,
           NULL, s.Site,
           CASE WHEN s.PartyTypeId = 1 THEN s.BirthDate ELSE NULL END,
           CASE WHEN s.PartyTypeId = 1 THEN s.Gender ELSE NULL END,
           CASE WHEN s.PartyTypeId = 1 THEN s.Nationality ELSE NULL END,
           CASE WHEN s.PartyTypeId = 2 THEN s.CompanyRegistration ELSE NULL END,
           CASE WHEN s.PartyTypeId = 2 THEN s.CAE ELSE NULL END,
           CASE WHEN s.PartyTypeId = 2 THEN s.NumberOfEmployee ELSE NULL END,
           s.Note, 1, 0, @SeedActorId, @UtcNow
    FROM ResolvedClientSeed s
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Clients c
        WHERE c.TenantId = s.TenantId
          AND c.Note = s.Note
          AND c.IsDeleted = 0
    );

    UPDATE s
       SET s.ClientId = c.Id
    FROM @ClientSeed s
    INNER JOIN dbo.Tenants t ON t.Email = s.TenantEmail AND t.IsDeleted = 0
    INNER JOIN dbo.Clients c
        ON c.TenantId = t.Id
       AND c.Note = s.Note
       AND c.IsDeleted = 0;

    INSERT INTO dbo.ClientFiscalData (
        TenantId, ClientId, TaxNumber, VatNumber,
        FiscalCountry, IsVatRegistered, IBAN, FiscalEmail,
        IsActive, IsDeleted, CreatedBy, CreatedAt
    )
    SELECT t.Id, s.ClientId, s.TaxNumber, s.VatNumber,
           'PT', s.IsVatRegistered, NULL, s.FiscalEmail,
           1, 0, @SeedActorId, @UtcNow
    FROM @ClientSeed s
    INNER JOIN dbo.Tenants t ON t.Email = s.TenantEmail AND t.IsDeleted = 0
    WHERE s.ClientId IS NOT NULL
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.ClientFiscalData fd
          WHERE fd.TenantId = t.Id
            AND fd.ClientId = s.ClientId
            AND fd.IsActive = 1
            AND fd.IsDeleted = 0
      );

    INSERT INTO dbo.ClientAddresses (
        TenantId, ClientId, AddressTypeId, CountryCode,
        Street, Neighborhood, City, District, PostalCode,
        StreetNumber, Complement, Latitude, Longitude, Note,
        IsPrimary, IsActive, IsDeleted, CreatedBy, CreatedAt
    )
    SELECT t.Id, s.ClientId, atp.Id, 'PT',
           s.Street, s.Neighborhood, s.City, s.District, s.PostalCode,
           s.StreetNumber, s.Complement, NULL, NULL,
           N'Morada principal criada pela carga inicial.',
           1, 1, 0, @SeedActorId, @UtcNow
    FROM @ClientSeed s
    INNER JOIN dbo.Tenants t ON t.Email = s.TenantEmail AND t.IsDeleted = 0
    INNER JOIN dbo.AddressTypes atp ON atp.Code = s.AddressTypeCode AND atp.IsDeleted = 0
    WHERE s.ClientId IS NOT NULL
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.ClientAddresses a
          WHERE a.TenantId = t.Id
            AND a.ClientId = s.ClientId
            AND a.IsPrimary = 1
            AND a.IsDeleted = 0
      );

    INSERT INTO dbo.ClientContactPersons (
        TenantId, ClientId, JobTitle, Department, Name,
        PhoneNumber, CellPhoneNumber, IsCellPhoneWhatsapp, Email,
        IsPrimary, IsActive, IsDeleted, CreatedBy, CreatedAt
    )
    SELECT t.Id, s.ClientId,
           CASE WHEN s.LegacyClientType IN (4,5) THEN N'Representante' ELSE NULL END,
           NULL,
           s.ContactName,
           s.PhoneNumber, s.CellPhoneNumber, s.IsWhatsapp,
           CONVERT(NVARCHAR(255), s.Email),
           1, 1, 0, @SeedActorId, @UtcNow
    FROM @ClientSeed s
    INNER JOIN dbo.Tenants t ON t.Email = s.TenantEmail AND t.IsDeleted = 0
    WHERE s.ClientId IS NOT NULL
      AND s.Email IS NOT NULL
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.ClientContactPersons cp
          WHERE cp.TenantId = t.Id
            AND cp.ClientId = s.ClientId
            AND cp.Email = CONVERT(NVARCHAR(255), s.Email)
            AND cp.IsDeleted = 0
      );

    INSERT INTO dbo.ClientDocuments (
        TenantId, ClientId, DocumentTypeId, DocumentNumber,
        IssuingCountryCode, IssuedAt, ExpiresAt,
        IsPrimary, IsActive, IsDeleted, CreatedBy, CreatedAt
    )
    SELECT t.Id, s.ClientId, dt.Id, s.DocumentNumber,
           'PT', NULL, NULL,
           1, 1, 0, @SeedActorId, @UtcNow
    FROM @ClientSeed s
    INNER JOIN dbo.Tenants t ON t.Email = s.TenantEmail AND t.IsDeleted = 0
    INNER JOIN dbo.DocumentTypes dt ON dt.Code = s.DocumentTypeCode AND dt.IsDeleted = 0
    WHERE s.ClientId IS NOT NULL
      AND s.DocumentTypeCode IS NOT NULL
      AND s.DocumentNumber IS NOT NULL
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.ClientDocuments d
          WHERE d.TenantId = t.Id
            AND d.ClientId = s.ClientId
            AND d.DocumentTypeId = dt.Id
            AND d.IssuingCountryCode = 'PT'
            AND d.DocumentNumber = s.DocumentNumber
            AND d.IsDeleted = 0
      );


    /* =========================================================
       17. LOCALIZAÇÕES SUPORTADAS

       Idioma padrão da aplicação: pt-PT.
       Idiomas carregados: pt-PT, pt-BR, en-US e es-ES.

       Esta secção atualiza traduções existentes e cria as que
       estiverem em falta, mantendo a carga idempotente.
       ========================================================= */

    DECLARE @SupportedLanguages TABLE (
        LanguageCode NVARCHAR(10) PRIMARY KEY,
        IsDefault BIT NOT NULL
    );

    INSERT INTO @SupportedLanguages (LanguageCode, IsDefault)
    VALUES
        (N'pt-PT', 1),
        (N'pt-BR', 0),
        (N'en-US', 0),
        (N'es-ES', 0);

    /* ---------------------------------------------------------
       PartyTypeTranslations
       --------------------------------------------------------- */

    DECLARE @PartyTypeTranslationSeed4 TABLE (
        PartyTypeId TINYINT NOT NULL,
        LanguageCode NVARCHAR(10) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(300) NULL,
        PRIMARY KEY (PartyTypeId, LanguageCode)
    );

    INSERT INTO @PartyTypeTranslationSeed4 (PartyTypeId, LanguageCode, Name, Description)
    VALUES
        (1, N'pt-PT', N'Pessoa singular', N'Representa uma pessoa singular.'),
        (1, N'pt-BR', N'Pessoa física', N'Representa uma pessoa física.'),
        (1, N'en-US', N'Individual', N'Represents a natural person.'),
        (1, N'es-ES', N'Persona física', N'Representa a una persona física.'),
        (2, N'pt-PT', N'Organização', N'Representa uma empresa, associação ou outra pessoa coletiva.'),
        (2, N'pt-BR', N'Organização', N'Representa uma empresa, associação ou outra pessoa jurídica.'),
        (2, N'en-US', N'Organization', N'Represents a company, association, or other legal entity.'),
        (2, N'es-ES', N'Organización', N'Representa una empresa, asociación u otra persona jurídica.');

    UPDATE tr
       SET tr.Name = s.Name,
           tr.Description = s.Description
    FROM dbo.PartyTypeTranslations tr
    INNER JOIN @PartyTypeTranslationSeed4 s
        ON s.PartyTypeId = tr.PartyTypeId
       AND s.LanguageCode = tr.LanguageCode;

    INSERT INTO dbo.PartyTypeTranslations (PartyTypeId, LanguageCode, Name, Description)
    SELECT s.PartyTypeId, s.LanguageCode, s.Name, s.Description
    FROM @PartyTypeTranslationSeed4 s
    INNER JOIN dbo.PartyTypes p
        ON p.Id = s.PartyTypeId
       AND p.IsDeleted = 0
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.PartyTypeTranslations tr
        WHERE tr.PartyTypeId = s.PartyTypeId
          AND tr.LanguageCode = s.LanguageCode
    );

    /* ---------------------------------------------------------
       AcquisitionSourceTypeTranslations
       --------------------------------------------------------- */

    DECLARE @AcquisitionSourceTranslationSeed4 TABLE (
        Code NVARCHAR(50) NOT NULL,
        LanguageCode NVARCHAR(10) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(300) NULL,
        PRIMARY KEY (Code, LanguageCode)
    );

    INSERT INTO @AcquisitionSourceTranslationSeed4 (Code, LanguageCode, Name, Description)
    VALUES
        (N'OTHER', N'pt-PT', N'Outros', N'Origem não especificada ou não classificada.'),
        (N'OTHER', N'pt-BR', N'Outros', N'Origem não especificada ou não classificada.'),
        (N'OTHER', N'en-US', N'Other', N'Unspecified or unclassified acquisition source.'),
        (N'OTHER', N'es-ES', N'Otros', N'Origen de adquisición no especificado o no clasificado.'),
        (N'INSTAGRAM', N'pt-PT', N'Instagram', N'Cliente ou tenant originado através do Instagram.'),
        (N'INSTAGRAM', N'pt-BR', N'Instagram', N'Cliente ou tenant originado através do Instagram.'),
        (N'INSTAGRAM', N'en-US', N'Instagram', N'Client or tenant acquired through Instagram.'),
        (N'INSTAGRAM', N'es-ES', N'Instagram', N'Cliente o tenant adquirido a través de Instagram.'),
        (N'FACEBOOK', N'pt-PT', N'Facebook', N'Cliente ou tenant originado através do Facebook.'),
        (N'FACEBOOK', N'pt-BR', N'Facebook', N'Cliente ou tenant originado através do Facebook.'),
        (N'FACEBOOK', N'en-US', N'Facebook', N'Client or tenant acquired through Facebook.'),
        (N'FACEBOOK', N'es-ES', N'Facebook', N'Cliente o tenant adquirido a través de Facebook.'),
        (N'LINKEDIN', N'pt-PT', N'LinkedIn', N'Cliente ou tenant originado através do LinkedIn.'),
        (N'LINKEDIN', N'pt-BR', N'LinkedIn', N'Cliente ou tenant originado através do LinkedIn.'),
        (N'LINKEDIN', N'en-US', N'LinkedIn', N'Client or tenant acquired through LinkedIn.'),
        (N'LINKEDIN', N'es-ES', N'LinkedIn', N'Cliente o tenant adquirido a través de LinkedIn.'),
        (N'YOUTUBE', N'pt-PT', N'YouTube', N'Cliente ou tenant originado através do YouTube.'),
        (N'YOUTUBE', N'pt-BR', N'YouTube', N'Cliente ou tenant originado através do YouTube.'),
        (N'YOUTUBE', N'en-US', N'YouTube', N'Client or tenant acquired through YouTube.'),
        (N'YOUTUBE', N'es-ES', N'YouTube', N'Cliente o tenant adquirido a través de YouTube.'),
        (N'WHATSAPP', N'pt-PT', N'WhatsApp', N'Cliente ou tenant originado através de contacto por WhatsApp.'),
        (N'WHATSAPP', N'pt-BR', N'WhatsApp', N'Cliente ou tenant originado através de contacto por WhatsApp.'),
        (N'WHATSAPP', N'en-US', N'WhatsApp', N'Client or tenant acquired through WhatsApp.'),
        (N'WHATSAPP', N'es-ES', N'WhatsApp', N'Cliente o tenant adquirido mediante contacto por WhatsApp.'),
        (N'TIKTOK', N'pt-PT', N'TikTok', N'Cliente ou tenant originado através do TikTok.'),
        (N'TIKTOK', N'pt-BR', N'TikTok', N'Cliente ou tenant originado através do TikTok.'),
        (N'TIKTOK', N'en-US', N'TikTok', N'Client or tenant acquired through TikTok.'),
        (N'TIKTOK', N'es-ES', N'TikTok', N'Cliente o tenant adquirido a través de TikTok.'),
        (N'GOOGLE', N'pt-PT', N'Google', N'Cliente ou tenant originado através de pesquisa ou anúncio no Google.'),
        (N'GOOGLE', N'pt-BR', N'Google', N'Cliente ou tenant originado através de pesquisa ou anúncio no Google.'),
        (N'GOOGLE', N'en-US', N'Google', N'Client or tenant acquired through Google search or advertising.'),
        (N'GOOGLE', N'es-ES', N'Google', N'Cliente o tenant adquirido mediante búsqueda o publicidad en Google.'),
        (N'REFERRAL', N'pt-PT', N'Indicação', N'Cliente ou tenant originado por indicação de amigos ou conhecidos.'),
        (N'REFERRAL', N'pt-BR', N'Indicação', N'Cliente ou tenant originado por indicação de amigos ou conhecidos.'),
        (N'REFERRAL', N'en-US', N'Referral', N'Client or tenant acquired through a referral.'),
        (N'REFERRAL', N'es-ES', N'Recomendación', N'Cliente o tenant adquirido por recomendación de amigos o conocidos.'),
        (N'TV', N'pt-PT', N'Televisão', N'Cliente ou tenant originado através de publicidade ou menção em televisão.'),
        (N'TV', N'pt-BR', N'Televisão', N'Cliente ou tenant originado através de publicidade ou menção em televisão.'),
        (N'TV', N'en-US', N'Television', N'Client or tenant acquired through television.'),
        (N'TV', N'es-ES', N'Televisión', N'Cliente o tenant adquirido mediante publicidad o mención en televisión.'),
        (N'RADIO', N'pt-PT', N'Rádio', N'Cliente ou tenant originado através de rádio.'),
        (N'RADIO', N'pt-BR', N'Rádio', N'Cliente ou tenant originado através de rádio.'),
        (N'RADIO', N'en-US', N'Radio', N'Client or tenant acquired through radio.'),
        (N'RADIO', N'es-ES', N'Radio', N'Cliente o tenant adquirido a través de la radio.'),
        (N'NEWSPAPER', N'pt-PT', N'Jornal', N'Cliente ou tenant originado através de jornal.'),
        (N'NEWSPAPER', N'pt-BR', N'Jornal', N'Cliente ou tenant originado através de jornal.'),
        (N'NEWSPAPER', N'en-US', N'Newspaper', N'Client or tenant acquired through a newspaper.'),
        (N'NEWSPAPER', N'es-ES', N'Periódico', N'Cliente o tenant adquirido a través de un periódico.'),
        (N'MAGAZINE', N'pt-PT', N'Revista', N'Cliente ou tenant originado através de revista.'),
        (N'MAGAZINE', N'pt-BR', N'Revista', N'Cliente ou tenant originado através de revista.'),
        (N'MAGAZINE', N'en-US', N'Magazine', N'Client or tenant acquired through a magazine.'),
        (N'MAGAZINE', N'es-ES', N'Revista', N'Cliente o tenant adquirido a través de una revista.'),
        (N'EVENTS', N'pt-PT', N'Eventos', N'Cliente ou tenant originado através de eventos.'),
        (N'EVENTS', N'pt-BR', N'Eventos', N'Cliente ou tenant originado através de eventos.'),
        (N'EVENTS', N'en-US', N'Events', N'Client or tenant acquired through events.'),
        (N'EVENTS', N'es-ES', N'Eventos', N'Cliente o tenant adquirido a través de eventos.');

    UPDATE tr
       SET tr.Name = s.Name,
           tr.Description = s.Description
    FROM dbo.AcquisitionSourceTypeTranslations tr
    INNER JOIN dbo.AcquisitionSourceTypes p
        ON p.Id = tr.AcquisitionSourceTypeId
       AND p.IsDeleted = 0
    INNER JOIN @AcquisitionSourceTranslationSeed4 s
        ON s.Code = p.Code
       AND s.LanguageCode = tr.LanguageCode;

    INSERT INTO dbo.AcquisitionSourceTypeTranslations (AcquisitionSourceTypeId, LanguageCode, Name, Description)
    SELECT p.Id, s.LanguageCode, s.Name, s.Description
    FROM @AcquisitionSourceTranslationSeed4 s
    INNER JOIN dbo.AcquisitionSourceTypes p
        ON p.Code = s.Code
       AND p.IsDeleted = 0
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.AcquisitionSourceTypeTranslations tr
        WHERE tr.AcquisitionSourceTypeId = p.Id
          AND tr.LanguageCode = s.LanguageCode
    );

    /* ---------------------------------------------------------
       AddressTypeTranslations
       --------------------------------------------------------- */

    DECLARE @AddressTypeTranslationSeed4 TABLE (
        Code NVARCHAR(50) NOT NULL,
        LanguageCode NVARCHAR(10) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        PRIMARY KEY (Code, LanguageCode)
    );

    INSERT INTO @AddressTypeTranslationSeed4 (Code, LanguageCode, Name, Description)
    VALUES
        (N'RESIDENTIAL', N'pt-PT', N'Morada residencial', N'Morada para habitação, usada como endereço principal de pessoas.'),
        (N'RESIDENTIAL', N'pt-BR', N'Endereço residencial', N'Endereço para habitação, usada como endereço principal de pessoas.'),
        (N'RESIDENTIAL', N'en-US', N'Residential address', N'Address used as a person''s primary residence.'),
        (N'RESIDENTIAL', N'es-ES', N'Dirección residencial', N'Dirección utilizada como residencia principal de una persona.'),
        (N'COMMERCIAL', N'pt-PT', N'Morada comercial', N'Morada de um negócio ou estabelecimento para atividade comercial e atendimento.'),
        (N'COMMERCIAL', N'pt-BR', N'Endereço comercial', N'Endereço de um negócio ou estabelecimento para atividade comercial e atendimento.'),
        (N'COMMERCIAL', N'en-US', N'Commercial address', N'Business or establishment address used for commercial activity.'),
        (N'COMMERCIAL', N'es-ES', N'Dirección comercial', N'Dirección de un negocio o establecimiento utilizada para actividad comercial y atención al público.'),
        (N'INDUSTRIAL', N'pt-PT', N'Morada industrial', N'Morada associada a fábrica, unidade industrial ou armazém.'),
        (N'INDUSTRIAL', N'pt-BR', N'Endereço industrial', N'Endereço associada a fábrica, unidade industrial ou armazém.'),
        (N'INDUSTRIAL', N'en-US', N'Industrial address', N'Address associated with a factory, industrial unit, or warehouse.'),
        (N'INDUSTRIAL', N'es-ES', N'Dirección industrial', N'Dirección asociada a una fábrica, unidad industrial o almacén.'),
        (N'RURAL', N'pt-PT', N'Morada rural', N'Morada em área rural, ligada a atividade agrícola ou residência isolada.'),
        (N'RURAL', N'pt-BR', N'Endereço rural', N'Endereço em área rural, ligada a atividade agrícola ou residência isolada.'),
        (N'RURAL', N'en-US', N'Rural address', N'Address in a rural area, related to agriculture or an isolated residence.'),
        (N'RURAL', N'es-ES', N'Dirección rural', N'Dirección situada en una zona rural, vinculada a actividad agrícola o a una residencia aislada.'),
        (N'PUBLIC_SERVICE', N'pt-PT', N'Morada de serviços públicos', N'Morada de uma entidade ou serviço público.'),
        (N'PUBLIC_SERVICE', N'pt-BR', N'Endereço de serviços públicos', N'Endereço de uma entidade ou serviço público.'),
        (N'PUBLIC_SERVICE', N'en-US', N'Public service address', N'Address of a public authority or public service.'),
        (N'PUBLIC_SERVICE', N'es-ES', N'Dirección de servicios públicos', N'Dirección de una entidad o servicio público.'),
        (N'EDUCATION', N'pt-PT', N'Morada de educação', N'Morada de uma instituição de ensino.'),
        (N'EDUCATION', N'pt-BR', N'Endereço de educação', N'Endereço de uma instituição de ensino.'),
        (N'EDUCATION', N'en-US', N'Education address', N'Address of an educational institution.'),
        (N'EDUCATION', N'es-ES', N'Dirección educativa', N'Dirección de una institución educativa.'),
        (N'HEALTHCARE', N'pt-PT', N'Morada de saúde', N'Morada de um serviço ou instituição de saúde.'),
        (N'HEALTHCARE', N'pt-BR', N'Endereço de saúde', N'Endereço de um serviço ou instituição de saúde.'),
        (N'HEALTHCARE', N'en-US', N'Healthcare address', N'Address of a healthcare service or institution.'),
        (N'HEALTHCARE', N'es-ES', N'Dirección sanitaria', N'Dirección de un servicio o institución sanitaria.'),
        (N'HOSPITALITY', N'pt-PT', N'Morada de alojamento ou turismo', N'Morada de uma unidade de alojamento ou turismo.'),
        (N'HOSPITALITY', N'pt-BR', N'Endereço de alojamento ou turismo', N'Endereço de uma unidade de alojamento ou turismo.'),
        (N'HOSPITALITY', N'en-US', N'Hospitality address', N'Address of a hospitality or tourism establishment.'),
        (N'HOSPITALITY', N'es-ES', N'Dirección de alojamiento o turismo', N'Dirección de un establecimiento de alojamiento o turismo.'),
        (N'LOGISTICS', N'pt-PT', N'Morada logística ou distribuição', N'Morada dedicada a logística, armazenamento ou distribuição.'),
        (N'LOGISTICS', N'pt-BR', N'Endereço logística ou distribuição', N'Endereço dedicada a logística, armazenamento ou distribuição.'),
        (N'LOGISTICS', N'en-US', N'Logistics address', N'Address dedicated to logistics, warehousing, or distribution.'),
        (N'LOGISTICS', N'es-ES', N'Dirección logística o de distribución', N'Dirección dedicada a logística, almacenamiento o distribución.'),
        (N'POSTAL_ALTERNATIVE', N'pt-PT', N'Morada postal alternativa', N'Morada alternativa para correspondência ou entregas.'),
        (N'POSTAL_ALTERNATIVE', N'pt-BR', N'Endereço postal alternativa', N'Endereço alternativa para correspondência ou entregas.'),
        (N'POSTAL_ALTERNATIVE', N'en-US', N'Alternative postal address', N'Alternative address for correspondence or deliveries.'),
        (N'POSTAL_ALTERNATIVE', N'es-ES', N'Dirección postal alternativa', N'Dirección alternativa para correspondencia o entregas.');

    UPDATE tr
       SET tr.Name = s.Name,
           tr.Description = s.Description
    FROM dbo.AddressTypeTranslations tr
    INNER JOIN dbo.AddressTypes p
        ON p.Id = tr.AddressTypeId
       AND p.IsDeleted = 0
    INNER JOIN @AddressTypeTranslationSeed4 s
        ON s.Code = p.Code
       AND s.LanguageCode = tr.LanguageCode;

    INSERT INTO dbo.AddressTypeTranslations (AddressTypeId, LanguageCode, Name, Description)
    SELECT p.Id, s.LanguageCode, s.Name, s.Description
    FROM @AddressTypeTranslationSeed4 s
    INNER JOIN dbo.AddressTypes p
        ON p.Code = s.Code
       AND p.IsDeleted = 0
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.AddressTypeTranslations tr
        WHERE tr.AddressTypeId = p.Id
          AND tr.LanguageCode = s.LanguageCode
    );

    /* ---------------------------------------------------------
       DocumentTypeTranslations
       --------------------------------------------------------- */

    DECLARE @DocumentTypeTranslationSeed4 TABLE (
        Code NVARCHAR(50) NOT NULL,
        LanguageCode NVARCHAR(10) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(300) NULL,
        PRIMARY KEY (Code, LanguageCode)
    );

    INSERT INTO @DocumentTypeTranslationSeed4 (Code, LanguageCode, Name, Description)
    VALUES
        (N'CITIZEN_CARD', N'pt-PT', N'Cartão de Cidadão', N'Documento de identificação civil português.'),
        (N'CITIZEN_CARD', N'pt-BR', N'Cartão de Cidadão', N'Documento de identificação civil português.'),
        (N'CITIZEN_CARD', N'en-US', N'Citizen card', N'Portuguese civil identification document.'),
        (N'CITIZEN_CARD', N'es-ES', N'Tarjeta de ciudadano', N'Documento portugués de identificación civil.'),
        (N'PASSPORT', N'pt-PT', N'Passaporte', N'Documento internacional de identificação e viagem.'),
        (N'PASSPORT', N'pt-BR', N'Passaporte', N'Documento internacional de identificação e viagem.'),
        (N'PASSPORT', N'en-US', N'Passport', N'International identity and travel document.'),
        (N'PASSPORT', N'es-ES', N'Pasaporte', N'Documento internacional de identificación y viaje.'),
        (N'DRIVING_LICENSE', N'pt-PT', N'Carta de Condução', N'Documento que autoriza a condução de veículos.'),
        (N'DRIVING_LICENSE', N'pt-BR', N'Carteira de motorista', N'Documento que autoriza a condução de veículos.'),
        (N'DRIVING_LICENSE', N'en-US', N'Driving licence', N'Document authorizing a person to drive vehicles.'),
        (N'DRIVING_LICENSE', N'es-ES', N'Permiso de conducción', N'Documento que autoriza a conducir vehículos.'),
        (N'TAX_IDENTIFICATION', N'pt-PT', N'Identificação fiscal', N'Documento ou comprovativo de identificação fiscal.'),
        (N'TAX_IDENTIFICATION', N'pt-BR', N'Identificação fiscal', N'Documento ou comprovativo de identificação fiscal.'),
        (N'TAX_IDENTIFICATION', N'en-US', N'Tax identification', N'Tax identification document or certificate.'),
        (N'TAX_IDENTIFICATION', N'es-ES', N'Identificación fiscal', N'Documento o certificado de identificación fiscal.'),
        (N'COMPANY_REGISTRATION', N'pt-PT', N'Registo comercial', N'Documento de registo ou constituição de uma organização.'),
        (N'COMPANY_REGISTRATION', N'pt-BR', N'Registro comercial', N'Documento de registro ou constituição de uma organização.'),
        (N'COMPANY_REGISTRATION', N'en-US', N'Company registration', N'Company incorporation or registration document.'),
        (N'COMPANY_REGISTRATION', N'es-ES', N'Registro mercantil', N'Documento de constitución o registro de una organización.'),
        (N'OTHER', N'pt-PT', N'Outro documento', N'Outro tipo de documento não classificado.'),
        (N'OTHER', N'pt-BR', N'Outro documento', N'Outro tipo de documento não classificado.'),
        (N'OTHER', N'en-US', N'Other document', N'Other unclassified document type.'),
        (N'OTHER', N'es-ES', N'Otro documento', N'Otro tipo de documento no clasificado.');

    UPDATE tr
       SET tr.Name = s.Name,
           tr.Description = s.Description
    FROM dbo.DocumentTypeTranslations tr
    INNER JOIN dbo.DocumentTypes p
        ON p.Id = tr.DocumentTypeId
       AND p.IsDeleted = 0
    INNER JOIN @DocumentTypeTranslationSeed4 s
        ON s.Code = p.Code
       AND s.LanguageCode = tr.LanguageCode;

    INSERT INTO dbo.DocumentTypeTranslations (DocumentTypeId, LanguageCode, Name, Description)
    SELECT p.Id, s.LanguageCode, s.Name, s.Description
    FROM @DocumentTypeTranslationSeed4 s
    INNER JOIN dbo.DocumentTypes p
        ON p.Code = s.Code
       AND p.IsDeleted = 0
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.DocumentTypeTranslations tr
        WHERE tr.DocumentTypeId = p.Id
          AND tr.LanguageCode = s.LanguageCode
    );

    /* ---------------------------------------------------------
       FileTypeTranslations
       --------------------------------------------------------- */

    DECLARE @FileTypeTranslationSeed4 TABLE (
        Code NVARCHAR(50) NOT NULL,
        LanguageCode NVARCHAR(10) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(300) NULL,
        PRIMARY KEY (Code, LanguageCode)
    );

    INSERT INTO @FileTypeTranslationSeed4 (Code, LanguageCode, Name, Description)
    VALUES
        (N'JPEG', N'pt-PT', N'Imagem JPEG', N'Imagem no formato JPEG.'),
        (N'JPEG', N'pt-BR', N'Imagem JPEG', N'Imagem no formato JPEG.'),
        (N'JPEG', N'en-US', N'JPEG image', N'Image in JPEG format.'),
        (N'JPEG', N'es-ES', N'Imagen JPEG', N'Imagen en formato JPEG.'),
        (N'PNG', N'pt-PT', N'Imagem PNG', N'Imagem no formato PNG.'),
        (N'PNG', N'pt-BR', N'Imagem PNG', N'Imagem no formato PNG.'),
        (N'PNG', N'en-US', N'PNG image', N'Image in PNG format.'),
        (N'PNG', N'es-ES', N'Imagen PNG', N'Imagen en formato PNG.'),
        (N'GIF', N'pt-PT', N'Imagem GIF', N'Imagem no formato GIF.'),
        (N'GIF', N'pt-BR', N'Imagem GIF', N'Imagem no formato GIF.'),
        (N'GIF', N'en-US', N'GIF image', N'Image in GIF format.'),
        (N'GIF', N'es-ES', N'Imagen GIF', N'Imagen en formato GIF.'),
        (N'WEBP', N'pt-PT', N'Imagem WebP', N'Imagem no formato WebP.'),
        (N'WEBP', N'pt-BR', N'Imagem WebP', N'Imagem no formato WebP.'),
        (N'WEBP', N'en-US', N'WebP image', N'Image in WebP format.'),
        (N'WEBP', N'es-ES', N'Imagen WebP', N'Imagen en formato WebP.'),
        (N'SVG', N'pt-PT', N'Imagem SVG', N'Imagem vetorial no formato SVG.'),
        (N'SVG', N'pt-BR', N'Imagem SVG', N'Imagem vetorial no formato SVG.'),
        (N'SVG', N'en-US', N'SVG image', N'Vector image in SVG format.'),
        (N'SVG', N'es-ES', N'Imagen SVG', N'Imagen vectorial en formato SVG.'),
        (N'PDF', N'pt-PT', N'Documento PDF', N'Documento no formato PDF.'),
        (N'PDF', N'pt-BR', N'Documento PDF', N'Documento no formato PDF.'),
        (N'PDF', N'en-US', N'PDF document', N'Document in PDF format.'),
        (N'PDF', N'es-ES', N'Documento PDF', N'Documento en formato PDF.'),
        (N'DOC', N'pt-PT', N'Documento Word 97-2003', N'Documento Microsoft Word no formato DOC.'),
        (N'DOC', N'pt-BR', N'Documento Word 97-2003', N'Documento Microsoft Word no formato DOC.'),
        (N'DOC', N'en-US', N'Word 97-2003 document', N'Microsoft Word document in DOC format.'),
        (N'DOC', N'es-ES', N'Documento Word 97-2003', N'Documento de Microsoft Word en formato DOC.'),
        (N'DOCX', N'pt-PT', N'Documento Word', N'Documento Microsoft Word no formato DOCX.'),
        (N'DOCX', N'pt-BR', N'Documento Word', N'Documento Microsoft Word no formato DOCX.'),
        (N'DOCX', N'en-US', N'Word document', N'Microsoft Word document in DOCX format.'),
        (N'DOCX', N'es-ES', N'Documento Word', N'Documento de Microsoft Word en formato DOCX.'),
        (N'XLS', N'pt-PT', N'Folha Excel 97-2003', N'Folha de cálculo Microsoft Excel no formato XLS.'),
        (N'XLS', N'pt-BR', N'Planilha Excel 97-2003', N'Planilha Microsoft Excel no formato XLS.'),
        (N'XLS', N'en-US', N'Excel 97-2003 workbook', N'Microsoft Excel workbook in XLS format.'),
        (N'XLS', N'es-ES', N'Libro Excel 97-2003', N'Libro de Microsoft Excel en formato XLS.'),
        (N'XLSX', N'pt-PT', N'Folha Excel', N'Folha de cálculo Microsoft Excel no formato XLSX.'),
        (N'XLSX', N'pt-BR', N'Planilha Excel', N'Planilha Microsoft Excel no formato XLSX.'),
        (N'XLSX', N'en-US', N'Excel workbook', N'Microsoft Excel workbook in XLSX format.'),
        (N'XLSX', N'es-ES', N'Libro Excel', N'Libro de Microsoft Excel en formato XLSX.'),
        (N'PPT', N'pt-PT', N'Apresentação PowerPoint 97-2003', N'Apresentação Microsoft PowerPoint no formato PPT.'),
        (N'PPT', N'pt-BR', N'Apresentação PowerPoint 97-2003', N'Apresentação Microsoft PowerPoint no formato PPT.'),
        (N'PPT', N'en-US', N'PowerPoint 97-2003 presentation', N'Microsoft PowerPoint presentation in PPT format.'),
        (N'PPT', N'es-ES', N'Presentación PowerPoint 97-2003', N'Presentación de Microsoft PowerPoint en formato PPT.'),
        (N'PPTX', N'pt-PT', N'Apresentação PowerPoint', N'Apresentação Microsoft PowerPoint no formato PPTX.'),
        (N'PPTX', N'pt-BR', N'Apresentação PowerPoint', N'Apresentação Microsoft PowerPoint no formato PPTX.'),
        (N'PPTX', N'en-US', N'PowerPoint presentation', N'Microsoft PowerPoint presentation in PPTX format.'),
        (N'PPTX', N'es-ES', N'Presentación PowerPoint', N'Presentación de Microsoft PowerPoint en formato PPTX.'),
        (N'TXT', N'pt-PT', N'Ficheiro de texto', N'Ficheiro de texto simples.'),
        (N'TXT', N'pt-BR', N'Arquivo de texto', N'Arquivo de texto simples.'),
        (N'TXT', N'en-US', N'Text file', N'Plain text file.'),
        (N'TXT', N'es-ES', N'Archivo de texto', N'Archivo de texto sin formato.'),
        (N'CSV', N'pt-PT', N'Ficheiro CSV', N'Ficheiro de valores separados por vírgulas.'),
        (N'CSV', N'pt-BR', N'Arquivo CSV', N'Arquivo de valores separados por vírgulas.'),
        (N'CSV', N'en-US', N'CSV file', N'Comma-separated values file.'),
        (N'CSV', N'es-ES', N'Archivo CSV', N'Archivo de valores separados por comas.'),
        (N'JSON', N'pt-PT', N'Ficheiro JSON', N'Ficheiro de dados no formato JSON.'),
        (N'JSON', N'pt-BR', N'Arquivo JSON', N'Arquivo de dados no formato JSON.'),
        (N'JSON', N'en-US', N'JSON file', N'Data file in JSON format.'),
        (N'JSON', N'es-ES', N'Archivo JSON', N'Archivo de datos en formato JSON.'),
        (N'XML', N'pt-PT', N'Ficheiro XML', N'Ficheiro de dados no formato XML.'),
        (N'XML', N'pt-BR', N'Arquivo XML', N'Arquivo de dados no formato XML.'),
        (N'XML', N'en-US', N'XML file', N'Data file in XML format.'),
        (N'XML', N'es-ES', N'Archivo XML', N'Archivo de datos en formato XML.'),
        (N'ZIP', N'pt-PT', N'Arquivo ZIP', N'Arquivo comprimido no formato ZIP.'),
        (N'ZIP', N'pt-BR', N'Arquivo ZIP', N'Arquivo comprimido no formato ZIP.'),
        (N'ZIP', N'en-US', N'ZIP archive', N'Compressed archive in ZIP format.'),
        (N'ZIP', N'es-ES', N'Archivo ZIP', N'Archivo comprimido en formato ZIP.'),
        (N'RAR', N'pt-PT', N'Arquivo RAR', N'Arquivo comprimido no formato RAR.'),
        (N'RAR', N'pt-BR', N'Arquivo RAR', N'Arquivo comprimido no formato RAR.'),
        (N'RAR', N'en-US', N'RAR archive', N'Compressed archive in RAR format.'),
        (N'RAR', N'es-ES', N'Archivo RAR', N'Archivo comprimido en formato RAR.'),
        (N'SEVEN_ZIP', N'pt-PT', N'Arquivo 7-Zip', N'Arquivo comprimido no formato 7z.'),
        (N'SEVEN_ZIP', N'pt-BR', N'Arquivo 7-Zip', N'Arquivo comprimido no formato 7z.'),
        (N'SEVEN_ZIP', N'en-US', N'7-Zip archive', N'Compressed archive in 7z format.'),
        (N'SEVEN_ZIP', N'es-ES', N'Archivo 7-Zip', N'Archivo comprimido en formato 7z.'),
        (N'BINARY', N'pt-PT', N'Ficheiro binário', N'Ficheiro binário genérico.'),
        (N'BINARY', N'pt-BR', N'Arquivo binário', N'Arquivo binário genérico.'),
        (N'BINARY', N'en-US', N'Binary file', N'Generic binary file.'),
        (N'BINARY', N'es-ES', N'Archivo binario', N'Archivo binario genérico.');

    UPDATE tr
       SET tr.Name = s.Name,
           tr.Description = s.Description
    FROM dbo.FileTypeTranslations tr
    INNER JOIN dbo.FileTypes p
        ON p.Id = tr.FileTypeId
       AND p.IsDeleted = 0
    INNER JOIN @FileTypeTranslationSeed4 s
        ON s.Code = p.Code
       AND s.LanguageCode = tr.LanguageCode;

    INSERT INTO dbo.FileTypeTranslations (FileTypeId, LanguageCode, Name, Description)
    SELECT p.Id, s.LanguageCode, s.Name, s.Description
    FROM @FileTypeTranslationSeed4 s
    INNER JOIN dbo.FileTypes p
        ON p.Code = s.Code
       AND p.IsDeleted = 0
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.FileTypeTranslations tr
        WHERE tr.FileTypeId = p.Id
          AND tr.LanguageCode = s.LanguageCode
    );

    /* ---------------------------------------------------------
       StatusDomainTranslations
       --------------------------------------------------------- */

    DECLARE @StatusDomainTranslationSeed4 TABLE (
        Code NVARCHAR(50) NOT NULL,
        LanguageCode NVARCHAR(10) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(300) NULL,
        PRIMARY KEY (Code, LanguageCode)
    );

    INSERT INTO @StatusDomainTranslationSeed4 (Code, LanguageCode, Name, Description)
    VALUES
        (N'CLIENT', N'pt-PT', N'Cliente', N'Estados aplicáveis a clientes.'),
        (N'CLIENT', N'pt-BR', N'Cliente', N'Estados aplicáveis a clientes.'),
        (N'CLIENT', N'en-US', N'Client', N'Statuses applicable to clients.'),
        (N'CLIENT', N'es-ES', N'Cliente', N'Estados aplicables a clientes.'),
        (N'EMPLOYEE', N'pt-PT', N'Colaborador', N'Estados aplicáveis a colaboradores.'),
        (N'EMPLOYEE', N'pt-BR', N'Colaborador', N'Estados aplicáveis a colaboradores.'),
        (N'EMPLOYEE', N'en-US', N'Employee', N'Statuses applicable to employees.'),
        (N'EMPLOYEE', N'es-ES', N'Empleado', N'Estados aplicables a empleados.'),
        (N'EQUIPMENT', N'pt-PT', N'Equipamento', N'Estados aplicáveis a equipamentos.'),
        (N'EQUIPMENT', N'pt-BR', N'Equipemento', N'Estados aplicáveis a equipementos.'),
        (N'EQUIPMENT', N'en-US', N'Equipment', N'Statuses applicable to equipment.'),
        (N'EQUIPMENT', N'es-ES', N'Equipamiento', N'Estados aplicables a equipamientos.'),
        (N'VEHICLE', N'pt-PT', N'Veículo', N'Estados aplicáveis a veículos.'),
        (N'VEHICLE', N'pt-BR', N'Veículo', N'Estados aplicáveis a veículos.'),
        (N'VEHICLE', N'en-US', N'Vehicle', N'Statuses applicable to vehicles.'),
        (N'VEHICLE', N'es-ES', N'Vehículo', N'Estados aplicables a vehículos.'),
        (N'VISIT', N'pt-PT', N'Visita', N'Estados aplicáveis a visitas ou intervenções.'),
        (N'VISIT', N'pt-BR', N'Visita', N'Estados aplicáveis a visitas ou atendimentos.'),
        (N'VISIT', N'en-US', N'Visit', N'Statuses applicable to visits or interventions.'),
        (N'VISIT', N'es-ES', N'Visita', N'Estados aplicables a visitas o intervenciones.'),
        (N'SUBSCRIPTION', N'pt-PT', N'Assinatura', N'Estados aplicáveis a assinaturas.'),
        (N'SUBSCRIPTION', N'pt-BR', N'Assinatura', N'Estados aplicáveis a assinaturas.'),
        (N'SUBSCRIPTION', N'en-US', N'Subscription', N'Statuses applicable to subscriptions.'),
        (N'SUBSCRIPTION', N'es-ES', N'Suscripción', N'Estados aplicables a suscripciones.');

    UPDATE tr
       SET tr.Name = s.Name,
           tr.Description = s.Description
    FROM dbo.StatusDomainTranslations tr
    INNER JOIN dbo.StatusDomains p
        ON p.Id = tr.StatusDomainId
       AND p.IsDeleted = 0
    INNER JOIN @StatusDomainTranslationSeed4 s
        ON s.Code = p.Code
       AND s.LanguageCode = tr.LanguageCode;

    INSERT INTO dbo.StatusDomainTranslations (StatusDomainId, LanguageCode, Name, Description)
    SELECT p.Id, s.LanguageCode, s.Name, s.Description
    FROM @StatusDomainTranslationSeed4 s
    INNER JOIN dbo.StatusDomains p
        ON p.Code = s.Code
       AND p.IsDeleted = 0
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.StatusDomainTranslations tr
        WHERE tr.StatusDomainId = p.Id
          AND tr.LanguageCode = s.LanguageCode
    );

    /* ---------------------------------------------------------
       SubscriptionPlanTranslations
       --------------------------------------------------------- */

    DECLARE @SubscriptionPlanTranslationSeed4 TABLE (
        Code NVARCHAR(50) NOT NULL,
        LanguageCode NVARCHAR(10) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        PRIMARY KEY (Code, LanguageCode)
    );

    INSERT INTO @SubscriptionPlanTranslationSeed4 (Code, LanguageCode, Name, Description)
    VALUES
        (N'FREE', N'pt-PT', N'Gratuito', N'Plano gratuito com funcionalidades básicas para testes e uso inicial.'),
        (N'FREE', N'pt-BR', N'Gratuito', N'Plano gratuito com funcionalidades básicas para testes e uso inicial.'),
        (N'FREE', N'en-US', N'Free', N'Free plan with basic features for testing and initial use.'),
        (N'FREE', N'es-ES', N'Gratuito', N'Plan gratuito con funciones básicas para pruebas y uso inicial.'),
        (N'BASIC', N'pt-PT', N'Básico', N'Plano básico para pequenos negócios com funcionalidades essenciais.'),
        (N'BASIC', N'pt-BR', N'Básico', N'Plano básico para pequenos negócios com funcionalidades essenciais.'),
        (N'BASIC', N'en-US', N'Basic', N'Basic plan for small businesses with essential features.'),
        (N'BASIC', N'es-ES', N'Básico', N'Plan básico para pequeñas empresas con funciones esenciales.'),
        (N'STANDARD', N'pt-PT', N'Standard', N'Plano intermédio com mais capacidade e funcionalidades avançadas.'),
        (N'STANDARD', N'pt-BR', N'Standard', N'Plano intermediário com mais capacidade e funcionalidades avançadas.'),
        (N'STANDARD', N'en-US', N'Standard', N'Intermediate plan with greater capacity and advanced features.'),
        (N'STANDARD', N'es-ES', N'Estándar', N'Plan intermedio con mayor capacidad y funciones avanzadas.'),
        (N'PROFESSIONAL', N'pt-PT', N'Profissional', N'Plano avançado para equipas com maior volume de intervenções.'),
        (N'PROFESSIONAL', N'pt-BR', N'Profissional', N'Plano avançado para equipes com maior volume de atendimentos.'),
        (N'PROFESSIONAL', N'en-US', N'Professional', N'Advanced plan for teams with a higher volume of interventions.'),
        (N'PROFESSIONAL', N'es-ES', N'Profesional', N'Plan avanzado para equipos con un mayor volumen de intervenciones.'),
        (N'ENTERPRISE', N'pt-PT', N'Enterprise', N'Plano completo para grandes organizações com necessidades complexas.'),
        (N'ENTERPRISE', N'pt-BR', N'Enterprise', N'Plano completo para grandes organizações com necessidades complexas.'),
        (N'ENTERPRISE', N'en-US', N'Enterprise', N'Complete plan for large organizations with complex requirements.'),
        (N'ENTERPRISE', N'es-ES', N'Empresarial', N'Plan completo para grandes organizaciones con necesidades complejas.'),
        (N'PAYG_HOURLY', N'pt-PT', N'Pagamento por hora', N'Plano baseado em consumo por hora.'),
        (N'PAYG_HOURLY', N'pt-BR', N'Pagamento por hora', N'Plano baseado em consumo por hora.'),
        (N'PAYG_HOURLY', N'en-US', N'Pay as you go hourly', N'Usage-based plan billed by the hour.'),
        (N'PAYG_HOURLY', N'es-ES', N'Pago por hora', N'Plan basado en consumo facturado por hora.'),
        (N'PAYG_DAILY', N'pt-PT', N'Pagamento por dia', N'Plano baseado em consumo por dia.'),
        (N'PAYG_DAILY', N'pt-BR', N'Pagamento por dia', N'Plano baseado em consumo por dia.'),
        (N'PAYG_DAILY', N'en-US', N'Pay as you go daily', N'Usage-based plan billed by the day.'),
        (N'PAYG_DAILY', N'es-ES', N'Pago por día', N'Plan basado en consumo facturado por día.');

    UPDATE tr
       SET tr.Name = s.Name,
           tr.Description = s.Description
    FROM dbo.SubscriptionPlanTranslations tr
    INNER JOIN dbo.SubscriptionPlans p
        ON p.Id = tr.SubscriptionPlanId
       AND p.IsDeleted = 0
    INNER JOIN @SubscriptionPlanTranslationSeed4 s
        ON s.Code = p.Code
       AND s.LanguageCode = tr.LanguageCode;

    INSERT INTO dbo.SubscriptionPlanTranslations (SubscriptionPlanId, LanguageCode, Name, Description)
    SELECT p.Id, s.LanguageCode, s.Name, s.Description
    FROM @SubscriptionPlanTranslationSeed4 s
    INNER JOIN dbo.SubscriptionPlans p
        ON p.Code = s.Code
       AND p.IsDeleted = 0
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.SubscriptionPlanTranslations tr
        WHERE tr.SubscriptionPlanId = p.Id
          AND tr.LanguageCode = s.LanguageCode
    );

    /* ---------------------------------------------------------
       StatusDefinitionTranslations por tenant
       --------------------------------------------------------- */

    DECLARE @StatusDefinitionTranslationSeed4 TABLE (
        DomainCode NVARCHAR(50) NOT NULL,
        StatusCode NVARCHAR(50) NOT NULL,
        LanguageCode NVARCHAR(10) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        PRIMARY KEY (DomainCode, StatusCode, LanguageCode)
    );

    INSERT INTO @StatusDefinitionTranslationSeed4 (
        DomainCode, StatusCode, LanguageCode, Name, Description
    )
    VALUES
        (N'CLIENT', N'PROSPECT', N'pt-PT', N'Potencial', N'Cliente potencial ainda não convertido.'),
        (N'CLIENT', N'PROSPECT', N'pt-BR', N'Potencial', N'Cliente potencial ainda não convertido.'),
        (N'CLIENT', N'PROSPECT', N'en-US', N'Prospect', N'Potential client not yet converted.'),
        (N'CLIENT', N'PROSPECT', N'es-ES', N'Potencial', N'Cliente potencial aún no convertido.'),
        (N'CLIENT', N'ACTIVE', N'pt-PT', N'Ativo', N'Cliente ativo.'),
        (N'CLIENT', N'ACTIVE', N'pt-BR', N'Ativo', N'Cliente ativo.'),
        (N'CLIENT', N'ACTIVE', N'en-US', N'Active', N'Active client.'),
        (N'CLIENT', N'ACTIVE', N'es-ES', N'Activo', N'Cliente activo.'),
        (N'CLIENT', N'INACTIVE', N'pt-PT', N'Inativo', N'Cliente temporariamente inativo.'),
        (N'CLIENT', N'INACTIVE', N'pt-BR', N'Inativo', N'Cliente temporariamente inativo.'),
        (N'CLIENT', N'INACTIVE', N'en-US', N'Inactive', N'Temporarily inactive client.'),
        (N'CLIENT', N'INACTIVE', N'es-ES', N'Inactivo', N'Cliente temporalmente inactivo.'),
        (N'CLIENT', N'SUSPENDED', N'pt-PT', N'Suspenso', N'Cliente com atividade suspensa.'),
        (N'CLIENT', N'SUSPENDED', N'pt-BR', N'Suspenso', N'Cliente com atividade suspensa.'),
        (N'CLIENT', N'SUSPENDED', N'en-US', N'Suspended', N'Client with suspended activity.'),
        (N'CLIENT', N'SUSPENDED', N'es-ES', N'Suspendido', N'Cliente con actividad suspendida.'),
        (N'CLIENT', N'ARCHIVED', N'pt-PT', N'Arquivado', N'Cliente arquivado para histórico.'),
        (N'CLIENT', N'ARCHIVED', N'pt-BR', N'Arquivado', N'Cliente arquivado para histórico.'),
        (N'CLIENT', N'ARCHIVED', N'en-US', N'Archived', N'Client archived for historical purposes.'),
        (N'CLIENT', N'ARCHIVED', N'es-ES', N'Archivado', N'Cliente archivado con fines históricos.'),
        (N'EMPLOYEE', N'ACTIVE', N'pt-PT', N'Ativo', N'Colaborador ativo.'),
        (N'EMPLOYEE', N'ACTIVE', N'pt-BR', N'Ativo', N'Colaborador ativo.'),
        (N'EMPLOYEE', N'ACTIVE', N'en-US', N'Active', N'Active employee.'),
        (N'EMPLOYEE', N'ACTIVE', N'es-ES', N'Activo', N'Empleado activo.'),
        (N'EMPLOYEE', N'ON_LEAVE', N'pt-PT', N'Ausente', N'Colaborador temporariamente ausente.'),
        (N'EMPLOYEE', N'ON_LEAVE', N'pt-BR', N'Ausente', N'Colaborador temporariamente ausente.'),
        (N'EMPLOYEE', N'ON_LEAVE', N'en-US', N'On leave', N'Employee temporarily on leave.'),
        (N'EMPLOYEE', N'ON_LEAVE', N'es-ES', N'De permiso', N'Empleado temporalmente ausente.'),
        (N'EMPLOYEE', N'INACTIVE', N'pt-PT', N'Inativo', N'Colaborador inativo.'),
        (N'EMPLOYEE', N'INACTIVE', N'pt-BR', N'Inativo', N'Colaborador inativo.'),
        (N'EMPLOYEE', N'INACTIVE', N'en-US', N'Inactive', N'Inactive employee.'),
        (N'EMPLOYEE', N'INACTIVE', N'es-ES', N'Inactivo', N'Empleado inactivo.'),
        (N'EMPLOYEE', N'TERMINATED', N'pt-PT', N'Desvinculado', N'Colaborador sem vínculo ativo.'),
        (N'EMPLOYEE', N'TERMINATED', N'pt-BR', N'Desligado', N'Colaborador sem vínculo ativo.'),
        (N'EMPLOYEE', N'TERMINATED', N'en-US', N'Terminated', N'Employee whose engagement has ended.'),
        (N'EMPLOYEE', N'TERMINATED', N'es-ES', N'Desvinculado', N'Empleado sin vínculo activo.'),
        (N'EQUIPMENT', N'AVAILABLE', N'pt-PT', N'Disponível', N'Equipamento disponível para utilização.'),
        (N'EQUIPMENT', N'AVAILABLE', N'pt-BR', N'Disponível', N'Equipemento disponível para uso.'),
        (N'EQUIPMENT', N'AVAILABLE', N'en-US', N'Available', N'Equipment available for use.'),
        (N'EQUIPMENT', N'AVAILABLE', N'es-ES', N'Disponible', N'Equipamiento disponible para su uso.'),
        (N'EQUIPMENT', N'IN_USE', N'pt-PT', N'Em utilização', N'Equipamento atualmente em utilização.'),
        (N'EQUIPMENT', N'IN_USE', N'pt-BR', N'Em uso', N'Equipemento atualmente em uso.'),
        (N'EQUIPMENT', N'IN_USE', N'en-US', N'In use', N'Equipment currently in use.'),
        (N'EQUIPMENT', N'IN_USE', N'es-ES', N'En uso', N'Equipamiento actualmente en uso.'),
        (N'EQUIPMENT', N'MAINTENANCE', N'pt-PT', N'Em manutenção', N'Equipamento em manutenção.'),
        (N'EQUIPMENT', N'MAINTENANCE', N'pt-BR', N'Em manutenção', N'Equipemento em manutenção.'),
        (N'EQUIPMENT', N'MAINTENANCE', N'en-US', N'Under maintenance', N'Equipment undergoing maintenance.'),
        (N'EQUIPMENT', N'MAINTENANCE', N'es-ES', N'En mantenimiento', N'Equipamiento en mantenimiento.'),
        (N'EQUIPMENT', N'RETIRED', N'pt-PT', N'Retirado', N'Equipamento retirado de operação.'),
        (N'EQUIPMENT', N'RETIRED', N'pt-BR', N'Retirado', N'Equipemento retirado de operação.'),
        (N'EQUIPMENT', N'RETIRED', N'en-US', N'Retired', N'Equipment retired from service.'),
        (N'EQUIPMENT', N'RETIRED', N'es-ES', N'Retirado', N'Equipamiento retirado del servicio.'),
        (N'VEHICLE', N'AVAILABLE', N'pt-PT', N'Disponível', N'Veículo disponível para utilização.'),
        (N'VEHICLE', N'AVAILABLE', N'pt-BR', N'Disponível', N'Veículo disponível para uso.'),
        (N'VEHICLE', N'AVAILABLE', N'en-US', N'Available', N'Vehicle available for use.'),
        (N'VEHICLE', N'AVAILABLE', N'es-ES', N'Disponible', N'Vehículo disponible para su uso.'),
        (N'VEHICLE', N'IN_USE', N'pt-PT', N'Em utilização', N'Veículo atualmente em utilização.'),
        (N'VEHICLE', N'IN_USE', N'pt-BR', N'Em uso', N'Veículo atualmente em uso.'),
        (N'VEHICLE', N'IN_USE', N'en-US', N'In use', N'Vehicle currently in use.'),
        (N'VEHICLE', N'IN_USE', N'es-ES', N'En uso', N'Vehículo actualmente en uso.'),
        (N'VEHICLE', N'MAINTENANCE', N'pt-PT', N'Em manutenção', N'Veículo em manutenção.'),
        (N'VEHICLE', N'MAINTENANCE', N'pt-BR', N'Em manutenção', N'Veículo em manutenção.'),
        (N'VEHICLE', N'MAINTENANCE', N'en-US', N'Under maintenance', N'Vehicle undergoing maintenance.'),
        (N'VEHICLE', N'MAINTENANCE', N'es-ES', N'En mantenimiento', N'Vehículo en mantenimiento.'),
        (N'VEHICLE', N'RETIRED', N'pt-PT', N'Retirado', N'Veículo retirado de operação.'),
        (N'VEHICLE', N'RETIRED', N'pt-BR', N'Retirado', N'Veículo retirado de operação.'),
        (N'VEHICLE', N'RETIRED', N'en-US', N'Retired', N'Vehicle retired from service.'),
        (N'VEHICLE', N'RETIRED', N'es-ES', N'Retirado', N'Vehículo retirado del servicio.'),
        (N'VISIT', N'SCHEDULED', N'pt-PT', N'Agendada', N'Visita criada e agendada para uma data futura.'),
        (N'VISIT', N'SCHEDULED', N'pt-BR', N'Agendada', N'Visita criada e agendada para uma data futura.'),
        (N'VISIT', N'SCHEDULED', N'en-US', N'Scheduled', N'Visit created and scheduled for a future date.'),
        (N'VISIT', N'SCHEDULED', N'es-ES', N'Programada', N'Visita creada y programada para una fecha futura.'),
        (N'VISIT', N'CONFIRMED', N'pt-PT', N'Confirmada', N'Visita confirmada com o cliente.'),
        (N'VISIT', N'CONFIRMED', N'pt-BR', N'Confirmada', N'Visita confirmada com o cliente.'),
        (N'VISIT', N'CONFIRMED', N'en-US', N'Confirmed', N'Visit confirmed with the client.'),
        (N'VISIT', N'CONFIRMED', N'es-ES', N'Confirmada', N'Visita confirmada con el cliente.'),
        (N'VISIT', N'EN_ROUTE', N'pt-PT', N'Em deslocação', N'Equipa em deslocação para o local.'),
        (N'VISIT', N'EN_ROUTE', N'pt-BR', N'Em deslocamento', N'Equipe em deslocamento para o local.'),
        (N'VISIT', N'EN_ROUTE', N'en-US', N'En route', N'Team traveling to the location.'),
        (N'VISIT', N'EN_ROUTE', N'es-ES', N'En camino', N'Equipo desplazándose hacia la ubicación.'),
        (N'VISIT', N'IN_PROGRESS', N'pt-PT', N'Em andamento', N'Visita em execução.'),
        (N'VISIT', N'IN_PROGRESS', N'pt-BR', N'Em andamento', N'Visita em execução.'),
        (N'VISIT', N'IN_PROGRESS', N'en-US', N'In progress', N'Visit currently being performed.'),
        (N'VISIT', N'IN_PROGRESS', N'es-ES', N'En curso', N'Visita actualmente en ejecución.'),
        (N'VISIT', N'PAUSED', N'pt-PT', N'Em pausa', N'Visita temporariamente pausada.'),
        (N'VISIT', N'PAUSED', N'pt-BR', N'Em pausa', N'Visita temporariamente pausada.'),
        (N'VISIT', N'PAUSED', N'en-US', N'Paused', N'Visit temporarily paused.'),
        (N'VISIT', N'PAUSED', N'es-ES', N'En pausa', N'Visita temporalmente pausada.'),
        (N'VISIT', N'WAITING_CLIENT', N'pt-PT', N'A aguardar cliente', N'Visita parada a aguardar ação do cliente.'),
        (N'VISIT', N'WAITING_CLIENT', N'pt-BR', N'Aguardando cliente', N'Visita parada aguardando ação do cliente.'),
        (N'VISIT', N'WAITING_CLIENT', N'en-US', N'Waiting for client', N'Visit waiting for client action.'),
        (N'VISIT', N'WAITING_CLIENT', N'es-ES', N'En espera del cliente', N'Visita detenida a la espera de una acción del cliente.'),
        (N'VISIT', N'WAITING_MATERIAL', N'pt-PT', N'A aguardar material', N'Visita suspensa por falta de material.'),
        (N'VISIT', N'WAITING_MATERIAL', N'pt-BR', N'Aguardando material', N'Visita suspensa por falta de material.'),
        (N'VISIT', N'WAITING_MATERIAL', N'en-US', N'Waiting for material', N'Visit waiting for required material.'),
        (N'VISIT', N'WAITING_MATERIAL', N'es-ES', N'En espera de material', N'Visita suspendida por falta del material necesario.'),
        (N'VISIT', N'RESCHEDULED', N'pt-PT', N'Reagendada', N'Visita reagendada para nova data.'),
        (N'VISIT', N'RESCHEDULED', N'pt-BR', N'Reagendada', N'Visita reagendada para nova data.'),
        (N'VISIT', N'RESCHEDULED', N'en-US', N'Rescheduled', N'Visit rescheduled to a new date.'),
        (N'VISIT', N'RESCHEDULED', N'es-ES', N'Reprogramada', N'Visita reprogramada para una nueva fecha.'),
        (N'VISIT', N'COMPLETED', N'pt-PT', N'Concluída', N'Visita concluída com sucesso.'),
        (N'VISIT', N'COMPLETED', N'pt-BR', N'Concluída', N'Visita concluída com sucesso.'),
        (N'VISIT', N'COMPLETED', N'en-US', N'Completed', N'Visit completed successfully.'),
        (N'VISIT', N'COMPLETED', N'es-ES', N'Completada', N'Visita completada correctamente.'),
        (N'VISIT', N'COMPLETED_PENDING', N'pt-PT', N'Concluída com pendências', N'Visita concluída com itens pendentes.'),
        (N'VISIT', N'COMPLETED_PENDING', N'pt-BR', N'Concluída com pendências', N'Visita concluída com itens pendentes.'),
        (N'VISIT', N'COMPLETED_PENDING', N'en-US', N'Completed with pending items', N'Visit completed with pending items.'),
        (N'VISIT', N'COMPLETED_PENDING', N'es-ES', N'Completada con pendientes', N'Visita completada con elementos pendientes.'),
        (N'VISIT', N'CANCELED', N'pt-PT', N'Cancelada', N'Visita cancelada.'),
        (N'VISIT', N'CANCELED', N'pt-BR', N'Cancelada', N'Visita cancelada.'),
        (N'VISIT', N'CANCELED', N'en-US', N'Canceled', N'Visit canceled.'),
        (N'VISIT', N'CANCELED', N'es-ES', N'Cancelada', N'Visita cancelada.'),
        (N'VISIT', N'NOT_PERFORMED', N'pt-PT', N'Não realizada', N'Visita não realizada.'),
        (N'VISIT', N'NOT_PERFORMED', N'pt-BR', N'Não realizada', N'Visita não realizada.'),
        (N'VISIT', N'NOT_PERFORMED', N'en-US', N'Not performed', N'Visit was not performed.'),
        (N'VISIT', N'NOT_PERFORMED', N'es-ES', N'No realizada', N'La visita no se realizó.'),
        (N'VISIT', N'VALIDATING', N'pt-PT', N'Em validação', N'Visita a aguardar validação.'),
        (N'VISIT', N'VALIDATING', N'pt-BR', N'Em validação', N'Visita aguardando validação.'),
        (N'VISIT', N'VALIDATING', N'en-US', N'Under validation', N'Visit awaiting validation.'),
        (N'VISIT', N'VALIDATING', N'es-ES', N'En validación', N'Visita pendiente de validación.'),
        (N'VISIT', N'INVOICED', N'pt-PT', N'Faturada', N'Visita já faturada.'),
        (N'VISIT', N'INVOICED', N'pt-BR', N'Faturada', N'Visita já faturada.'),
        (N'VISIT', N'INVOICED', N'en-US', N'Invoiced', N'Visit already invoiced.'),
        (N'VISIT', N'INVOICED', N'es-ES', N'Facturada', N'Visita ya facturada.'),
        (N'VISIT', N'ARCHIVED', N'pt-PT', N'Arquivada', N'Visita encerrada e arquivada.'),
        (N'VISIT', N'ARCHIVED', N'pt-BR', N'Arquivada', N'Visita encerrada e arquivada.'),
        (N'VISIT', N'ARCHIVED', N'en-US', N'Archived', N'Visit closed and archived.'),
        (N'VISIT', N'ARCHIVED', N'es-ES', N'Archivada', N'Visita cerrada y archivada.'),
        (N'SUBSCRIPTION', N'TRIAL', N'pt-PT', N'Em período experimental', N'Assinatura em período experimental.'),
        (N'SUBSCRIPTION', N'TRIAL', N'pt-BR', N'Em período de teste', N'Assinatura em período de teste.'),
        (N'SUBSCRIPTION', N'TRIAL', N'en-US', N'Trial', N'Subscription in trial period.'),
        (N'SUBSCRIPTION', N'TRIAL', N'es-ES', N'En periodo de prueba', N'Suscripción en periodo de prueba.'),
        (N'SUBSCRIPTION', N'ACTIVE', N'pt-PT', N'Ativa', N'Assinatura ativa.'),
        (N'SUBSCRIPTION', N'ACTIVE', N'pt-BR', N'Ativa', N'Assinatura ativa.'),
        (N'SUBSCRIPTION', N'ACTIVE', N'en-US', N'Active', N'Active subscription.'),
        (N'SUBSCRIPTION', N'ACTIVE', N'es-ES', N'Activa', N'Suscripción activa.'),
        (N'SUBSCRIPTION', N'PAST_DUE', N'pt-PT', N'Pagamento em atraso', N'Assinatura com pagamento em atraso.'),
        (N'SUBSCRIPTION', N'PAST_DUE', N'pt-BR', N'Pagamento em atraso', N'Assinatura com pagamento em atraso.'),
        (N'SUBSCRIPTION', N'PAST_DUE', N'en-US', N'Past due', N'Subscription with overdue payment.'),
        (N'SUBSCRIPTION', N'PAST_DUE', N'es-ES', N'Pago atrasado', N'Suscripción con un pago vencido.'),
        (N'SUBSCRIPTION', N'CANCELED', N'pt-PT', N'Cancelada', N'Assinatura cancelada.'),
        (N'SUBSCRIPTION', N'CANCELED', N'pt-BR', N'Cancelada', N'Assinatura cancelada.'),
        (N'SUBSCRIPTION', N'CANCELED', N'en-US', N'Canceled', N'Canceled subscription.'),
        (N'SUBSCRIPTION', N'CANCELED', N'es-ES', N'Cancelada', N'Suscripción cancelada.'),
        (N'SUBSCRIPTION', N'EXPIRED', N'pt-PT', N'Expirada', N'Assinatura expirada.'),
        (N'SUBSCRIPTION', N'EXPIRED', N'pt-BR', N'Expirada', N'Assinatura expirada.'),
        (N'SUBSCRIPTION', N'EXPIRED', N'en-US', N'Expired', N'Expired subscription.'),
        (N'SUBSCRIPTION', N'EXPIRED', N'es-ES', N'Expirada', N'Suscripción expirada.');

    UPDATE tr
       SET tr.Name = s.Name,
           tr.Description = s.Description
    FROM dbo.StatusDefinitionTranslations tr
    INNER JOIN dbo.StatusDefinitions sd
        ON sd.Id = tr.StatusDefinitionId
       AND sd.TenantId = tr.TenantId
       AND sd.StatusDomainId = tr.StatusDomainId
       AND sd.IsDeleted = 0
    INNER JOIN dbo.StatusDomains d
        ON d.Id = sd.StatusDomainId
       AND d.IsDeleted = 0
    INNER JOIN @StatusDefinitionTranslationSeed4 s
        ON s.DomainCode = d.Code
       AND s.StatusCode = sd.Code
       AND s.LanguageCode = tr.LanguageCode;

    INSERT INTO dbo.StatusDefinitionTranslations (
        TenantId, StatusDomainId, StatusDefinitionId,
        LanguageCode, Name, Description
    )
    SELECT sd.TenantId, sd.StatusDomainId, sd.Id,
           s.LanguageCode, s.Name, s.Description
    FROM dbo.StatusDefinitions sd
    INNER JOIN dbo.StatusDomains d
        ON d.Id = sd.StatusDomainId
       AND d.IsDeleted = 0
    INNER JOIN @StatusDefinitionTranslationSeed4 s
        ON s.DomainCode = d.Code
       AND s.StatusCode = sd.Code
    WHERE sd.IsDeleted = 0
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.StatusDefinitionTranslations tr
          WHERE tr.TenantId = sd.TenantId
            AND tr.StatusDefinitionId = sd.Id
            AND tr.LanguageCode = s.LanguageCode
      );

    /* ---------------------------------------------------------
       Validação de cobertura dos quatro idiomas
       --------------------------------------------------------- */

    IF EXISTS (
        SELECT 1
        FROM @PartyTypeSeed p
        CROSS JOIN @SupportedLanguages l
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.PartyTypeTranslations tr
            WHERE tr.PartyTypeId = p.Id
              AND tr.LanguageCode = l.LanguageCode
        )
    ) THROW 51020, 'Faltam traduções em PartyTypeTranslations.', 1;

    IF EXISTS (
        SELECT 1
        FROM @AcquisitionSourceSeed s
        INNER JOIN dbo.AcquisitionSourceTypes p ON p.Code = s.Code AND p.IsDeleted = 0
        CROSS JOIN @SupportedLanguages l
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.AcquisitionSourceTypeTranslations tr
            WHERE tr.AcquisitionSourceTypeId = p.Id
              AND tr.LanguageCode = l.LanguageCode
        )
    ) THROW 51021, 'Faltam traduções em AcquisitionSourceTypeTranslations.', 1;

    IF EXISTS (
        SELECT 1
        FROM @AddressTypeSeed s
        INNER JOIN dbo.AddressTypes p ON p.Code = s.Code AND p.IsDeleted = 0
        CROSS JOIN @SupportedLanguages l
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.AddressTypeTranslations tr
            WHERE tr.AddressTypeId = p.Id
              AND tr.LanguageCode = l.LanguageCode
        )
    ) THROW 51022, 'Faltam traduções em AddressTypeTranslations.', 1;

    IF EXISTS (
        SELECT 1
        FROM @DocumentTypeSeed s
        INNER JOIN dbo.DocumentTypes p ON p.Code = s.Code AND p.IsDeleted = 0
        CROSS JOIN @SupportedLanguages l
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.DocumentTypeTranslations tr
            WHERE tr.DocumentTypeId = p.Id
              AND tr.LanguageCode = l.LanguageCode
        )
    ) THROW 51023, 'Faltam traduções em DocumentTypeTranslations.', 1;

    IF EXISTS (
        SELECT 1
        FROM @FileTypeSeed s
        INNER JOIN dbo.FileTypes p ON p.Code = s.Code AND p.IsDeleted = 0
        CROSS JOIN @SupportedLanguages l
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.FileTypeTranslations tr
            WHERE tr.FileTypeId = p.Id
              AND tr.LanguageCode = l.LanguageCode
        )
    ) THROW 51024, 'Faltam traduções em FileTypeTranslations.', 1;

    IF EXISTS (
        SELECT 1
        FROM @StatusDomainSeed s
        INNER JOIN dbo.StatusDomains p ON p.Code = s.Code AND p.IsDeleted = 0
        CROSS JOIN @SupportedLanguages l
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.StatusDomainTranslations tr
            WHERE tr.StatusDomainId = p.Id
              AND tr.LanguageCode = l.LanguageCode
        )
    ) THROW 51025, 'Faltam traduções em StatusDomainTranslations.', 1;

    IF EXISTS (
        SELECT 1
        FROM @PlanSeed s
        INNER JOIN dbo.SubscriptionPlans p ON p.Code = s.Code AND p.IsDeleted = 0
        CROSS JOIN @SupportedLanguages l
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.SubscriptionPlanTranslations tr
            WHERE tr.SubscriptionPlanId = p.Id
              AND tr.LanguageCode = l.LanguageCode
        )
    ) THROW 51026, 'Faltam traduções em SubscriptionPlanTranslations.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.StatusDefinitions sd
        INNER JOIN dbo.StatusDomains d ON d.Id = sd.StatusDomainId AND d.IsDeleted = 0
        INNER JOIN @StatusDefinitionSeed s ON s.DomainCode = d.Code AND s.StatusCode = sd.Code
        CROSS JOIN @SupportedLanguages l
        WHERE sd.IsDeleted = 0
          AND NOT EXISTS (
              SELECT 1 FROM dbo.StatusDefinitionTranslations tr
              WHERE tr.TenantId = sd.TenantId
                AND tr.StatusDefinitionId = sd.Id
                AND tr.LanguageCode = l.LanguageCode
          )
    ) THROW 51027, 'Faltam traduções em StatusDefinitionTranslations.', 1;

    /* =========================================================
       18. CONCLUSÃO
       ========================================================= */

    COMMIT TRANSACTION;
    EXEC sys.sp_set_session_context @key = N'IsSuperAdmin', @value = NULL;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    EXEC sys.sp_set_session_context @key = N'IsSuperAdmin', @value = NULL;
    THROW;
END CATCH;
GO
