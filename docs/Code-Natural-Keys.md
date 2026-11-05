# Especificação dos conceitos de Code

Decisão arquitetural de 25/10/2026 sobre `feature/refactor-code-natural-keys`.
O trabalho anterior foi preservado. Os inventários descrevem o repositório;
nenhuma base ou integração externa foi consultada ou modificada.

## Identidades e classificação

Id é a chave técnica relacional interna (PK/FK). FunctionalCode é a identidade
funcional canónica, portátil e estável. Name é apresentação traduzível.
FunctionalCode não é tradução, display nem surrogate key.

| Classe | Finalidade | Regra |
| --- | --- | --- |
| A — FunctionalCode | Identidade funcional | Inglês ASCII UPPER_SNAKE_CASE; imutável |
| B — StandardCode | Padrão externo, incluindo HTTP contextual | Preservar sintaxe própria |
| C — DomainSpecificCode | Código de domínio, como postal e atividade económica | Preservar domínio |
| D — RedundantCode | Duplicação sem função comprovada no modelo vigente | Remover apenas após análise de consumidores |

O [inventário](Code-Field-Inventory.md) contém 30 colunas persistidas: oito A,
17 B e cinco C, mais DTOs, contextos e aliases de seeds. SubscriptionPlans.Currency
é também B apesar do nome sem sufixo Code. Não foi encontrado outro Code genérico.
CountryCode/IssuingCountryCode, CurrencyCode e LanguageCode conservam os padrões
externos. PostalCode e EconomicActivityCode conservam seu domínio. Validação de
comprimento não comprova pertença integral às listas ISO/BCP 47.

## Geração inicial aprovada

Para novos registos dos sete catálogos não técnicos, a única fonte é o Name de
exatamente uma tradução canónica en-US. O cliente envia traduções, nunca Code ou
EnglishCode. A aplicação seleciona en-US, normaliza sintaticamente, verifica
colisão e persiste Code e traduções. Não usa pt-PT, locale corrente ou outro
fallback. Ausência, duplicação ou nome inglês vazio/inválido produz HTTP 400 com
localization key. A aplicação não traduz nem verifica semanticamente se texto
livre é inglês; a representação canónica é responsabilidade de quem a fornece.

| Entidade | Fonte para novos registos | Exemplo |
| --- | --- | --- |
| AddressTypes | Translations[en-US].Name | Residential → RESIDENTIAL |
| AcquisitionSourceTypes | Translations[en-US].Name | Television → TELEVISION |
| DocumentTypes | Translations[en-US].Name | National ID → NATIONAL_ID |
| PartyTypes | Translations[en-US].Name | Individual → INDIVIDUAL |
| StatusDomains | Translations[en-US].Name | Visit → VISIT |
| StatusDefinitions | Translations[en-US].Name | Waiting for client → WAITING_FOR_CLIENT |
| SubscriptionPlans | Translations[en-US].Name | Standard → STANDARD |
| FileTypes | Extension técnica e aliases explícitos | .jpg/.jpeg → JPEG |

`src/VianaHub.Global.Gerit.Domain/Helpers/CatalogCodeGenerator.cs` centraliza a
seleção canónica e normalização. O normalizador faz Trim, normalização Unicode,
remoção de diacríticos, uppercase invariant e transformação de separadores em
underscore. Preserva ASCII alfanumérico, elimina underscores repetidos/periféricos,
limita a 50 caracteres e rejeita resultado vazio. Não usa GUID, timestamp,
aleatoriedade, tradução automática, IA ou dicionário de tradução improvisado.

FileTypes não usa en-US para gerar Code. Preserva aliases técnicos comprovados:
.pdf → PDF; .jpg/.jpeg → JPEG; .7z → SEVEN_ZIP; .bin → BINARY.
Os restantes formatos derivam da extensão técnica. Compatibilidade entre MIME e
extensão é validação distinta da identidade do formato.

## Contratos e breaking changes

Create de AddressTypes, AcquisitionSourceTypes, PartyTypes, StatusDomains e
SubscriptionPlans passa de Name/Description simples para Translations, usando os
DTOs de tradução existentes e exigindo en-US. DocumentTypes e StatusDefinitions
já recebem Translations e passam a exigir en-US. Os restantes campos funcionais
permanecem: por exemplo, preços, moeda, limites e domínio de status.

OpenAPI marca Translations como obrigatório nos sete Creates. A metadata dos
requests e das respostas 201/400/409 é verificada por testes. O Create de Plans
passa a documentar 409, já devolvido em colisões. Os sete validators são resolvidos
pelo registo de rotas, incluindo AddressTypes, PartyTypes e StatusDomains que
antes não estavam registados. Não foram refeitos os restantes endpoints.

Exemplo mínimo de traduções no Create:

```json
{"translations":[{"languageCode":"en-US","name":"Residential","description":"Residential address"},{"languageCode":"pt-PT","name":"Morada residencial","description":"Morada para habitação"}]}
```

