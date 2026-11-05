# Inventário dos campos Code e *Code

Auditoria de 25/10/2026 sobre o working tree de `feature/refactor-code-natural-keys`. Documento de evidências e recomendações; não comprova estado de bases existentes nem contratos de integrações externas. A matriz de schema preserva a auditoria da fase 1; proveniência foi atualizada à decisão definitiva en-US para novas criações. Os estados anteriores são identificados como históricos. A especificação final fica em [Code-Natural-Keys.md](Code-Natural-Keys.md).

## Decisão definitiva para novas criações

Nos sete catálogos não técnicos, Code deriva exclusivamente de exatamente uma
Translations[en-US].Name. Ausência/duplicação/invalidez retorna 400 localizado,
sem tradução automática nem fallback pt-PT/locale. O consumidor não envia Code.
FileTypes mantém derivação técnica com aliases. Qualquer Update de tradução,
inclusive en-US, preserva o Code. Seeds existentes continuam com os 102 códigos
estabelecidos, incluindo WAITING_CLIENT, TV e PAYG_HOURLY, mesmo quando diferem da
normalização literal do Name. PartyTypes foi normalizado para INDIVIDUAL e
ORGANIZATION; IDs 1/2 e traduções foram preservados.

Breaking changes: Create de AddressTypes, AcquisitionSourceTypes, PartyTypes,
StatusDomains e Plans recebe Translations em lugar de Name/Description simples;
DocumentTypes e StatusDefinitions mantêm Translations, agora com en-US obrigatório.
Bulk de AddressTypes/Plans exige EnglishName e admite EnglishDescription, mantendo
Name/Description para pt-PT. Code não é input. Fonte semântica resolvida: GO
arquitetural condicionado aos quality gates, sem autorização de execução SQL.
## Classificação e limites

A = FunctionalCode canónico em inglês, estável e independente de display. B = StandardCode externo; inclui o protocolo HTTP nas propriedades técnicas não persistidas. C = DomainSpecificCode. D = RedundantCode, somente com redundância comprovada. A classificação B descreve a semântica; não afirma validação integral ISO/BCP/HTTP na implementação.

Há **30 propriedades persistidas** correspondentes a `Code`/`*Code`: 8 genéricas A e 22 específicas B/C. Todas as C# string? abaixo têm nullable de referência; isso não equivale a permitir NULL no banco. Adicionalmente, `SubscriptionPlans.Currency` é um conceito B sem sufixo Code. Propriedades de contexto, DTOs e HTTP repetem/transportam conceitos e não representam novas entidades.

## Matriz completa — schema e identidade

As duas matrizes usam a mesma chave Entidade.Propriedade e juntas contêm todos os 16 atributos pedidos. Tipo/tamanho referem-se ao SQL Server; tipo/nullable C# também constam no apêndice.

