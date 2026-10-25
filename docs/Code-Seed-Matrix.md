# Matriz dos FunctionalCodes dos seeds

Inventário inicial da Fase 1 (coluna Code auditado), comparado com o estado atual dos seeds (coluna Canónico proposto, agora aplicado). Os dois seeds possuem as mesmas 102 identidades funcionais. Apenas PartyTypes mudou de casing para INDIVIDUAL/ORGANIZATION, preservando IDs 1/2 e traduções. Os outros 100 valores permanecem idênticos. Linhas de fonte referem-se ao seed vigente.

Para NOVOS registos dos sete catálogos não técnicos, en-US.Name é a única fonte de geração. Essa regra não recalcula seeds/dados estabelecidos: TV não vira TELEVISION, WAITING_CLIENT não vira WAITING_FOR_CLIENT e PAYG_HOURLY não vira PAY_AS_YOU_GO_HOURLY. Updates de qualquer tradução, inclusive en-US, preservam Code. FileTypes mantém derivação técnica. A decisão de fonte inglesa está resolvida; validação técnica depende dos quality gates e migrações continuam sujeitas a homologação SQL manual, sem execução nesta feature.

| Tabela / domínio | Code auditado | Canónico proposto | Mudança / motivo | Fonte |
| --- | --- | --- | --- | --- |
| PartyTypes | `Individual` | `INDIVIDUAL` | Sim; inglês UPPER_SNAKE_CASE | Initial_BackOffice.sql:52 |
| PartyTypes | `Organization` | `ORGANIZATION` | Sim; inglês UPPER_SNAKE_CASE | Initial_BackOffice.sql:53 |
| AcquisitionSourceTypes | `OTHER` | `OTHER` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:103 |
| AcquisitionSourceTypes | `INSTAGRAM` | `INSTAGRAM` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:104 |
| AcquisitionSourceTypes | `FACEBOOK` | `FACEBOOK` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:105 |
| AcquisitionSourceTypes | `LINKEDIN` | `LINKEDIN` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:106 |
| AcquisitionSourceTypes | `YOUTUBE` | `YOUTUBE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:107 |
| AcquisitionSourceTypes | `WHATSAPP` | `WHATSAPP` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:108 |
| AcquisitionSourceTypes | `TIKTOK` | `TIKTOK` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:109 |
| AcquisitionSourceTypes | `GOOGLE` | `GOOGLE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:110 |
| AcquisitionSourceTypes | `REFERRAL` | `REFERRAL` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:111 |
| AcquisitionSourceTypes | `TV` | `TV` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:112 |
| AcquisitionSourceTypes | `RADIO` | `RADIO` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:113 |
| AcquisitionSourceTypes | `NEWSPAPER` | `NEWSPAPER` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:114 |
| AcquisitionSourceTypes | `MAGAZINE` | `MAGAZINE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:115 |
| AcquisitionSourceTypes | `EVENTS` | `EVENTS` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:116 |
| AddressTypes | `RESIDENTIAL` | `RESIDENTIAL` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:170 |
| AddressTypes | `COMMERCIAL` | `COMMERCIAL` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:171 |
| AddressTypes | `INDUSTRIAL` | `INDUSTRIAL` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:172 |
| AddressTypes | `RURAL` | `RURAL` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:173 |
| AddressTypes | `PUBLIC_SERVICE` | `PUBLIC_SERVICE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:174 |
| AddressTypes | `EDUCATION` | `EDUCATION` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:175 |
| AddressTypes | `HEALTHCARE` | `HEALTHCARE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:176 |
| AddressTypes | `HOSPITALITY` | `HOSPITALITY` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:177 |
| AddressTypes | `LOGISTICS` | `LOGISTICS` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:178 |
| AddressTypes | `POSTAL_ALTERNATIVE` | `POSTAL_ALTERNATIVE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:179 |
| DocumentTypes | `CITIZEN_CARD` | `CITIZEN_CARD` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:233 |
| DocumentTypes | `PASSPORT` | `PASSPORT` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:234 |
| DocumentTypes | `DRIVING_LICENSE` | `DRIVING_LICENSE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:235 |
| DocumentTypes | `TAX_IDENTIFICATION` | `TAX_IDENTIFICATION` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:236 |
| DocumentTypes | `COMPANY_REGISTRATION` | `COMPANY_REGISTRATION` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:237 |
| DocumentTypes | `OTHER` | `OTHER` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:238 |
| FileTypes | `JPEG` | `JPEG` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:294 |
| FileTypes | `PNG` | `PNG` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:295 |
| FileTypes | `GIF` | `GIF` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:296 |
| FileTypes | `WEBP` | `WEBP` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:297 |
| FileTypes | `SVG` | `SVG` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:298 |
| FileTypes | `PDF` | `PDF` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:299 |
| FileTypes | `DOC` | `DOC` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:300 |
| FileTypes | `DOCX` | `DOCX` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:301 |
| FileTypes | `XLS` | `XLS` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:302 |
| FileTypes | `XLSX` | `XLSX` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:303 |
| FileTypes | `PPT` | `PPT` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:304 |
| FileTypes | `PPTX` | `PPTX` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:305 |
| FileTypes | `TXT` | `TXT` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:306 |
| FileTypes | `CSV` | `CSV` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:307 |
| FileTypes | `JSON` | `JSON` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:308 |
| FileTypes | `XML` | `XML` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:309 |
| FileTypes | `ZIP` | `ZIP` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:310 |
| FileTypes | `RAR` | `RAR` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:311 |
| FileTypes | `SEVEN_ZIP` | `SEVEN_ZIP` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:312 |
| FileTypes | `BINARY` | `BINARY` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:313 |
| StatusDomains | `CLIENT` | `CLIENT` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:368 |
| StatusDomains | `EMPLOYEE` | `EMPLOYEE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:369 |
| StatusDomains | `EQUIPMENT` | `EQUIPMENT` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:370 |
| StatusDomains | `VEHICLE` | `VEHICLE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:371 |
| StatusDomains | `VISIT` | `VISIT` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:372 |
| StatusDomains | `SUBSCRIPTION` | `SUBSCRIPTION` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:373 |
| SubscriptionPlans | `FREE` | `FREE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:438 |
| SubscriptionPlans | `BASIC` | `BASIC` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:439 |
| SubscriptionPlans | `STANDARD` | `STANDARD` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:440 |
| SubscriptionPlans | `PROFESSIONAL` | `PROFESSIONAL` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:441 |
| SubscriptionPlans | `ENTERPRISE` | `ENTERPRISE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:442 |
| SubscriptionPlans | `PAYG_HOURLY` | `PAYG_HOURLY` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:443 |
| SubscriptionPlans | `PAYG_DAILY` | `PAYG_DAILY` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:444 |
| StatusDefinitions / CLIENT | `PROSPECT` | `PROSPECT` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:831 |
| StatusDefinitions / CLIENT | `ACTIVE` | `ACTIVE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:832 |
| StatusDefinitions / CLIENT | `INACTIVE` | `INACTIVE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:833 |
| StatusDefinitions / CLIENT | `SUSPENDED` | `SUSPENDED` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:834 |
| StatusDefinitions / CLIENT | `ARCHIVED` | `ARCHIVED` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:835 |
| StatusDefinitions / EMPLOYEE | `ACTIVE` | `ACTIVE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:836 |
| StatusDefinitions / EMPLOYEE | `ON_LEAVE` | `ON_LEAVE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:837 |
| StatusDefinitions / EMPLOYEE | `INACTIVE` | `INACTIVE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:838 |
| StatusDefinitions / EMPLOYEE | `TERMINATED` | `TERMINATED` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:839 |
| StatusDefinitions / EQUIPMENT | `AVAILABLE` | `AVAILABLE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:840 |
| StatusDefinitions / EQUIPMENT | `IN_USE` | `IN_USE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:841 |
| StatusDefinitions / EQUIPMENT | `MAINTENANCE` | `MAINTENANCE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:842 |
| StatusDefinitions / EQUIPMENT | `RETIRED` | `RETIRED` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:843 |
| StatusDefinitions / VEHICLE | `AVAILABLE` | `AVAILABLE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:844 |
| StatusDefinitions / VEHICLE | `IN_USE` | `IN_USE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:845 |
| StatusDefinitions / VEHICLE | `MAINTENANCE` | `MAINTENANCE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:846 |
| StatusDefinitions / VEHICLE | `RETIRED` | `RETIRED` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:847 |
| StatusDefinitions / VISIT | `SCHEDULED` | `SCHEDULED` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:848 |
| StatusDefinitions / VISIT | `CONFIRMED` | `CONFIRMED` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:849 |
| StatusDefinitions / VISIT | `EN_ROUTE` | `EN_ROUTE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:850 |
| StatusDefinitions / VISIT | `IN_PROGRESS` | `IN_PROGRESS` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:851 |
| StatusDefinitions / VISIT | `PAUSED` | `PAUSED` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:852 |
| StatusDefinitions / VISIT | `WAITING_CLIENT` | `WAITING_CLIENT` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:853 |
| StatusDefinitions / VISIT | `WAITING_MATERIAL` | `WAITING_MATERIAL` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:854 |
| StatusDefinitions / VISIT | `RESCHEDULED` | `RESCHEDULED` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:855 |
| StatusDefinitions / VISIT | `COMPLETED` | `COMPLETED` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:856 |
| StatusDefinitions / VISIT | `COMPLETED_PENDING` | `COMPLETED_PENDING` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:857 |
| StatusDefinitions / VISIT | `CANCELED` | `CANCELED` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:858 |
| StatusDefinitions / VISIT | `NOT_PERFORMED` | `NOT_PERFORMED` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:859 |
| StatusDefinitions / VISIT | `VALIDATING` | `VALIDATING` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:860 |
| StatusDefinitions / VISIT | `INVOICED` | `INVOICED` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:861 |
| StatusDefinitions / VISIT | `ARCHIVED` | `ARCHIVED` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:862 |
| StatusDefinitions / SUBSCRIPTION | `TRIAL` | `TRIAL` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:863 |
| StatusDefinitions / SUBSCRIPTION | `ACTIVE` | `ACTIVE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:864 |
| StatusDefinitions / SUBSCRIPTION | `PAST_DUE` | `PAST_DUE` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:865 |
| StatusDefinitions / SUBSCRIPTION | `CANCELED` | `CANCELED` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:866 |
| StatusDefinitions / SUBSCRIPTION | `EXPIRED` | `EXPIRED` | Não; identidade inglesa/técnica estável | Initial_BackOffice.sql:867 |

