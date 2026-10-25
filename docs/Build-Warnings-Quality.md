# Qualidade do build e política de warnings

## Política permanente

Warning budget: **0–9 permitido temporariamente; >=10 é NO-GO**. Target: **zero first-party warnings**.

Depois da limpeza não existe exceção temporária: a implementação atual é mais estrita e exige **zero warnings de qualquer origem**. Um warning novo já exige correção. Não há supressões ou lista de exceções.

`Directory.Build.props` define `TreatWarningsAsErrors=true` para os projetos da solution. A configuração de nullable e os analyzers continuam ativos. O CI usa `dotnet restore ... -warnaserror` e `dotnet build ... --no-incremental -warnaserror`: o próprio MSBuild promove diagnósticos a erros, incluindo warnings emitidos por targets. Não há parsing de texto, contagem dependente de idioma ou ferramenta adicional. O build completo evita resultados aparentemente limpos devido ao cache incremental. Esta política rejeita automaticamente >=10 e também 1–9 warnings.

## Baseline medido em 25/10/2026

Branch: `feature/refactor-code-natural-keys`. O working tree já continha a feature não commitada; esse trabalho foi preservado.

O comando Release incremental inicial mostrou apenas **1 warning NU1603**. Isso não representava a solution inteira. A medição confiável acrescentou `--no-incremental` ao mesmo build Release, depois do restore:

- **1.344 ocorrências de warning**, zero erros no resumo do MSBuild;
- **1.343 diagnósticos únicos**, deduplicados por linha completa (código, localização, mensagem e projeto);
- **2.688 linhas de diagnóstico** no log, porque o resumo volta a imprimir os warnings;
- um CS8602 em `DomainExtensions.cs`, na mesma localização, foi emitido duas vezes para componentes distintos da expressão de reflexão; isso explica a diferença entre ocorrências e diagnósticos únicos;
- 1.342 diagnósticos únicos first-party e 1 de dependência; zero diagnósticos de código externo/gerado.

A tabela completa por ficheiro/localização/origem/causa foi guardada em `%TEMP%/gerit-warning-baseline-diagnostics.csv`. O log integral está em `%TEMP%/gerit-warnings-baseline-full.log`.

### Distribuição por código (diagnósticos únicos)

| Código | Quantidade |
|---|---:|
| CS0105 | 1 |
| CS0108 | 16 |
| CS0168 | 18 |
| CS0618 | 24 |
| CS8387 | 1 |
| CS8600 | 16 |
| CS8601 | 4 |
| CS8602 | 129 |
| CS8603 | 207 |
| CS8604 | 469 |
| CS8605 | 5 |
| CS8613 | 3 |
| CS8618 | 112 |
| CS8619 | 6 |
| CS8620 | 10 |
| CS8622 | 2 |
| CS8625 | 307 |
| CS8629 | 2 |
| CS8765 | 1 |
| CS8766 | 8 |
| CS8767 | 1 |
| NU1603 | 1 |
| **Total** | **1343** |

### Distribuição por projeto (diagnósticos únicos)

| Projeto | Quantidade |
|---|---:|
| Api | 31 |
| Application | 845 |
| Domain | 210 |
| Infra.Data | 205 |
| Infra.Job | 16 |
| Tests | 36 |
| **Total** | **1343** |

Tests inclui 35 diagnósticos C# e o NU1603. Domain tem 210 diagnósticos únicos e 211 ocorrências, devido à expressão de reflexão referida acima.

## Causa específica de 452 → 912

Os logs históricos não eram recompilações completas da mesma seleção de projetos:

- `gerit-code-audit-build.log`: Domain 211 ocorrências + Infra.Data 205 + Tests 35 + NU1603 1 = **452** (451 únicos; 904 linhas);
- `gerit-enus-build-final.log`: Application 845 + API 31 + Tests 35 + NU1603 1 = **912** (912 únicos; 1.824 linhas).