| Entidade | Tabela dbo | Propriedade | Tipo/tamanho SQL | Nullable | Unique/index/constraint | Classe | Recomendação |
|---|---|---|---|---|---|---|---|
| AddressTypeEntity | AddressTypes | Code | NVARCHAR(50) | SQL/EF obrigatório; C# string? | UX_AddressTypes_Code; único IsDeleted=0 em SQL/EF | A | en-US.Name obrigatório no Create; sem fallback; Code imutável |
| AcquisitionSourceTypeEntity | AcquisitionSourceTypes | Code | NVARCHAR(50) | SQL/EF obrigatório; C# string? | SQL UX_* filtrado IsDeleted=0; EF UQ_* único sem filtro | A | Novos: en-US.Name obrigatório; seeds estáveis; imutável |
| DocumentTypeEntity | DocumentTypes | Code | NVARCHAR(50) | SQL/EF obrigatório; C# string? | UX_DocumentTypes_Code; único IsDeleted=0 em SQL/EF | A | Novos: en-US.Name obrigatório, sem fallback; imutável |
| PartyTypeEntity | PartyTypes | Code | NVARCHAR(50) | SQL/EF obrigatório; C# string? | SQL UX_* filtrado IsDeleted=0; EF UQ_* único sem filtro | A | Novos: en-US.Name obrigatório; seeds INDIVIDUAL/ORGANIZATION IDs1/2; imutável |
| StatusDomainEntity | StatusDomains | Code | NVARCHAR(50) | SQL/EF obrigatório; C# string? | UX_StatusDomains_Code; único IsDeleted=0 em SQL/EF | A | Novos: en-US.Name obrigatório, sem fallback; imutável |
| StatusDefinitionEntity | StatusDefinitions | Code | NVARCHAR(50) | SQL/EF obrigatório; C# string? | UX_StatusDefinitions_Tenant_Domain_Code; TenantId+StatusDomainId+Code IsDeleted=0 | A | Novos: en-US.Name obrigatório; escopo tenant+domínio; imutável |
| FileTypeEntity | FileTypes | Code | NVARCHAR(50) | SQL/EF obrigatório; C# string? | UX_FileTypes_Code; único IsDeleted=0 em SQL/EF; também MimeType+Extension filtrado | A | Extension técnica com aliases JPEG, SEVEN_ZIP e BINARY explícitos; imutável |
| SubscriptionPlanEntity | SubscriptionPlans | Code | NVARCHAR(50) | SQL/EF obrigatório; C# string? | UX_SubscriptionPlans_Code; único IsDeleted=0 em SQL/EF | A | Novos: en-US.Name obrigatório; códigos comerciais existentes estáveis; imutável |
| TenantAddressesEntity | TenantAddresses | CountryCode | CHAR(2) | SQL/EF obrigatório; C# string? | Nenhum específico | B | Preservar ISO alpha-2; não aplicar CatalogCodeGenerator |
| TenantAddressesEntity | TenantAddresses | PostalCode | NVARCHAR(20) | SQL/EF obrigatório; C# string? | Nenhum específico | C | Preservar zeros, hífens e formato do país; não UPPER_SNAKE_CASE |
| ClientAddressesEntity | ClientAddresses | CountryCode | CHAR(2) | SQL/EF obrigatório; C# string? | Nenhum específico | B | Preservar ISO alpha-2; não aplicar CatalogCodeGenerator |
| ClientAddressesEntity | ClientAddresses | PostalCode | NVARCHAR(20) | SQL/EF obrigatório; C# string? | Nenhum específico | C | Preservar zeros, hífens e formato do país; não UPPER_SNAKE_CASE |
| EmployeeAddressesEntity | EmployeeAddresses | CountryCode | CHAR(2) | SQL/EF obrigatório; C# string? | Nenhum específico | B | Preservar ISO alpha-2; não aplicar CatalogCodeGenerator |
| EmployeeAddressesEntity | EmployeeAddresses | PostalCode | NVARCHAR(20) | SQL/EF obrigatório; C# string? | Nenhum específico | C | Preservar zeros, hífens e formato do país; não UPPER_SNAKE_CASE |
| VisitAddressesEntity | VisitAddresses | CountryCode | CHAR(2) | SQL/EF obrigatório; C# string? | Nenhum específico | B | Preservar ISO alpha-2; não aplicar CatalogCodeGenerator |
| VisitAddressesEntity | VisitAddresses | PostalCode | NVARCHAR(20) | SQL/EF obrigatório; C# string? | Nenhum específico | C | Preservar zeros, hífens e formato do país; não UPPER_SNAKE_CASE |
| ClientDocumentsEntity | ClientDocuments | IssuingCountryCode | CHAR(2) | SQL/EF obrigatório; C# string? | UX_ClientDocuments_Type_Country_Number; TenantId+DocumentTypeId+IssuingCountryCode+DocumentNumber; IsDeleted=0 | B | Preservar ISO; registrar conflito de nullable/validação sem alterar comportamento |
| TenantDocumentsEntity | TenantDocuments | IssuingCountryCode | CHAR(2) | SQL/EF obrigatório; C# string? | Nenhum específico com país | B | Preservar ISO; registrar conflito null/obrigatoriedade |
| SubscriptionEntity | Subscriptions | CurrencyCode | CHAR(3) | SQL obrigatório; EF opcional; C# string? | Nenhum específico | B | Preservar ISO4217; divergência SQL/EF exige decisão específica |
| VisitEntity | Visits | CurrencyCode | CHAR(3) | SQL/EF obrigatório; C# string | Nenhum específico | B | Preservar ISO4217; não gerar por display |
| UserPreferencesEntity | UserPreferences | CurrencyCode | NVARCHAR(3) | SQL/EF obrigatório; C# string? | Nenhum específico | B | Preservar standard; valor não é identidade funcional da preferência |
| ClientEntity | Clients | EconomicActivityCode | NVARCHAR(20) | SQL/EF nullable; C# string? | CK_Clients_PartyTypeData; sem índice específico | C | Preservar CAE/domínio fiscal; IDs hardcoded são risco independente |
| PartyTypeTranslationsEntity | PartyTypeTranslations | LanguageCode | NVARCHAR(10) | SQL/EF obrigatório; C# string? | UQ ParentId+LanguageCode; UQ LanguageCode+Name | B | Preservar tags pt-PT/pt-BR/en-US/es-ES; default pt-PT não é código funcional |
| AcquisitionSourceTypeTranslationsEntity | AcquisitionSourceTypeTranslations | LanguageCode | NVARCHAR(10) | SQL/EF obrigatório; C# string? | UQ ParentId+LanguageCode; UQ LanguageCode+Name | B | Preservar tags pt-PT/pt-BR/en-US/es-ES; default pt-PT não é código funcional |
| AddressTypeTranslationsEntity | AddressTypeTranslations | LanguageCode | NVARCHAR(10) | SQL/EF obrigatório; C# string? | UQ ParentId+LanguageCode; UQ LanguageCode+Name | B | Preservar tags pt-PT/pt-BR/en-US/es-ES; default pt-PT não é código funcional |
| DocumentTypeTranslationsEntity | DocumentTypeTranslations | LanguageCode | NVARCHAR(10) | SQL/EF obrigatório; C# string? | UQ ParentId+LanguageCode; UQ LanguageCode+Name | B | Preservar tags pt-PT/pt-BR/en-US/es-ES; default pt-PT não é código funcional |
| FileTypeTranslationsEntity | FileTypeTranslations | LanguageCode | NVARCHAR(10) | SQL/EF obrigatório; C# string? | UQ ParentId+LanguageCode; UQ LanguageCode+Name | B | Preservar tags pt-PT/pt-BR/en-US/es-ES; default pt-PT não é código funcional |
| StatusDomainTranslationsEntity | StatusDomainTranslations | LanguageCode | NVARCHAR(10) | SQL/EF obrigatório; C# string? | UQ ParentId+LanguageCode; UQ LanguageCode+Name | B | Preservar tags pt-PT/pt-BR/en-US/es-ES; default pt-PT não é código funcional |
| StatusDefinitionTranslationsEntity | StatusDefinitionTranslations | LanguageCode | NVARCHAR(10) | SQL/EF obrigatório; C# string? | UQ TenantId+StatusDefinitionId+LanguageCode; UQ TenantId+StatusDomainId+LanguageCode+Name | B | Preservar tags pt-PT/pt-BR/en-US/es-ES; default pt-PT não é código funcional |
| SubscriptionPlanTranslationsEntity | SubscriptionPlanTranslations | LanguageCode | NVARCHAR(10) | SQL/EF obrigatório; C# string? | UQ ParentId+LanguageCode; UQ LanguageCode+Name | B | Preservar tags pt-PT/pt-BR/en-US/es-ES; default pt-PT não é código funcional |

## Matriz completa — proveniência e consumidores

