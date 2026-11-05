# Triagem CodeQL — PR #430

PR: https://github.com/vianahub-pt/VianaHub.Global.Gerit.Api/pull/430

## Evidência e alcance

- Head analisado pelo GitHub: `40c03c09c5614809787a1af9b3ed39813a8ad415`.
- Base develop do PR: `f8c854e063473992e95dcf51fc724d9d6685ecbc`.
- PR altera **514 ficheiros**. A comparação foi feita com Git local contra o SHA exato da base, sem depender da recuperação truncada do diff remoto.
- Análise CodeQL ID `2022387257`, SARIF relativo ao merge sintético `0be6b17492ac7b63b4e882635e120ade9dc892df`.
- A API/SARIF integral tem **293 resultados: 10 Medium de segurança, 1 High de segurança e 282 de qualidade**.
- O check visível do PR apresenta **79 anotações: 3 Medium, 21 outros warnings e 55 notes**. Os três Medium visíveis são #9, #10 e #11; não representam todos os resultados de segurança da análise integral.
- O conector GitHub não disponibiliza code-scanning alerts; os dados foram obtidos em modo leitura com `gh api`, incluindo SARIF e annotations. Nenhum alerta foi fechado/dismissed via API.

A/B classificam a origem; C/D classificam o diagnóstico: **A introduzido**, **B preexistente em develop**, **C false positive**, **D legítimo a corrigir**. Essas dimensões são separadas, porque um finding pode ser preexistente e legítimo.

As 11 linhas apontadas pelo CodeQL são idênticas entre base e head. Os ficheiros JwtSetup, UserValidationFilter e HangfireDashboardAuthorizationFilter não tinham alterações no PR. NoOpEmailSender mudou apenas `object[]` para `object?[]`; HangfireJobService mudou apenas o retorno de enqueue para nullable. Os fluxos de entrada até aos sinks já existiam. **Nenhum dos 11 findings de segurança foi introduzido pelo refactor.**

## Todos os Medium e superiores

As localizações abaixo pertencem ao snapshot remoto analisado; as correções estão ainda no working tree.