Total: 102 entradas. Não substituir códigos por transliteração do nome pt-PT ou normalização cega do nome en-US. JPEG/jpg, SEVEN_ZIP/7z e BINARY/bin são aliases técnicos explícitos; CITIZEN_CARD identifica documento português, não qualquer NATIONAL_ID.

## Outros códigos comprovados nos seeds

Estes valores não são FunctionalCode e não são normalizados. Inventário das
tabelas variáveis de ambos os scripts, complementado pelos literais dos SELECTs.

| Campo / conceito | Valores distintos | Classificação / decisão |
| --- | --- | --- |
| CountryCode / IssuingCountryCode | PT | B; país, preservar formato externo |
| CurrencyCode / Currency | EUR | B; moeda, preservar formato externo |
| LanguageCode | pt-PT, pt-BR, en-US, es-ES | B; idioma, preservar casing e hífen |
| EconomicActivityCode | 43320, 47730, 56101, 62010, 63110, 70220, 73110; também NULL | C; atividade económica, preservar domínio |
| PostalCode | 1000-190, 1000-260, 1000-300, 1050-120, 1050-121, 1050-191, 1069-300, 1100-048, 1200-014, 1200-195, 1200-868, 1250-096, 1250-140, 1600-209, 1600-404, 1700-008, 1749-061, 1900-350, 1998-017, 1999-001, 2790-124, 2795-242, 2900-452, 3000-179, 3000-315, 3000-395, 3030-175, 3030-199, 3045-504, 3500-135, 3810-005, 3810-164, 3830-292, 4000-205, 4000-295, 4000-447, 4050-036, 4050-174, 4050-379, 4100-130, 4100-320, 4150-146, 4200-216, 4300-454, 4465-671, 4465-764, 4470-177, 4535-186, 4700-000, 4700-329, 4710-229, 7000-656, 7370-025, 8000-283, 8000-285, 8100-536, 8500-300 | C; código postal, preservar hífen |

PartyTypeCode, AddressTypeCode, AcquisitionSourceCode, DocumentTypeCode, DomainCode
e StatusCode das tabelas variáveis referenciam os FunctionalCodes listados acima;
não são novas identidades persistidas. Valores desconhecidos de bases existentes
não podem ser inventariados por leitura do repositório.