| Entidade.Propriedade | Origem do valor auditada | Onde utilizado | Regra de negócio | Integração | Seed | Import/export | Response |
|---|---|---|---|---|---|---|---|
| AddressTypeEntity.Code | Translations[en-US].Name no Create; EnglishName no Bulk (antes: Name pt-PT) | Create/Bulk e seed; não há consumo funcional externo comprovado | Identidade funcional do catálogo; não confundir com FK Id | Não encontrada no código auditado | Sim | Bulk CSV implementado; exportador dedicado não encontrado | Somente DetailResponse; list Response NÃO expõe Code |
| AcquisitionSourceTypeEntity.Code | Translations[en-US].Name no Create (antes: Name pt-PT) | Create e seed; existência consulta IgnoreQueryFilters | Identidade funcional do catálogo; não confundir com FK Id | Não encontrada no código auditado | Sim | Bulk retorna 501; DTO presente não prova importação; exportador dedicado não encontrado | Somente DetailResponse; list Response NÃO expõe Code |
| DocumentTypeEntity.Code | Create.Translations[en-US].Name (antes: tradução pt-PT) | Create/ExistsByCode e seed | Identidade funcional do catálogo; não confundir com FK Id | Não encontrada no código auditado | Sim | Sem Bulk encontrado; exportador dedicado não encontrado | Somente DetailResponse; list Response NÃO expõe Code |
| PartyTypeEntity.Code | Translations[en-US].Name no Create (antes: Name pt-PT) | Create/ExistsByCode e seed; regra Client ainda usa PartyTypeId1/2 | Identidade funcional do catálogo; não confundir com FK Id | Não encontrada no código auditado | Sim | Sem Bulk encontrado; exportador dedicado não encontrado | Somente DetailResponse; list Response NÃO expõe Code |
| StatusDomainEntity.Code | Translations[en-US].Name no Create (antes: Name pt-PT) | Create/ExistsByCode; seed resolve domínio de StatusDefinitions | Identidade funcional do catálogo; não confundir com FK Id | Não encontrada no código auditado | Sim | Sem Bulk encontrado; exportador dedicado não encontrado | Somente DetailResponse; list Response NÃO expõe Code |
| StatusDefinitionEntity.Code | Create.Translations[en-US].Name (antes: tradução pt-PT) | Create/ExistsByCode; seed resolve estados por domínio | Identidade funcional do catálogo; não confundir com FK Id | Não encontrada no código auditado | Sim | Sem Bulk encontrado; exportador dedicado não encontrado | Somente DetailResponse; list Response NÃO expõe Code |
| FileTypeEntity.Code | Extension técnica em Create/Bulk | Formato técnico; Create/ExistsByCode e seed | Identidade funcional do catálogo; não confundir com FK Id | Não encontrada no código auditado | Sim | Bulk CSV implementado; exportador dedicado não encontrado | Somente DetailResponse; list Response NÃO expõe Code |
| SubscriptionPlanEntity.Code | Translations[en-US].Name no Create; EnglishName no Bulk (antes: Name pt-PT) | Identidade comercial em seed e unicidade de Create | Identidade funcional do catálogo; não confundir com FK Id | Não encontrada no código auditado | Sim | Bulk CSV implementado; exportador dedicado não encontrado | Somente DetailResponse; list Response NÃO expõe Code |
| TenantAddressesEntity.CountryCode | Create/Update request; SQL default PT; TenantAddressesAppService aplica fallback PT | TenantAddressesAppService, mapping e pesquisa CountryCode em TenantAddressesDataRepository | País do endereço | Não encontrada no código auditado | Sim | Sem Bulk; exportador dedicado não encontrado | Response e DetailResponse |
| TenantAddressesEntity.PostalCode | Create/Update request | TenantAddressesAppService e pesquisa PostalCode em TenantAddressesDataRepository | Endereço postal | Não encontrada no código auditado | Sim | Sem Bulk; exportador dedicado não encontrado | DetailResponse |
| ClientAddressesEntity.CountryCode | Create/Update request e CSV; SQL default PT | ClientAddressAppService, mapping e deduplicação por CountryCode em ClientAddressRepository | País do endereço | Não encontrada no código auditado | Sim | Bulk CSV; exportador dedicado não encontrado | Response e DetailResponse |
| ClientAddressesEntity.PostalCode | Create/Update request e CSV | ClientAddressAppService, pesquisa e deduplicação em ClientAddressRepository | Endereço postal | Não encontrada no código auditado | Sim | Bulk CSV; exportador dedicado não encontrado | DetailResponse |
| EmployeeAddressesEntity.CountryCode | Create/Update request e CSV; SQL default PT | EmployeeAddressAppService e mapping; deduplicação usa Street/City/PostalCode, sem país | País do endereço | Não encontrada no código auditado | Sim | Bulk CSV; exportador dedicado não encontrado | Response e DetailResponse |
| EmployeeAddressesEntity.PostalCode | Create/Update request e CSV | EmployeeAddressAppService, pesquisa e deduplicação em EmployeeAddressDataRepository | Endereço postal | Não encontrada no código auditado | Sim | Bulk CSV; exportador dedicado não encontrado | DetailResponse |
| VisitAddressesEntity.CountryCode | Create/Update request e CSV; SQL default PT; constructor fallback PT | VisitAddressAppService e mapping; deduplicação usa Street/City/PostalCode, sem país | País do endereço | Não encontrada no código auditado | Sim | Bulk CSV; exportador dedicado não encontrado | Response e DetailResponse |
| VisitAddressesEntity.PostalCode | Create/Update request e CSV | VisitAddressAppService, pesquisa e deduplicação em VisitAddressDataRepository | Endereço postal | Não encontrada no código auditado | Sim | Bulk CSV; exportador dedicado não encontrado | DetailResponse |
| ClientDocumentsEntity.IssuingCountryCode | Constructor recebe valor e fallback PT; Update pode atribuir null | Persistência EF, validators ClientDocument e seed | País na identidade documental | Não encontrada no código auditado | Sim | Sem DTO/Bulk público encontrado; exportador dedicado não encontrado | Sem DTO response encontrado |
| TenantDocumentsEntity.IssuingCountryCode | Constructor recebe valor e fallback PT; Update pode atribuir null | Persistência EF e seed v2 | País emissor | Não encontrada no código auditado | Sim v2 | Sem DTO/Bulk público encontrado; exportador dedicado não encontrado | Sem DTO response encontrado |
| SubscriptionEntity.CurrencyCode | Create/Update/Bulk request; constructor sem fallback | SubscriptionAppService e montante contratado | Denominação monetária | Não encontrada no código auditado | Sim | Bulk CSV; exportador dedicado não encontrado | DetailResponse |
| VisitEntity.CurrencyCode | Create/Update/Bulk request; default EUR | VisitAppService e valores EstimatedValue/RealValue | Denominação monetária | Não encontrada no código auditado | Sim v2 | Bulk CSV; exportador dedicado não encontrado | DetailResponse |
| UserPreferencesEntity.CurrencyCode | Create/Update request; SQL/EF default EUR | UserPreferencesAppService e preferência | Preferência de moeda de apresentação | Não encontrada no código auditado | Sim | Sem Bulk; exportador dedicado não encontrado | Response e DetailResponse |
| ClientEntity.EconomicActivityCode | Create/Update request; Bulk geral atribui null | ClientAppService valida consistência; repository pesquisa; validators | Proibido para pessoa singular PartyTypeId1; atividade empresarial | Não encontrada no código auditado | Sim | Bulk geral não importa este campo; exportador dedicado não encontrado | DetailResponse |
| PartyTypeTranslationsEntity.LanguageCode | Seed; Create recebe Translations com en-US; Update preserva Code; endpoint explícito de tradução | TranslationResolver, mapping, seleção de tradução e unicidade | Identificação cultural da tradução | Não encontrada no código auditado | Sim | Sem Bulk; exportador dedicado não encontrado | LanguageCode resolvido no Response/DetailResponse do pai |
| AcquisitionSourceTypeTranslationsEntity.LanguageCode | Seed; Create recebe Translations com en-US; Update preserva Code | TranslationResolver, mapping, seleção de tradução e unicidade | Identificação cultural da tradução | Não encontrada no código auditado | Sim | Bulk do pai retorna 501; sem importação de idioma ou exportador dedicado | LanguageCode resolvido no Response/DetailResponse do pai |
| AddressTypeTranslationsEntity.LanguageCode | Seed; Create recebe Translations com en-US; Bulk recebe EnglishName/en-US e Name/pt-PT; Update preserva Code | TranslationResolver, mapping, seleção de tradução e unicidade | Identificação cultural da tradução | Não encontrada no código auditado | Sim | Bulk exige EnglishName/en-US e mantém Name/pt-PT; sem exportador dedicado | LanguageCode resolvido no Response/DetailResponse do pai |
| DocumentTypeTranslationsEntity.LanguageCode | Seed; Create.Translations exige en-US; endpoints de tradução não recalculam Code | TranslationResolver, mapping, seleção de tradução e unicidade | Identificação cultural da tradução | Não encontrada no código auditado | Sim | Sem Bulk; exportador dedicado não encontrado | LanguageCode resolvido no Response/DetailResponse do pai |
| FileTypeTranslationsEntity.LanguageCode | Seed; Create/Bulk atuais não recebem nem criam traduções | TranslationResolver, mapping, seleção de tradução e unicidade | Identificação cultural da tradução | Não encontrada no código auditado | Sim | Bulk importa MIME/Extension, sem LanguageCode/tradução; sem exportador dedicado | LanguageCode resolvido no Response/DetailResponse do pai |
| StatusDomainTranslationsEntity.LanguageCode | Seed; Create recebe Translations com en-US; Update preserva Code; endpoint explícito de tradução | TranslationResolver, mapping, seleção de tradução e unicidade | Identificação cultural da tradução | Não encontrada no código auditado | Sim | Sem Bulk; exportador dedicado não encontrado | LanguageCode resolvido no Response/DetailResponse do pai |
| StatusDefinitionTranslationsEntity.LanguageCode | Seed; Create.Translations exige en-US; endpoints de tradução não recalculam Code | TranslationResolver, mapping, seleção de tradução e unicidade | Identificação cultural da tradução | Não encontrada no código auditado | Sim | Sem Bulk; exportador dedicado não encontrado | LanguageCode resolvido no Response/DetailResponse do pai |
| SubscriptionPlanTranslationsEntity.LanguageCode | Seed; Create recebe Translations com en-US; Bulk EnglishName/en-US e Name/pt-PT; Update não recalcula Code | TranslationResolver, mapping, seleção de tradução e unicidade | Identificação cultural da tradução | Não encontrada no código auditado | Sim | Bulk exige EnglishName/en-US e mantém Name/pt-PT; sem exportador dedicado | LanguageCode resolvido no Response/DetailResponse do pai |