| Alerta | Severidade | Regra | Ficheiro/linha no PR | Classificação | Risco e tratamento |
|---|---|---|---|---|---|
| [#2](https://github.com/vianahub-pt/VianaHub.Global.Gerit.Api/security/code-scanning/2) | medium | `cs/log-forging` | `src/VianaHub.Global.Gerit.Api/Filters/UserValidationFilter.cs:41` | B + D | Rota userId inválida escrita no log; escapar o valor antes de o registar. |
| [#3](https://github.com/vianahub-pt/VianaHub.Global.Gerit.Api/security/code-scanning/3) | medium | `cs/log-forging` | `src/VianaHub.Global.Gerit.Api/Security/HangfireDashboardAuthorizationFilter.cs:108` | B + D | Username decodificado do Basic Auth escrito após negação; escapar o valor. |
| [#4](https://github.com/vianahub-pt/VianaHub.Global.Gerit.Api/security/code-scanning/4) | medium | `cs/log-forging` | `src/VianaHub.Global.Gerit.Api/Security/HangfireDashboardAuthorizationFilter.cs:115` | B + D | Username escrito após autenticação; escapar mesmo quando coincide com configuração. |
| [#5](https://github.com/vianahub-pt/VianaHub.Global.Gerit.Api/security/code-scanning/5) | medium | `cs/log-forging` | `src/VianaHub.Global.Gerit.Api/Configuration/JwtSetup.cs:207` | B + D | Path e mensagem de erro de autenticação; escapar ambos. |
| [#6](https://github.com/vianahub-pt/VianaHub.Global.Gerit.Api/security/code-scanning/6) | medium | `cs/log-forging` | `src/VianaHub.Global.Gerit.Api/Configuration/JwtSetup.cs:223` | B + D | Path e claim sub; escapar ambos sem alterar o principal. |
| [#7](https://github.com/vianahub-pt/VianaHub.Global.Gerit.Api/security/code-scanning/7) | medium | `cs/log-forging` | `src/VianaHub.Global.Gerit.Api/Configuration/JwtSetup.cs:235` | B + D | Path e erro de challenge; escapar ambos. |
| [#8](https://github.com/vianahub-pt/VianaHub.Global.Gerit.Api/security/code-scanning/8) | medium | `cs/log-forging` | `src/VianaHub.Global.Gerit.Api/Filters/UserValidationFilter.cs:99` | B + D | Path, identidade e nome no diagnóstico de acesso cross-user; escapar apenas os argumentos de logging. |
| [#9](https://github.com/vianahub-pt/VianaHub.Global.Gerit.Api/security/code-scanning/9) | medium | `cs/log-forging` | `src/VianaHub.Global.Gerit.Infra.Job/Services/HangfireJobService.cs:41` | B + D | TimeZoneId/JobName e exceção refletindo o input; codificar argumentos e detalhes de exceção. |
| [#10](https://github.com/vianahub-pt/VianaHub.Global.Gerit.Api/security/code-scanning/10) | medium | `cs/log-forging` | `src/VianaHub.Global.Gerit.Infra.Integration/Messaging/NoOpEmailSender.cs:22` | B + D | Destinatário de email e assunto enviados ao log; codificar ambos. |
| [#11](https://github.com/vianahub-pt/VianaHub.Global.Gerit.Api/security/code-scanning/11) | medium | `cs/log-forging` | `src/VianaHub.Global.Gerit.Infra.Integration/Messaging/NoOpEmailSender.cs:23` | B + D | Corpo localizado incorpora Name externo; codificar o resultado antes de logging. |
| [#12](https://github.com/vianahub-pt/VianaHub.Global.Gerit.Api/security/code-scanning/12) | high | `cs/user-controlled-bypass` | `src/VianaHub.Global.Gerit.Api/Security/HangfireDashboardAuthorizationFilter.cs:81` | B + C | O ramo apenas emite WWW-Authenticate e retorna false. Não concede acesso nem evita a comparação de credenciais. |

`cs/log-forging` corresponde a CWE-117. Os dez Medium são sinks reais de logging sem neutralização de separadores de linha. Templates estruturados por si só não removem CR/LF dos argumentos. A explorabilidade de alguns ramos depende da validação/autenticação anterior; não se assume que isso torne todos os dados seguros para logging.

O High #12 (`cs/user-controlled-bypass`) é um **false positive quanto a bypass**: os três destinos relacionados no SARIF são chamadas de `ChallengeBasicAuth`, que escrevem apenas um header constante. Header ausente, esquema errado, Base64 malformado ou credenciais inválidas devolvem false. O true de produção só é alcançado depois da comparação de username/password. Há um bypass local intencional em Development com debugger, controlado pelo servidor e não pelo header. Não foi adicionada suppression nem feita alteração cosmética do ramo para esconder o alerta.

Na revisão foi identificado um problema independente e legítimo, **B+D**, não reportado como alerta próprio no SARIF: valor default `changeme` e ausência de guarda para credenciais vazias no dashboard. O default de password foi removido e o filtro falha fechado quando username/password não estão provisionados. Testes cobrem configuração null, vazia e whitespace.

## Correções aplicadas

- Novo `Domain/Helpers/LogValueSanitizer.cs`: neutraliza CR, LF, NEL, U+2028 e U+2029 em valores de log, mantendo null e texto normal. Não altera entidades, requests, claims, passwords ou nomes operacionais.
- Os dez sinks apontados usam valores neutralizados e templates constantes com placeholders.
- Exceções podem incorporar input (CSV, parsing, timezone, SQL errors). Os logs alterados mantêm tipo, mensagem, inner exceptions e stack trace num campo estruturado `ExceptionDetails` neutralizado; não registam simultaneamente o objeto original não sanitizado.
- Foram revistos também sinks equivalentes preexistentes em AuthorizationFilter, execução de jobs, motivo de revogação/algoritmo JWT, erros de CSV, validação de requests e middleware global. As correções abrangem estes fluxos em vez de esconderem apenas as três anotações visíveis.
- As comparações de autorização e os argumentos enviados ao executor Hangfire mantêm os valores originais; os testes verificam essa separação.
- Nenhum pacote, configuração CodeQL, severidade, path exclusion, NoWarn, pragma ou suppression foi alterado.

## Paths, processos e URLs

- `DatabaseExtensions`: leitura de `docs/sql/Create-Tables.sql` a partir do content root do servidor e componentes constantes. Não há nome de script vindo de request. **Nenhum SQL foi executado nesta tarefa.**
- Localização/Swagger: as culturas vindas de query/header são selecionadas por allowlist nos middlewares RequestLocalization/SwaggerLocalization/SwaggerTranslation. Não há fluxo de `../`, path absoluto ou filename externo até File.ReadAllText nesses caminhos.
- Os oito resultados integrais `cs/path-combine` são notes de comportamento da API, não alertas de path traversal. As combinações observadas usam constantes ou cultura já validada; não se substituiu Combine só para apagar a nota.
- `DomainExtensions.ToByteArrayFileImage` lê um path arbitrário, mas não tem chamadores no repositório e não é endpoint. Não foi encontrado fluxo externo alcançável; permanece um helper interno que exige revisão se passar a receber paths de requests.
- Não foram encontrados `Process.Start`, `ProcessStartInfo` ou shells com argumentos derivados de requests, nem HttpClient com URL externa controlada pelo utilizador. Jobs usam reflexão sobre tipos carregados, com whitelist/validação de domínio e contrato IJob; não executam um comando de sistema recebido por HTTP.
- Não foram encontrados redirects por parâmetro URL/returnUrl. As URLs do Swagger são constantes e o redirect HTTP→HTTPS é o middleware padrão.

## Resultados de qualidade que permanecem

Os **76 apontamentos não-security** das anotações originais não são warnings do compilador e não foram tratados como Medium. Incluem assignments/upcasts, condições redundantes, catch-all, LINQ, complexidade e notas de Path.Combine. A lista integral está no apêndice; não se afirma que tenham sido eliminados nem se alterou a severidade.

A análise completa original tem outros resultados fora das anotações selecionadas pelo GitHub. Esta correção cobre todos os seus Medium/High, mas não pretende zerar os 282 resultados de qualidade da solution.

## Validação e limites

- Regressão adversarial: CR/LF e Unicode, destinatário/corpo de email, timezone/jobname/exceção, preservação dos argumentos de execução, rota inválida, callbacks JWT, negação Basic Auth e credenciais vazias.
- Os testes não fazem chamadas externas, SQL ou deploy.
- Formatter/verify aplicados apenas aos **31 C# alterados**.
- `dotnet restore VianaHub.Global.Gerit.sln`: exit 0.
- Build Release com `--no-restore --no-incremental`: **0 errors, 0 warnings**, exit 0. Foi forçada a recompilação da solution inteira para evitar um resultado parcial devido ao cache.
- `dotnet test VianaHub.Global.Gerit.sln --configuration Release --no-build`: **216 passed, 0 failed, 0 skipped**, exit 0 (194 existentes + 22 regressões adversariais).
- `dotnet format` e `--verify-no-changes` nos 31 ficheiros: ambos exit 0.
- `git diff --check`: exit 0.
- Suppressions adicionadas: **0**. Configuração CodeQL/CI alterada: **não**.
- Não existe novo Medium/High conhecido na revisão local destes fluxos. Os dez Medium identificados foram tratados; a confirmação automática remota permanece pendente.

Os logs de exceções alterados usam agora a propriedade estruturada `ExceptionDetails`, em vez de fornecerem simultaneamente o objeto original ao sink. O conteúdo técnico continua disponível com separadores escapados; pesquisas/dashboards que dependam especificamente do campo de exceção nativo devem considerar esse novo campo.

**GO para novo push das correções após revisão do utilizador.** Não equivale a GO para merge: antes disso, reexecutar CodeQL e confirmar os findings sobre o novo SHA. O High #12 permanece classificado C, sem dismissal automático, e os resultados de qualidade originais continuam visíveis para acompanhamento.

Evidências locais em `%TEMP%`: `gerit-codeql-pr430.sarif`, `gerit-codeql-pr430-alerts.json`, `gerit-codeql-pr430-annotations.json`, `gerit-codeql-security-triage.json`, `gerit-codeql-final-{restore,build,tests}.log`, `gerit-codeql-format-final.log`, `gerit-codeql-format-verify.log` e `gerit-codeql-final-diff-check.log`. Nenhum segredo foi copiado para este relatório.
- CodeQL CLI não está disponível localmente. Foi analisado o SARIF real e foram executados testes dos sinks; o GitHub CodeQL **ainda não foi reexecutado sobre estas alterações não publicadas**. Os alertas remotos continuam no snapshot antigo. Não se afirma que o GitHub já os marcou como resolvidos.

Uma alteração local prévia em `appsettings.Development.json` foi preservada e não pertence à correção. Não foram feitos staging, commit, push, merge ou deploy.

## Apêndice — 79 anotações originais

| Regra | Nível da anotação | Localização no snapshot | Tratamento |
|---|---|---|---|
| `cs/useless-assignment-to-local` | warning | `src/VianaHub.Global.Gerit.Domain/Validators/Job/JobDefinitionCreateValidator.cs:102` | Qualidade, fora da correção de segurança prioritária |
| `cs/useless-assignment-to-local` | warning | `src/VianaHub.Global.Gerit.Domain/Validators/Job/JobDefinitionUpdateValidator.cs:88` | Qualidade, fora da correção de segurança prioritária |
| `cs/constant-condition` | warning | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Billing/PlanDataRepository.cs:55` | Qualidade, fora da correção de segurança prioritária |
| `cs/constant-condition` | warning | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Business/AcquisitionSourceTypeDataRepository.cs:54` | Qualidade, fora da correção de segurança prioritária |
| `cs/constant-condition` | warning | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Business/AddressTypeDataRepository.cs:52` | Qualidade, fora da correção de segurança prioritária |
| `cs/constant-condition` | warning | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Business/EmployeeDataRepository.cs:49` | Qualidade, fora da correção de segurança prioritária |
| `cs/log-forging` | warning | `src/VianaHub.Global.Gerit.Infra.Integration/Messaging/NoOpEmailSender.cs:22` | Medium de segurança: corrigido localmente; B+D |
| `cs/log-forging` | warning | `src/VianaHub.Global.Gerit.Infra.Integration/Messaging/NoOpEmailSender.cs:23` | Medium de segurança: corrigido localmente; B+D |
| `cs/useless-assignment-to-local` | warning | `src/VianaHub.Global.Gerit.Infra.Job/Services/HangfireJobService.cs:32` | Qualidade, fora da correção de segurança prioritária |
| `cs/useless-assignment-to-local` | warning | `src/VianaHub.Global.Gerit.Infra.Job/Services/HangfireJobService.cs:37` | Qualidade, fora da correção de segurança prioritária |
| `cs/log-forging` | warning | `src/VianaHub.Global.Gerit.Infra.Job/Services/HangfireJobService.cs:41` | Medium de segurança: corrigido localmente; B+D |
| `cs/useless-assignment-to-local` | warning | `src/VianaHub.Global.Gerit.Infra.Job/Services/HangfireJobService.cs:42` | Qualidade, fora da correção de segurança prioritária |
| `cs/useless-upcast` | warning | `tests/VianaHub.Global.Gerit.Tests/Application/Services/Business/ClientAppServiceTests.cs:33` | Qualidade, fora da correção de segurança prioritária |
| `cs/useless-upcast` | warning | `tests/VianaHub.Global.Gerit.Tests/Application/Services/Business/PartyTypeAppServiceTests.cs:38` | Qualidade, fora da correção de segurança prioritária |
| `cs/useless-assignment-to-local` | warning | `tests/VianaHub.Global.Gerit.Tests/Application/Services/Business/PartyTypeAppServiceTests.cs:52` | Qualidade, fora da correção de segurança prioritária |
| `cs/useless-upcast` | warning | `tests/VianaHub.Global.Gerit.Tests/Application/Services/Business/PartyTypeAppServiceTests.cs:149` | Qualidade, fora da correção de segurança prioritária |
| `cs/useless-upcast` | warning | `tests/VianaHub.Global.Gerit.Tests/Application/Services/Business/StatusDomainAppServiceTests.cs:38` | Qualidade, fora da correção de segurança prioritária |
| `cs/useless-assignment-to-local` | warning | `tests/VianaHub.Global.Gerit.Tests/Application/Services/Business/StatusDomainAppServiceTests.cs:52` | Qualidade, fora da correção de segurança prioritária |
| `cs/useless-upcast` | warning | `tests/VianaHub.Global.Gerit.Tests/Application/Services/Business/StatusDomainAppServiceTests.cs:134` | Qualidade, fora da correção de segurança prioritária |
| `cs/dereferenced-value-may-be-null` | warning | `src/VianaHub.Global.Gerit.Application/Services/Billing/PlanAppService.cs:384` | Qualidade, fora da correção de segurança prioritária |
| `cs/useless-assignment-to-local` | warning | `src/VianaHub.Global.Gerit.Application/Services/Business/AddressTypeAppService.cs:89` | Qualidade, fora da correção de segurança prioritária |
| `cs/dereferenced-value-may-be-null` | warning | `src/VianaHub.Global.Gerit.Application/Services/Business/AddressTypeAppService.cs:343` | Qualidade, fora da correção de segurança prioritária |
| `cs/constant-condition` | warning | `src/VianaHub.Global.Gerit.Application/Services/Business/DocumentTypeAppService.cs:108` | Qualidade, fora da correção de segurança prioritária |
| `cs/constant-condition` | warning | `src/VianaHub.Global.Gerit.Application/Services/Business/StatusDefinitionAppService.cs:112` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Tools/DatabaseExtensions.cs:407` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Tools/DatabaseExtensions.cs:227` | Qualidade, fora da correção de segurança prioritária |
| `cs/linq/missed-select` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Tools/DatabaseExtensions.cs:193` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Tools/DatabaseExtensions.cs:156` | Qualidade, fora da correção de segurança prioritária |
| `cs/path-combine` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Tools/DatabaseExtensions.cs:65` | Qualidade, fora da correção de segurança prioritária |
| `cs/complex-condition` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Identity/UserPreferencesDataRepository.cs:57` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Identity/RoleDataRepository.cs:126` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Identity/JwtKeyDataRepository.cs:123` | Qualidade, fora da correção de segurança prioritária |
| `cs/complex-condition` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Business/VisitContactPersonDataRepository.cs:48` | Qualidade, fora da correção de segurança prioritária |
| `cs/complex-condition` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Business/VisitAddressDataRepository.cs:55` | Qualidade, fora da correção de segurança prioritária |
| `cs/complex-condition` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Business/VehicleDataRepository.cs:62` | Qualidade, fora da correção de segurança prioritária |
| `cs/complex-condition` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Business/VehicleDataRepository.cs:53` | Qualidade, fora da correção de segurança prioritária |
| `cs/missed-ternary-operator` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Business/VehicleDataRepository.cs:51` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Application/Services/Identity/ResourceAppService.cs:225` | Qualidade, fora da correção de segurança prioritária |
| `cs/linq/missed-where` | notice | `src/VianaHub.Global.Gerit.Application/Services/Identity/AuthAppService.cs:380` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Infra.Job/Services/HangfireJobExecutor.cs:43` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Infra.Job/Services/HangfireJobExecutor.cs:57` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Infra.Job/Services/HangfireJobExecutor.cs:74` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Application/Services/Business/VehicleAppService.cs:240` | Qualidade, fora da correção de segurança prioritária |
| `cs/linq/missed-select` | notice | `src/VianaHub.Global.Gerit.Application/Services/Business/StatusDefinitionAppService.cs:114` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Infra.Job/Services/HangfireJobService.cs:39` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Application/Services/Business/EquipmentTypeAppService.cs:232` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Application/Services/Business/EquipmentAppService.cs:235` | Qualidade, fora da correção de segurança prioritária |
| `cs/linq/missed-select` | notice | `src/VianaHub.Global.Gerit.Application/Services/Business/DocumentTypeAppService.cs:110` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Application/Services/Billing/TenantAppService.cs:219` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Domain/Tools/Cryptography/CryptoRSA.cs:168` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Domain/Tools/Pagination/CreateSort.cs:134` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Domain/Validators/Job/JobDefinitionCreateValidator.cs:91` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Domain/Services/Identity/JwtKeyDomainService.cs:524` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Domain/Validators/Job/JobDefinitionCreateValidator.cs:105` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Domain/Validators/Job/JobDefinitionCreateValidator.cs:119` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Domain/Validators/Job/JobDefinitionCreateValidator.cs:142` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Domain/Validators/Job/JobDefinitionCreateValidator.cs:162` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Domain/Validators/Job/JobDefinitionCreateValidator.cs:177` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Domain/Validators/Job/JobDefinitionUpdateValidator.cs:77` | Qualidade, fora da correção de segurança prioritária |
| `cs/linq/missed-where` | notice | `src/VianaHub.Global.Gerit.Domain/Helpers/CatalogCodeGenerator.cs:21` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Domain/Validators/Job/JobDefinitionUpdateValidator.cs:91` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Domain/Validators/Job/JobDefinitionUpdateValidator.cs:105` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Domain/Validators/Job/JobDefinitionUpdateValidator.cs:127` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Domain/Validators/Job/JobDefinitionUpdateValidator.cs:145` | Qualidade, fora da correção de segurança prioritária |
| `cs/empty-catch-block` | notice | `src/VianaHub.Global.Gerit.Domain/Validators/Job/JobDefinitionUpdateValidator.cs:145` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Domain/Validators/Job/JobDefinitionUpdateValidator.cs:158` | Qualidade, fora da correção de segurança prioritária |
| `cs/empty-catch-block` | notice | `src/VianaHub.Global.Gerit.Domain/Validators/Job/JobDefinitionUpdateValidator.cs:158` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Application/Services/Identity/UserRoleAppService.cs:210` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Application/Services/Identity/UserAppService.cs:256` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Application/Services/Identity/RolePermissionAppService.cs:228` | Qualidade, fora da correção de segurança prioritária |
| `cs/complex-condition` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Business/ClientAddressRepository.cs:58` | Qualidade, fora da correção de segurança prioritária |
| `cs/complex-condition` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Business/ClientFiscalDataDataRepository.cs:53` | Qualidade, fora da correção de segurança prioritária |
| `cs/complex-condition` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Business/ClientRepository.cs:70` | Qualidade, fora da correção de segurança prioritária |
| `cs/complex-condition` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Business/ClientRepository.cs:80` | Qualidade, fora da correção de segurança prioritária |
| `cs/complex-condition` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Business/EmployeeAddressDataRepository.cs:52` | Qualidade, fora da correção de segurança prioritária |
| `cs/complex-condition` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Business/EmployeeContactPersonDataRepository.cs:45` | Qualidade, fora da correção de segurança prioritária |
| `cs/catch-of-all-exceptions` | notice | `src/VianaHub.Global.Gerit.Application/Services/Identity/RoleAppService.cs:227` | Qualidade, fora da correção de segurança prioritária |
| `cs/complex-condition` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Business/EmployeeFiscalDataDataRepository.cs:53` | Qualidade, fora da correção de segurança prioritária |
| `cs/complex-condition` | notice | `src/VianaHub.Global.Gerit.Infra.Data/Repository/Business/EquipmentDataRepository.cs:51` | Qualidade, fora da correção de segurança prioritária |