O delta **+460** é exatamente `(845 + 31) - (211 + 205)`. O build incremental recompilou Application/API e reutilizou Domain/Infra.Data. Os logs não sustentam atribuir 460 novos defeitos à feature ou a uma dependência. A recompilação completa revelou o conjunto total, incluindo Infra.Job.

## Dependências

A árvore foi capturada com `dotnet list VianaHub.Global.Gerit.sln package --include-transitive`, e a proveniência confirmada com `dotnet nuget why`.

| PackageReference alterado | Antes | Depois | Motivo |
|---|---|---|---|
| Tests / Moq | 4.20.3 solicitado, 4.20.69 resolvido | 4.20.69 explícito | Elimina NU1603; mantém a versão já executada pelos testes |
| Infra.Data / Microsoft.IdentityModel.Protocols.OpenIdConnect | 6.35.0 transitivo | 8.14.0 explícito | Alinha a família IdentityModel já exigida em 8.14.0 pelo AutoMapper |

AutoMapper 16.1.1 introduzia JsonWebTokens/Tokens 8.14.0; EF SqlServer 8.0.11 → SqlClient 5.1.6 introduzia OIDC/JWT 6.35.0; JwtBearer 8.0.11 introduzia OIDC/JWT 7.1.2. A referência de alinhamento propaga OIDC, Protocols, JWT, JsonWebTokens, Tokens, Logging e Abstractions 8.14.0 aos projetos consumidores. .NET 8 foi mantido. Não houve upgrade arbitrário de framework, downgrade, referência repetida por projeto nem alteração de pacote externo.

A mistura IdentityModel era um risco real de leitura/validação, mas não a origem das centenas de avisos nullable. O teste de emissão RSA valida agora o mesmo token com JsonWebTokenHandler e JwtSecurityTokenHandler, mantendo assinatura RS256, issuer, audience e lifetime ativos.

## Correções first-party

- Operadores de igualdade e Equals de Entity agora declaram os valores nulos que já tratavam; os testes `entity == null` deixam de violar o contrato base.
- Construtores/Update aceitam parâmetros anuláveis apenas quando já transportavam estado anulável para propriedades da entidade, mantendo a validação de domínio e INotify.
- Retornos de ausência em repositórios, domínio, Application, CSV, segredos e jobs foram alinhados com as interfaces. Não se inventaram objetos ou strings para substituir resultados ausentes.
- Coleções de entidades/paginação são inicializadas. Navegações EF podem estar por carregar e estão representadas como anuláveis, sem lançar novas exceções ao acessá-las.
- Includes com navegações opcionais usam caminhos compostos por `nameof`, sem null-forgiving. Os predicados de pesquisa verificam campos/relações opcionais antes de os utilizar.
- Perfis AutoMapper tratam navegações por carregar e mantêm IDs/FKs. Testes cobrem o mapeamento e a obrigatoriedade das relações no modelo EF.
- FileValidation declara `NotNullWhen(true)` no input que já rejeitava ficheiros ausentes; os testes de uploads nulos continuam ativos.
- Reflection e resultados escalares verificam ausência real; um COUNT sem resultado permanece uma falha técnica explícita. Nenhuma consulta SQL foi executada nesta tarefa.
- Check constraints usam `ToTable(table => table.HasCheckConstraint(...))`; Hangfire usa a sobrecarga com queue e RecurringJobOptions, preservando timezone/queue.
- Campos de localização duplicados usam a inicialização da base; o filtro Job usa IsActive herdado; imports e capturas não utilizados foram removidos; o genérico local de ordenação foi distinguido.
- Contratos de filtros HTTP usam `ValueTask<object?>`, e localização retorna string porque tem fallback para a chave.

Não foram adicionados NoWarn, pragmas, exclusões, reduções de WarningLevel, desativação de nullable/analyzers ou operadores `!` para esconder diagnósticos. A feature FunctionalCode/en-US, os testes existentes e os artefactos SQL manuais foram preservados.