## Escopo global, tenant e divergências

- PartyTypes, AddressTypes, AcquisitionSourceTypes, DocumentTypes, FileTypes, StatusDomains e SubscriptionPlans são catálogos globais; StatusDefinitions é por tenant e domínio. Id continua sendo a FK relacional.
- AcquisitionSourceTypes.Code: SQL declara `UX_AcquisitionSourceTypes_Code WHERE IsDeleted=0`; EF declara `UQ_AcquisitionSourceTypes_Code` único sem filtro. O ExistsByCode usa IgnoreQueryFilters, reservado globalmente inclusive removidos. PartyTypes também difere: SQL filtrado e EF UQ sem filtro. Não assumir equivalência entre nome UQ e constraint SQL: no EF é HasIndex.IsUnique.
- StatusDefinitions: índice único TenantId+StatusDomainId+Code; a consulta ExistsByCode recebe domínio e depende do escopo/filtro de tenant. RLS/session context deve ser respeitado; não tratar Code como global.
- Plan, AddressType e FileType ExistsByCode consultam entidades sem predicate explícito de IsDeleted; observar query filters efetivos. Unicidade filtrada do SQL não basta para inferir comportamento de aplicação.
- Generic Code aparece apenas nos oito DTOs DetailResponse, não nos DTOs de listagem Response. Nenhum request normal dos oito recebe Code no working tree auditado.
- Subscriptions.CurrencyCode: SQL NOT NULL versus EF IsRequired(false); C# constructor e Update aceitam null. Não corrigir silenciosamente ao normalizar FunctionalCode.
- ClientDocuments/TenantDocuments.IssuingCountryCode: C# nullable e Update atribui null, mas SQL/EF obrigatórios. Validator ClientDocument só valida tamanho quando preenchido; comentário SQL opcional contradiz NOT NULL.
- CountryCode e CurrencyCode têm validações predominantemente de tamanho, sem prova de catálogo ISO completo. EconomicActivityCode é proibido para PartyTypeId1; regras ainda usam IDs1/2, não Code.
- AcquisitionSourceType BulkUpload devolve 501: DTO/rota não equivalem a importação implementada.

