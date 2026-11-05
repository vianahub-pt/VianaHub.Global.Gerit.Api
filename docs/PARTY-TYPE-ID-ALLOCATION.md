# IDs gerados de PartyType

## Causa e schema atual

O Create original persistia e devolvia zero porque o construtor de `PartyTypeEntity` não atribui ID e a coluna `PartyTypes.Id` era `TINYINT NOT NULL` sem `IDENTITY`. O mapping não configurava geração e o teste unitário forçava 42 por reflection, simulando um comportamento inexistente naquele schema.

O operador alterou o banco separadamente: **`dbo.PartyTypes.Id = TINYINT IDENTITY(1,1)`**, com seed/incremento 1, preservando `1 = INDIVIDUAL` e `2 = ORGANIZATION`. A aplicação não executa essa conversão, reseed, migration ou seed no startup. O antigo registro inválido `Id = 0`, Code `IDEMPOTENCY_5F7130E0A0`, e sua tradução foram removidos pelo operador; esta branch não executa cleanup ou bypass.

## Persistência e traduções

`PartyTypeMapping` usa `UseIdentityColumn(1, 1)` com `TINYINT`. `PartyTypeDataRepository.AddAsync` continua adicionando o agregado e chamando `SaveChangesAsync`. O SQL Server gera a chave e o EF materializa `PartyTypeEntity.Id`; o AppService devolve esse mesmo ID. Não há alocador manual, `MAX+1`, lock de aplicação, reflection ou setter público de ID.

As traduções são adicionadas à collection `entity.Translations` antes da persistência, quando o ID ainda é zero. O relacionamento configurado em `PartyTypeTranslationsMapping` identifica o agregado principal e faz o fixup do FK, primeiro durante tracking e depois ao receber o ID definitivo. O EF grava o grafo atomicamente; a integração SQL verifica os valores persistidos da PK e de todas as FKs. Nenhuma tradução nova deve permanecer com `PartyTypeId = 0`.

Duas conexões concorrentes recebem identificadores distintos do SQL Server. Gaps de identity são normais, inclusive após rollback, e não constituem falha. O limite físico continua sendo 255 por se tratar de `TINYINT`; esta alteração não amplia a coluna nem reinicia o contador. Esgotamento exige planejamento operacional separado, sem wrap ou reuso manual de IDs.

## Scripts SQL

`Create-Tables.sql` descreve a coluna identity. Os inserts explícitos dos seeds 1/2 nesse script e em `Initial_BackOffice.sql` / `Initial_BackOffice-v2.sql` usam `SET IDENTITY_INSERT dbo.PartyTypes ON/OFF`, limitado ao bloco de inserção. `Migrate-Normalize-Functional-Codes.sql` somente normaliza os Codes existentes por UPDATE; não exige alteração. Esses scripts são artefatos operacionais e não são executados pela aplicação ou por esta correção no banco da aplicação.

## Testes

O teste unitário de Create cobre somente a orquestração do AppService e o agregado ainda sem ID. Não simula geração do banco. Os testes SQL criam bases efémeras exclusivas com os mappings reais de PartyType/translations, seeds explícitos 1/2 e conexão criptografada, fornecida por `GERIT_RLS_TEST_CONNECTIONSTRING`. Essas bases são eliminadas no disposal; nenhuma DDL de teste é aplicada ao catálogo da aplicação.

O teardown usa uma conexão própria sem pooling, valida o nome gerado da base e passa esse nome por parâmetro e `QUOTENAME` ao DROP. Evita `EnsureDeletedAsync`, que [limpa todos os pools no provider SQL Server do EF Core](https://github.com/dotnet/efcore/blob/v8.0.22/src/EFCore.SqlServer/Storage/Internal/SqlServerDatabaseCreator.cs) e interfere nos testes existentes que verificam a reutilização da mesma sessão RLS. Nenhum teste JWT/RLS foi alterado.

A integração cobre primeira e segunda criação pelo AppService/domínio/repository reais, ID retornado igual ao materializado e persistido, FKs das traduções, seeds preservados, concorrência em conexões independentes e rollback do grafo inválido sem exigir identidade contígua. O teste HTTP usa somente endpoints existentes e dados descartáveis únicos.

## Shadowing e outros catálogos

`PartyTypeEntity.Id` é `new byte Id`, enquanto `Entity.Id` é int. O EF mapeia a propriedade byte declarada na entidade; os DTOs e o perfil AutoMapper também usam explicitamente essa propriedade. A integração SQL e o HTTP verificam o ID retornado e persistido. O shadowing não gera IDs e continua sendo dívida técnica: reflection genérica/ordenação pode enfrentar `AmbiguousMatchException`, e equality/IsTransient da base merecem análise separada. Não se refatora a hierarquia nesta branch.

A auditoria somente leitura dos outros catálogos (`AcquisitionSourceTypes`, `AddressTypes`, `DocumentTypes`, `EquipmentTypes`, `FileTypes`, `StatusDefinitions`, `StatusDomains`, `SubscriptionPlans`) encontrou PKs int identity. Não foi identificado nesses oito o mesmo conjunto de PK sem identity e Create sem atribuição. Não houve alteração em outros recursos, validators de transição, JWT, RLS, autorização, Hangfire ou Web.

## Validação em 01/11/2026

- `dotnet restore VianaHub.Global.Gerit.sln -warnaserror`: PASS.
- `dotnet build -c Release --no-restore -warnaserror`: PASS, zero warnings/erros.
- `dotnet test -c Release --no-build --no-restore`, com conexão SQL real fornecida ao processo: **1.204 PASS, 0 FAIL, 0 SKIP** (baseline 1.201 + três integrações novas).
- Formatter exato do CI, `dotnet format VianaHub.Global.Gerit.sln --include <três arquivos C# alterados> --verify-no-changes --no-restore`: PASS.
- `git diff --check`: PASS.
- HTTP local entre 07:40:24 e 07:40:34 UTC: login 200; Create **201 / ID 3**, Code `ID_ALLOCATION_TEST_1A1960D8826`; GET 200 em en-US e pt-PT com ID, Code e traduções esperados; Delete 200; repetição 410.
- Consulta somente leitura posterior confirmou ambas as traduções com FK 3, registro descartável soft-deleted pelo endpoint, zero traduções com FK 0, seeds 1/2 intactos e nenhuma base efémera restante. Identity seed/incremento = 1/1; contador = 3 após o Create HTTP.

A API iniciada para o teste foi encerrada. Os três scripts SQL alterados não foram executados no banco da aplicação; a conversão anterior de schema e a remoção do ID 0 foram operações externas do operador.