## Medições de builds completos bem-sucedidos

| Etapa | Warnings | Erros |
|---|---:|---:|
| Baseline completo | 1344 | 0 |
| Dependências | 1343 | 0 |
| Contratos base e transporte anulável | 815 | 0 |
| Retornos, coleções e interfaces | 269 | 0 |
| Pesquisas e APIs de mapping | 94 | 0 |
| Navegações opcionais e contratos finais | 69 | 0 |
| Limpeza completa | 0 | 0 |
| Política central + CI strict MSBuild | 0 | 0 |

## Gates de entrega

| Gate final | Resultado |
|---|---|
| `dotnet restore VianaHub.Global.Gerit.sln` | Aprovado, exit 0 |
| Build Release com `--no-restore --no-incremental -warnaserror` | **0 erros, 0 warnings**, exit 0 |
| Comando solicitado `dotnet build VianaHub.Global.Gerit.sln --configuration Release --no-restore` | **0 erros, 0 warnings**, exit 0 |
| `dotnet test VianaHub.Global.Gerit.sln --configuration Release --no-build` | **194 aprovados, 0 falhas, 0 ignorados**, exit 0 |
| `dotnet format` só nos C# desta limpeza | **397 ficheiros**, dois lotes, exit 0 |
| `dotnet format --verify-no-changes` nos mesmos ficheiros | Dois lotes aprovados, exit 0 |
| `git diff --check` | Aprovado, exit 0 |
| Prova negativa do gate | Um `#warning` temporário foi promovido a **error CS1030** e o build falhou com exit 1; ficheiro removido e o build completo final passou |
| Verificação de encoding | Nenhum dos C# alterados ganhou caracteres de substituição Unicode relativamente ao HEAD |

O gate negativo foi executado sem `-warnaserror`, demonstrando que `Directory.Build.props` protege também o build local normal. Não é uma supressão: a prova inseriu um diagnóstico artificial, confirmou a rejeição e removeu somente o ficheiro de teste criado pela própria tarefa.

Logs integrais em `%TEMP%`: `gerit-warnings-final-restore.log`, `gerit-warnings-final-full-build.log`, `gerit-warnings-final-build.log`, `gerit-warnings-final-tests.log`, `gerit-warnings-format-{1,2}.log`, `gerit-warnings-format-verify-{1,2}.log`, `gerit-warnings-gate-negative.log`, `gerit-warnings-final-diff-check.log` e `gerit-warnings-final-status.log`.

O formato foi limitado por hashes capturados antes das correções; não foi aplicado a ficheiros C# apenas por já pertencerem ao refactor funcional. A lista encontra-se em `%TEMP%/gerit-warning-cleanup-cs.txt`. Os 192 testes existentes foram preservados e acrescidos de dois testes de contrato; a validação dos dois leitores JWT foi acrescentada ao teste RSA existente.

## Estado Git e decisão

- Branch confirmada: `feature/refactor-code-natural-keys`.
- HEAD mantido: `838e924be5f1ce534ff3d7caf4211e895563eebd`.
- `git status`: **494 entradas modificadas + 23 não rastreadas = 517 entradas; zero staged**. Inclui todo o trabalho funcional anterior, que já estava não commitado.
- Zero ficheiros rastreados removidos. AGENTS.md e `.opencode` sem alterações.
- Os três novos artefactos desta limpeza são `Directory.Build.props`, este relatório e `NullablePersistenceContractTests.cs`; os outros ficheiros não rastreados já pertenciam à feature.
- **GO para commit quanto aos gates solicitados**: budget <10 atingido (zero), testes e verificações aprovados. O commit continua reservado ao utilizador.

Os testes são locais/in-memory e de contrato; esta limpeza não executou integração com SQL Server nem fez deploy.

Warnings restantes no build limpo: **nenhum**. Justificativa/plano individual: não aplicável. O budget **<10** foi atingido sem exceções.

Não foram feitos commit, push, PR, deploy ou execução de SQL.