## Conceitos B sem coluna adicional Code

| Conceito | Persistência/tipo | Origem/uso | Índice/nullable | API/seed/import | Recomendação |
|---|---|---|---|---|---|
| SubscriptionPlanEntity.Currency | dbo.SubscriptionPlans.Currency; NVARCHAR(3) | Create/Update/Bulk de Plan; preços comerciais | Sem índice específico; C# string?, EF/SQL obrigatórios | DetailResponse, seed, Bulk CSV; integração externa não encontrada | B, moeda ISO4217; preservar e nunca CatalogCodeGenerator |
| RequestLanguageContext.LanguageCode e IRequestLanguageContext.LanguageCode | Não persistidos; string | HttpContext.Items RequestCulture, SupportedLanguages, fallback pt-PT | Sem índice/coluna; não nullable | Contexto usado por mapeamento/resolução; não é novo catálogo | B, tags culturais preservadas |
| ErrorNotify.StatusCode / ViewResult.StatusCode / INotify.GetStatusCode | Não persistidos; HttpStatusCode | Acumulador de notificação e seleção de resposta HTTP | Sem índice/coluna; valor enum | Serialização/transporte técnico de status; não seed/import de catálogo | B, protocolo HTTP; não FunctionalCode |

SupportedLanguages é `[pt-PT, pt-BR, en-US, es-ES]`, default pt-PT. LanguageCode retornado é o idioma realmente resolvido. `StatusCodes` do ASP.NET, `HttpStatusCode`, parâmetros statusCode e `.Produces(status)` são códigos de protocolo; `GetHashCode` é método de igualdade .NET, não campo.

## Autorização: análise sem mudança automática

Actions/Resources/Roles não possuem Code no estado auditado. Name é identificador semântico imutável (Update altera somente Description). AuthAppService usa Role.Name, Resource.Name e Action.Name, normalizados para lowercase nas claims. AuthorizationFilter/CustomAuthorize usam os mesmos identificadores textuais. RolePermissions e UserRoles mantêm FKs por IDs.

| Entidade | Benefício de Code separado | Risco e recomendação |
|---|---|---|
| Actions | GET_ALL/GET_BY separam identificador funcional de display traduzível | getby difere de get_by; não restaurar automaticamente. Migração versionada futura com aliases/tokens antigos |
| Resources | ACTIONS/CLIENTS/VISITS permitem display editável e identidade portátil | Alguns lowercase coincidem, mas não é garantia universal. Preservar Names imutáveis agora |
| Roles | ADMIN/BACK_OFFICE/MANAGER/OPERATOR permitem display livre e chave por tenant | backoffice difere de back_office; requer TenantId+Code, política de roles customizadas e compatibilidade de claims |

O Code anterior era redundante no fluxo runtime efetivamente utilizado, mas isso não prova redundância inerente para necessidades futuras de integração/display. Reintroduzir exige decisão arquitetural explícita: mapa Name→Code, atualização coordenada JWT/CustomAuthorize/filtros/seeds/clientes, política de expiração ou aliases para tokens existentes e preservação dos IDs/FKs. Não há mudança automática de autorização nesta auditoria.

## Importação, integração e exemplos

Não foi encontrada integração externa implementada que consuma os Codes auditados; campos Stripe em assinaturas não demonstram consumo de CurrencyCode por Stripe. Seeds usam Code para resolver IDs entre ambientes; essa portabilidade é evidência real, diferente de integração futura hipotética. BulkUpload usa majoritariamente IDs relacionais; oferecer importação por Code é oportunidade que exige contrato versionado e resolução por escopo. Não existe exportador dedicado encontrado.

A coleção Postman contém CurrencyCode USD e locale pt-BR válidos semanticamente, além de placeholders CountryCode string incompatíveis com tamanho2. Exemplos não comprovam validação ISO. CSV não deve remover hífens/zeros de PostalCode nem converter LanguageCode em UPPER_SNAKE_CASE.

## Apêndice: declarações C# Code/*Code e moeda sem sufixo

Lista extraída de propriedades declaradas em DTOs, domínio, readmodels, contextos e notificações no working tree. Interfaces/contextos e respostas repetem conceitos da matriz. Métodos e parâmetros não são contados como propriedades persistidas. A coluna nullable descreve sintaxe C#, não nulabilidade do schema. `Code` num DTO Detail pertence ao catálogo do diretório, não cria entidade adicional.