BulkUpload implementado de AddressTypes e Plans usa CSV com delimitador `;`:
EnglishName é obrigatório e EnglishDescription opcional para en-US; Name e
Description mantêm a tradução pt-PT. Não há coluna Code. Exemplos em
`src/VianaHub.Global.Gerit.Api/Assets/examples/address-types-example.csv` e
`plans-example.csv`. Os restantes campos comerciais do CSV de Plans mantêm-se.
Bulk de AcquisitionSourceTypes continua sem implementação (501); um DTO não prova
existência de importação operacional. Não se criou exportador ou integração nova.

Update normal e endpoints de tradução não recebem nem recalculam Code. Alterar
qualquer tradução, inclusive en-US, preserva o Code persistido: Individual →
Natural person mantém INDIVIDUAL. A regra vale também para mudanças técnicas de
FileTypes. Migrações explícitas são distintas de Update de apresentação.

## Unicidade e códigos externos

Colisões previsíveis produzem HTTP 409 com localização. Índices/constraints são a
última defesa; não há sufixo para contornar conflito. Truncamento e remoção de
acentos podem causar colisão. StatusDefinitions mantém TenantId + StatusDomainId
+ Code e isolamento RLS/interceptors. Os outros catálogos são globais.

PartyTypes e AcquisitionSourceTypes têm unicidade EF não filtrada; o SQL oficial
usa índices filtrados IsDeleted=0. Consultas preventivas incluem removidos quando
necessário para respeitar o constraint mais restritivo. A divergência permanece
registada no inventário. Nulabilidade de Subscriptions.CurrencyCode e dos países
emissores é assunto separado, sem alteração automática nesta feature.

## Seeds e migrações

A [matriz de seeds](Code-Seed-Matrix.md) lista 102 identidades funcionais. Seeds
são definições de sistema com Code explícito: não calculam Code em SQL a partir
de traduções. Os dois Initial_BackOffice usam o mesmo conjunto de FunctionalCodes.
PartyTypes mantém IDs determinísticos 1/2, en-US Individual/Organization e Codes
INDIVIDUAL/ORGANIZATION. Só esses dois valores tiveram normalização de casing.
Os outros 100 códigos existentes são preservados.

A regra en-US é para novas criações. Um seed/registo existente pode ter Code
canónico diferente da normalização literal do nome: WAITING_CLIENT permanece
mesmo com Waiting for client; TV permanece mesmo com Television; PAYG_HOURLY
permanece mesmo com Pay as you go hourly; MAINTENANCE permanece mesmo com Under
maintenance. Não se recalculam essas identidades. CITIZEN_CARD não é substituído
por NATIONAL_ID; TAX_IDENTIFICATION não é renomeado por preferência estilística.

`docs/sql/Migrate-Normalize-Functional-Codes.sql` contém somente os mappings
Individual → INDIVIDUAL e Organization → ORGANIZATION. É transacional e idempotente,
com pré-validação de colisões incluindo apagados, comparação BIN2 e tamanho exato,
preservação de IDs/FKs/traduções e relatório de desconhecidos não alterados.
Não traduz códigos portugueses desconhecidos. Inclui opções SET necessárias aos
índices filtrados. A migração não foi executada: primeira/segunda execução,
colisões, collation e integridade devem ser homologadas manualmente em SQL Server.

`docs/sql/Migrate-Remove-Redundant-Code.sql` continua a migração anterior de
Actions/Resources/Roles, também não executada e distinta da normalização.

## Autorização e portabilidade

Actions, Resources e Roles continuam sem Code; Name canónico e imutável é a
identidade semântica atual. AuthAppService emite roles e permissions por Name;
JWT, AuthorizationFilter e CustomAuthorize preservam o contrato atual.
RolePermissions e UserRoles continuam por IDs. Uma evolução futura para
Resource.Code/Action.Code/Role.Code permitiria display independente, mas exigiria
migração coordenada de matching, seeds, tokens e papéis personalizados. Está fora
desta feature e não bloqueia a decisão en-US aqui aprovada.

FunctionalCode é o identificador portátil recomendado para referências a
catálogos; Id continua relacional e traduções são display. Imports de entidades
que hoje usam IDs não foram convertidos automaticamente. Integrações futuras
precisam respeitar escopo tenant/domínio e resolução de códigos removidos.
Não foi comprovado consumidor externo operacional em Infra.Integration; isso não
prova ausência de consumidores fora do checkout. Client e constraints ainda usam
PartyTypeId1/2, oportunidade de evolução separada.

## Estado de aprovação e validação

A fonte semântica dos sete catálogos está resolvida por decisão definitiva:
en-US.Name na criação, sem fallback e sem Code fornecido pelo cliente.
GO arquitetural condicionado aos quality gates técnicos e à revisão dos breaking
changes documentados. O antigo NO-GO por ausência de fonte inglesa foi superado.
Não implica autorização para commit, push, PR, deploy ou execução de SQL.

Build/testes não substituem homologação das migrações em SQL Server. Os resultados
atuais de restore, format, build, testes e diff check são apresentados no relatório
da execução; esta especificação não declara gates aprovados antes da sua execução.