| Arquivo:linha | Tipo/classe | Propriedade | Tipo C# | Nullable sintático | Classe semântica |
|---|---|---|---|---|---|
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Billing/Plan/BulkUploadPlanItem.cs:13 | BulkUploadPlanItem | Currency | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Billing/Plan/CreatePlanRequest.cs:11 | CreatePlanRequest | Currency | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Billing/Plan/CreatePlanTranslationRequest.cs:5 | CreatePlanTranslationRequest | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Billing/Plan/UpdatePlanRequest.cs:11 | UpdatePlanRequest | Currency | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Billing/Subscription/BulkUploadSubscriptionItem.cs:11 | BulkUploadSubscriptionItem | CurrencyCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Billing/Subscription/CreateSubscriptionRequest.cs:11 | CreateSubscriptionRequest | CurrencyCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Billing/Subscription/UpdateSubscriptionRequest.cs:10 | UpdateSubscriptionRequest | CurrencyCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Billing/TenantAddress/CreateTenantAddressRequest.cs:6 | CreateTenantAddressRequest | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Billing/TenantAddress/CreateTenantAddressRequest.cs:11 | CreateTenantAddressRequest | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Billing/TenantAddress/UpdateTenantAddressRequest.cs:6 | UpdateTenantAddressRequest | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Billing/TenantAddress/UpdateTenantAddressRequest.cs:11 | UpdateTenantAddressRequest | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/AcquisitionSourceType/CreateAcquisitionSourceTypeTranslationRequest.cs:5 | CreateAcquisitionSourceTypeTranslationRequest | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/AddressType/CreateAddressTypeTranslationRequest.cs:5 | CreateAddressTypeTranslationRequest | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/Client/CreateClientRequest.cs:21 | CreateClientRequest | EconomicActivityCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/Client/UpdateClientRequest.cs:24 | UpdateClientRequest | EconomicActivityCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/ClientAddress/BulkUploadClientAddressItem.cs:9 | BulkUploadClientAddressItem | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/ClientAddress/BulkUploadClientAddressItem.cs:16 | BulkUploadClientAddressItem | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/ClientAddress/CreateClientAddressRequest.cs:9 | CreateClientAddressRequest | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/ClientAddress/CreateClientAddressRequest.cs:16 | CreateClientAddressRequest | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/ClientAddress/UpdateClientAddressRequest.cs:9 | UpdateClientAddressRequest | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/ClientAddress/UpdateClientAddressRequest.cs:16 | UpdateClientAddressRequest | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/DocumentType/CreateDocumentTypeTranslationRequest.cs:8 | CreateDocumentTypeTranslationRequest | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/EmployeeAddress/BulkUploadEmployeeAddressItem.cs:10 | BulkUploadEmployeeAddressItem | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/EmployeeAddress/BulkUploadEmployeeAddressItem.cs:17 | BulkUploadEmployeeAddressItem | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/EmployeeAddress/CreateEmployeeAddressRequest.cs:9 | CreateEmployeeAddressRequest | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/EmployeeAddress/CreateEmployeeAddressRequest.cs:16 | CreateEmployeeAddressRequest | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/EmployeeAddress/UpdateEmployeeAddressRequest.cs:9 | UpdateEmployeeAddressRequest | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/EmployeeAddress/UpdateEmployeeAddressRequest.cs:15 | UpdateEmployeeAddressRequest | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/PartyType/CreatePartyTypeTranslationRequest.cs:8 | CreatePartyTypeTranslationRequest | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/StatusDefinition/CreateStatusDefinitionTranslationRequest.cs:8 | CreateStatusDefinitionTranslationRequest | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/StatusDomain/CreateStatusDomainTranslationRequest.cs:8 | CreateStatusDomainTranslationRequest | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/VisitAddress/BulkUploadVisitAddressItem.cs:7 | BulkUploadVisitAddressItem | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/VisitAddress/BulkUploadVisitAddressItem.cs:14 | BulkUploadVisitAddressItem | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/VisitAddress/CreateVisitAddressRequest.cs:9 | CreateVisitAddressRequest | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/VisitAddress/CreateVisitAddressRequest.cs:16 | CreateVisitAddressRequest | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/VisitAddress/UpdateVisitAddressRequest.cs:9 | UpdateVisitAddressRequest | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/VisitAddress/UpdateVisitAddressRequest.cs:16 | UpdateVisitAddressRequest | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/Vist/BulkUploadVisitItem.cs:8 | BulkUploadVisitItem | CurrencyCode | string | Não | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/Vist/CreateVisitRequest.cs:8 | CreateVisitRequest | CurrencyCode | string | Não | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Business/Vist/UpdateVisitRequest.cs:8 | UpdateVisitRequest | CurrencyCode | string | Não | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Identity/UserPreferences/CreateUserPreferencesRequest.cs:6 | CreateUserPreferencesRequest | CurrencyCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Request/Identity/UserPreferences/UpdateUserPreferencesRequest.cs:6 | UpdateUserPreferencesRequest | CurrencyCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Billing/Plan/PlanDetailResponse.cs:6 | PlanDetailResponse | Code | string? | Sim | A |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Billing/Plan/PlanDetailResponse.cs:7 | PlanDetailResponse | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Billing/Plan/PlanDetailResponse.cs:14 | PlanDetailResponse | Currency | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Billing/Plan/PlanResponse.cs:6 | PlanResponse | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Billing/Subscription/SubscriptionDetailResponse.cs:15 | SubscriptionDetailResponse | CurrencyCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Billing/TenantAddress/TenantAddressDetailResponse.cs:9 | TenantAddressDetailResponse | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Billing/TenantAddress/TenantAddressDetailResponse.cs:14 | TenantAddressDetailResponse | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Billing/TenantAddress/TenantAddressResponse.cs:7 | TenantAddressResponse | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/AcquisitionSourceType/AcquisitionSourceTypeDetailResponse.cs:10 | AcquisitionSourceTypeDetailResponse | Code | string? | Sim | A |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/AcquisitionSourceType/AcquisitionSourceTypeDetailResponse.cs:11 | AcquisitionSourceTypeDetailResponse | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/AcquisitionSourceType/AcquisitionSourceTypeResponse.cs:6 | AcquisitionSourceTypeResponse | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/AddressType/AddressTypeDetailResponse.cs:11 | AddressTypeDetailResponse | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/AddressType/AddressTypeDetailResponse.cs:12 | AddressTypeDetailResponse | Code | string? | Sim | A |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/AddressType/AddressTypeResponse.cs:6 | AddressTypeResponse | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/Client/ClientDetailResponse.cs:26 | ClientDetailResponse | EconomicActivityCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/ClientAddress/ClientAddressDetailResponse.cs:17 | ClientAddressDetailResponse | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/ClientAddress/ClientAddressDetailResponse.cs:24 | ClientAddressDetailResponse | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/ClientAddress/ClientAddressResponse.cs:10 | ClientAddressResponse | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/DocumentType/DocumentTypeDetailResponse.cs:9 | DocumentTypeDetailResponse | Code | string? | Sim | A |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/DocumentType/DocumentTypeDetailResponse.cs:10 | DocumentTypeDetailResponse | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/DocumentType/DocumentTypeResponse.cs:9 | DocumentTypeResponse | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/EmployeeAddress/EmployeeAddressDetailResponse.cs:17 | EmployeeAddressDetailResponse | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/EmployeeAddress/EmployeeAddressDetailResponse.cs:18 | EmployeeAddressDetailResponse | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/EmployeeAddress/EmployeeAddressResponse.cs:10 | EmployeeAddressResponse | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/FileType/FileTypeDetailResponse.cs:12 | FileTypeDetailResponse | Code | string? | Sim | A |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/FileType/FileTypeDetailResponse.cs:15 | FileTypeDetailResponse | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/FileType/FileTypeResponse.cs:6 | FileTypeResponse | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/PartyType/PartyTypeDetailResponse.cs:10 | PartyTypeDetailResponse | Code | string? | Sim | A |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/PartyType/PartyTypeDetailResponse.cs:11 | PartyTypeDetailResponse | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/PartyType/PartyTypeResponse.cs:9 | PartyTypeResponse | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/StatusDefinition/StatusDefinitionDetailResponse.cs:11 | StatusDefinitionDetailResponse | Code | string? | Sim | A |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/StatusDefinition/StatusDefinitionDetailResponse.cs:12 | StatusDefinitionDetailResponse | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/StatusDefinition/StatusDefinitionResponse.cs:9 | StatusDefinitionResponse | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/StatusDomain/StatusDomainDetailResponse.cs:9 | StatusDomainDetailResponse | Code | string? | Sim | A |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/StatusDomain/StatusDomainDetailResponse.cs:10 | StatusDomainDetailResponse | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/StatusDomain/StatusDomainResponse.cs:9 | StatusDomainResponse | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/Visit/VisitDetailResponse.cs:18 | VisitDetailResponse | CurrencyCode | string | Não | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/VisitAddress/VisitAddressDetailResponse.cs:11 | VisitAddressDetailResponse | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/VisitAddress/VisitAddressDetailResponse.cs:18 | VisitAddressDetailResponse | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Business/VisitAddress/VisitAddressResponse.cs:10 | VisitAddressResponse | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Identity/UserPreferences/UserPreferencesDetailResponse.cs:16 | UserPreferencesDetailResponse | CurrencyCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Application/Dtos/Response/Identity/UserPreferences/UserPreferencesResponse.cs:12 | UserPreferencesResponse | CurrencyCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Billing/SubscriptionEntity.cs:17 | SubscriptionEntity | CurrencyCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Billing/SubscriptionPlanEntity.cs:13 | SubscriptionPlanEntity | Code | string? | Sim | A |
| src/VianaHub.Global.Gerit.Domain/Entities/Billing/SubscriptionPlanEntity.cs:18 | SubscriptionPlanEntity | Currency | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Billing/SubscriptionPlanTranslationsEntity.cs:11 | SubscriptionPlanTranslationsEntity | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Billing/TenantAddressesEntity.cs:13 | TenantAddressesEntity | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Billing/TenantAddressesEntity.cs:18 | TenantAddressesEntity | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/AcquisitionSourceTypeEntity.cs:12 | AcquisitionSourceTypeEntity | Code | string? | Sim | A |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/AcquisitionSourceTypeTranslationsEntity.cs:11 | AcquisitionSourceTypeTranslationsEntity | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/AddressTypeEntity.cs:11 | AddressTypeEntity | Code | string? | Sim | A |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/AddressTypeTranslationsEntity.cs:11 | AddressTypeTranslationsEntity | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/ClientAddressesEntity.cs:11 | ClientAddressesEntity | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/ClientAddressesEntity.cs:18 | ClientAddressesEntity | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/ClientDocumentsEntity.cs:17 | ClientDocumentsEntity | IssuingCountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/ClientEntity.cs:30 | ClientEntity | EconomicActivityCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/DocumentTypeEntity.cs:11 | DocumentTypeEntity | Code | string? | Sim | A |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/DocumentTypeTranslationsEntity.cs:11 | DocumentTypeTranslationsEntity | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/EmployeeAddressesEntity.cs:14 | EmployeeAddressesEntity | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/EmployeeAddressesEntity.cs:19 | EmployeeAddressesEntity | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/FileTypeEntity.cs:9 | FileTypeEntity | Code | string? | Sim | A |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/FileTypeTranslationsEntity.cs:11 | FileTypeTranslationsEntity | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/PartyTypeEntity.cs:12 | PartyTypeEntity | Code | string? | Sim | A |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/PartyTypeTranslationsEntity.cs:11 | PartyTypeTranslationsEntity | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/StatusDefinitionEntity.cs:14 | StatusDefinitionEntity | Code | string? | Sim | A |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/StatusDefinitionTranslationsEntity.cs:13 | StatusDefinitionTranslationsEntity | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/StatusDomainEntity.cs:11 | StatusDomainEntity | Code | string? | Sim | A |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/StatusDomainTranslationsEntity.cs:11 | StatusDomainTranslationsEntity | LanguageCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/TenantDocumentsEntity.cs:16 | TenantDocumentsEntity | IssuingCountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/VisitAddressesEntity.cs:14 | VisitAddressesEntity | CountryCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/VisitAddressesEntity.cs:21 | VisitAddressesEntity | PostalCode | string? | Sim | C |
| src/VianaHub.Global.Gerit.Domain/Entities/Business/VisitEntity.cs:16 | VisitEntity | CurrencyCode | string | Não | B |
| src/VianaHub.Global.Gerit.Domain/Entities/Identity/UserPreferencesEntity.cs:12 | UserPreferencesEntity | CurrencyCode | string? | Sim | B |
| src/VianaHub.Global.Gerit.Domain/Interfaces/Base/IRequestLanguageContext.cs:12 | IRequestLanguageContext | LanguageCode | string | Não | B |
| src/VianaHub.Global.Gerit.Domain/Tools/Notifications/ErrorNotify.cs:7 | ErrorNotify | StatusCode | HttpStatusCode | Não | B |
| src/VianaHub.Global.Gerit.Domain/Tools/Notifications/ViewResult.cs:8 | ViewResult | StatusCode | HttpStatusCode | Não | B |
| src/VianaHub.Global.Gerit.Infra.Data/Context/RequestLanguageContext.cs:21 | RequestLanguageContext | LanguageCode | string | Não | B |

Total de declarações listadas: **119** (inclui transporte/contexto; não somar como entidades).

`CatalogCodeGenerator.CanonicalLanguageCode` é a constante técnica B `en-US` que seleciona a fonte semântica; não é um novo FunctionalCode de entidade.

## Apêndice: aliases e identificadores SQL Code/*Code

Aliases, parâmetros e colunas de tabelas-variável/CTEs não são novas propriedades persistidas. Code referencia o catálogo do bloco; DomainCode/StatusDomainCode e StatusCode em seeds representam StatusDomains.Code e StatusDefinitions.Code; estes StatusCode não são HTTP. PartyTypeCode, AddressTypeCode, AcquisitionSourceCode e DocumentTypeCode resolvem as respectivas FKs por catálogo. Nomes de constraints/índices e strings de mensagens não são colunas. A tabela abaixo dá todos os identificadores lexicais Code/*Code encontrados em scripts SQL, com arquivos/linhas para conferir contexto; referências de migração a Actions/Resources/Roles.Code são históricas, não restauração do campo.

| Identificador | Arquivos:linhas | Classificação/contexto |
|---|---|---|
| AcquisitionSourceCode | docs/sql/Initial_BackOffice-v2.sql:752; docs/sql/Initial_BackOffice.sql:1179 | A; referência/alias de catálogo, conferir bloco; não entidade adicional |
| AddressTypeCode | docs/sql/Initial_BackOffice-v2.sql:754; docs/sql/Initial_BackOffice.sql:721 | A; referência/alias de catálogo, conferir bloco; não entidade adicional |
| CanonicalCode | docs/sql/Migrate-Normalize-Functional-Codes.sql:24 | A; referência/alias de catálogo, conferir bloco; não entidade adicional |
| CountryCode | docs/sql/Create-Tables.sql:268; docs/sql/Initial_BackOffice-v2.sql:491; docs/sql/Initial_BackOffice.sql:722 | B; coluna ou transporte do standard |
| CurrencyCode | docs/sql/Create-Tables.sql:390; docs/sql/Initial_BackOffice-v2.sql:607; docs/sql/Initial_BackOffice.sql:940 | B; coluna ou transporte do standard |
| DocumentTypeCode | docs/sql/Initial_BackOffice-v2.sql:759; docs/sql/Initial_BackOffice.sql:1195 | A; referência/alias de catálogo, conferir bloco; não entidade adicional |
| DomainCode | docs/sql/Initial_BackOffice-v2.sql:526; docs/sql/Initial_BackOffice.sql:816 | A; referência/alias de catálogo, conferir bloco; não entidade adicional |
| EconomicActivityCode | docs/sql/Create-Tables.sql:654; docs/sql/Initial_BackOffice-v2.sql:751; docs/sql/Initial_BackOffice.sql:1317 | C; coluna/transporte de domínio específico |
| IntendedCode | docs/sql/Migrate-Normalize-Functional-Codes.sql:43 | A; referência/alias de catálogo, conferir bloco; não entidade adicional |
| IssuingCountryCode | docs/sql/Create-Tables.sql:324; docs/sql/Initial_BackOffice-v2.sql:515; docs/sql/Initial_BackOffice.sql:1455 | B; coluna ou transporte do standard |
| LanguageCode | docs/sql/Create-Tables.sql:20; docs/sql/Initial_BackOffice-v2.sql:62; docs/sql/Initial_BackOffice.sql:70 | B; coluna ou transporte do standard |
| PartyTypeCode | docs/sql/Initial_BackOffice-v2.sql:747 | A; referência/alias de catálogo, conferir bloco; não entidade adicional |
| PostalCode | docs/sql/Create-Tables.sql:273; docs/sql/Initial_BackOffice-v2.sql:491; docs/sql/Initial_BackOffice.sql:727 | C; coluna/transporte de domínio específico |
| PreservedUnknownCode | docs/sql/Migrate-Normalize-Functional-Codes.sql:63 | A; referência/alias de catálogo, conferir bloco; não entidade adicional |
| PreviousCode | docs/sql/Migrate-Normalize-Functional-Codes.sql:23 | A; referência/alias de catálogo, conferir bloco; não entidade adicional |
| StatusCode | docs/sql/Initial_BackOffice-v2.sql:526; docs/sql/Initial_BackOffice.sql:817 | A; referência/alias de catálogo, conferir bloco; não entidade adicional |

O identificador simples `Code` (sem prefixo) aparece nos oito catálogos e nas estruturas auxiliares dos seeds/migrações; sua classificação depende da tabela alvo. Nos objetos históricos Actions/Resources/Roles, a migração remove a duplicação sem alterar IDs. Campos funcionais constantes em SQL não devem ser classificados por casing de alias.

## Fontes e verificabilidade

Fontes primárias: `src/VianaHub.Global.Gerit.Domain/Entities`, `src/VianaHub.Global.Gerit.Application/Dtos`, `Services`, `Mappings`, `src/VianaHub.Global.Gerit.Infra.Data/Mappings`, `Repository`, `Context`, validators API/domínio, `docs/sql/Create-Tables.sql`, `Initial_BackOffice.sql`, `Initial_BackOffice-v2.sql`, `docs/collection/Gerit.postman_collection.json`. A matriz não substitui inventário dos valores de seed atual→canónico no documento arquitetural/migração. Nenhum script foi executado contra bases e nenhuma integração externa foi presumida.
