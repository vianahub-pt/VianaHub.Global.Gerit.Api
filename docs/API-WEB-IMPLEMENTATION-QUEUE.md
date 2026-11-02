# API → Web: fila após certificação de Teams100

Atualização de02/11/2026, mesma branch fix/team-api-readiness. Histórico preservado: **Teams91→99→100%**. [Contrato transversal Accept-Language e evidências](API-WEB-READINESS-MATRIX.md#team-accept-language-certification).

## Estado atual

- TierA: **Actions100% eTeams100%**. O blocker do título de validação foi corrigido via LocalizationService e chave já existente nos quatro dicionários.
- Locale5/5 agora exige Accept-Language comprovado de ponta a ponta, incluindo validação, conflito, autorização e gone/not found quando aplicável. Recursos sem Translations também devem localizar mensagens.
- Gerit.Web deve injetar Accept-Language centralmente em todas as chamadas e encaminhá-lo pelo BFF. Gerit.Api mantém header opcional no protocolo e fallbackpt-PT quando ausente;fr-FR não suportado também usa esse fallback. Nenhuma chamada válida passa a400 só pela ausência do header.
-70 regressões específicas passaram:33 de AppService/domínio/repository e37 HTTP. Suíte completa:1268 PASS,0 FAIL,6 SKIP,1274 descobertos. Não chamar os seis SKIP de PASS.
- Formatter dos arquivos alterados PASS. Global permanece em188 diagnósticos/137 arquivos preexistentes, com **zero novos**: dívida registrada não bloqueante conforme o critério autorizado. Não registrar global formatter como PASS.

## Próximo candidato

**NEXT1:EquipmentTypes —91%**, seguindo a prioridade definida no pedido. Não foi iniciado nesta execução. UserPreferences e as abas ClientContactPerson/ClientAddress permanecem candidatos anteriores, sem nova reavaliação ou implementação. Nenhuma fila adicional foi inventada para preencher posição.

Teams sai da fila de remediação e está pronto para implementação no Gerit.Web no escopo certificado. O próximo trabalho no Web deve reutilizar o contrato agora formalizado, preservando Name/Description editáveis em Teams,409 de duplicidade, Update204, idempotência e410 para removidos. Esta execução não implementou Web.

As conclusões NO/99% abaixo pertencem aos checkpoints anteriores e foram superadas pela certificação acima. Preservadas para auditoria; não representam blockers atuais. Não há blocker funcional restante de Teams neste escopo. Sem commit,push,merge ou deploy.

## Checkpoint99 — histórico preservado


Atualização de02/11/2026, branch fix/team-api-readiness. [Matriz e evidência de Teams](API-WEB-READINESS-MATRIX.md#team-readiness-followup).

**Teams:91→99%.** Create e Update duplicados agora409; próprio nome continua editável, Update mantém204, estados/delete preservam idempotência e410. Sessenta regressões específicas passaram, incluindo27 HTTP locais com JwtBearer/CustomAuthorize. A suíte completa tem1258 PASS,0 FAIL e os mesmos6 SKIP (1264 descobertos).

Não declarar100%: o título compartilhado de validação não é localizado para inglês/espanhol (Locale4/5), e o formatter global tem188 diagnósticos em137 arquivos não alterados. Código de outros recursos não foi corrigido.

| Posição atual | Recurso | Readiness | Decisão |
|---|---|---:|---|
| NEXT1 | Teams |99%| Continua primeiro; fechar locale compartilhado e gate global antes de homologar100% |
| NEXT2 | EquipmentTypes |91%| Candidato seguinte; não houve reavaliação/correção nesta branch |
| NEXT3 | UserPreferences |91%| Configurações do usuário, não catálogo de negócio |
| NEXT4 | ClientContactPerson |91%| Aba de cliente existente |
| NEXT5 | ClientAddress |92%| Aba de cliente existente |

Quick-fix de Teams após esta execução: o gap restante de contrato é o título do ValidationFilter compartilhado; prechecks409, regressões CRUD/HTTP e propagação de persistência já foram fechados. A correção do título precisa preservar o escopo dos outros recursos. Higiene global do formatter é bloqueio de gate separado. Não existem cinco novos recursos certificados100%; TierA continua Actions.

A análise inicial abaixo é mantida como snapshot, inclusive score91 e propostas anteriores de Teams; ela não substitui esta atualização. Nenhum commit, push, PR, merge ou deploy foi feito.

## Snapshot da auditoria e fila originais — preservado


Auditoria consolidada em 02/11/2026. Base: develop `77f4a9bfc53264c6f5abf2d3d711524c99e19215`. Somente documentação para revisão; nenhuma correção foi implementada.

## Decisão recomendada

**O próximo recurso a implementar no Gerit Web é Teams**, em escopo de CRUD principal, usando Actions como referência técnica.

Evidência: TeamDetailResponse expõe Id, TenantId, Name, Description, IsActive e CreatedAt; Create/Update têm payloads próprios recuperáveis; os endpoints oferecem list/detail/paged/create/update/activate/deactivate/delete/bulk; o resource Teams e as ações usadas existem nos dois seeds. O contexto de tenant já vem da autenticação, sem lookup de uma FK de status. TeamDataRepository filtra IsDeleted e os serviços propagam bool de persistência. Teams é referenciado por EmployeeTeam e VisitTeam, oferecendo desbloqueio concreto de dois fluxos. Não depende do blocker das traduções de PartyType nem da relação não exposta de StatusDefinitions.

Score: **91%**, não100%. Faltam regressões próprias de CRUD/HTTP e definição consistente de conflitos400/409. Isso não impede começar uma UI que respeite o contrato4xx atual, mas impede declarar o recurso homologado100% nesta auditoria. Não houve novo smoke HTTP/Web.

[Consulta da matriz e da pilha completa](API-WEB-READINESS-MATRIX.md#team).

## Leitura correta dos tiers e da fila

- TIER A100%: apenas **Actions**, já implementado e fornecido como referência validada. Não há cinco recursos novos certificados100% para listar honestamente.
- TIER B90–99%: candidatos de superfície pequena, com lacunas descritas; nem sempre a lacuna exige mudar comportamento da API: parte é teste/contrato documentado. Não atribuir100% pela simples quantidade de testes compartilhados.
- TIER C75–89%: corrigir contrato ou dependências antes do fluxo completo quando houver hard cap; um candidato pode ter score inferior por insuficiência de evidência, sem um bug impeditivo provado.
- TIER D<75%: não iniciar a administração completa no Web antes da remediação.
- NON-UI / PLATFORM: Auth, JwtKey e Job. Permanecem dependências/plataforma, não são três páginas CRUD de negócio.

Os NEXT abaixo têm contrato recuperável e nenhum hard cap confirmado **no escopo limitado declarado**. A fila é de início de implementação, não uma reclassificação para TierA. A lista estrita “novos recursos já certificados100%” está vazia. As abas de filho podem administrar pais preexistentes; não se deve afirmar que o aggregate do pai inteiro está pronto por causa delas.

## Top implementation queue

| Posição | Recurso / score | Escopo imediato | Por que nesta posição | Condições e limites |
|---|---|---|---|---|
| NEXT 1 | Teams —91% | CRUD principal de equipas | Contrato simples, só contexto de tenant; desbloqueia EmployeeTeam e VisitTeam; padrão próximo de Actions | Respeitar Name editável neste domínio; não copiar imutabilidade de Name de Actions. Fechar regressões e conflitos antes de100% |
| NEXT 2 | EquipmentTypes —91% | Catálogo de tipos de equipamento por tenant | Name/Description/estado expostos, baixo número de dependências; desbloqueia Equipments | Não é catálogo global. Create duplicado400; UNIQUE de Update coberto por middleware409; adicionar testes reais do contrato |
| NEXT 3 | UserPreferences —91% | Configurações do usuário autorizado | Todos os campos editáveis expostos; Create usa usuário/tenant correntes; GET /user/{userId} usa ID real da sessão | Não inventar /current ou nova página de negócio; confirmar defaults de horário e grants para a role alvo; não ampliar autorização |
| NEXT 4 | ClientContactPerson —91% | Aba de contatos de um Client existente | Detail/Update recuperáveis; clientId encaminhado ao repository; não necessita criar Client nem escolher status | Depende de shell/rota do pai no Web e de cliente existente. Não declarar CRUD completo de Clients pronto; testar ownership entre pais |
| NEXT 5 | ClientAddress —92% | Aba de endereços de um Client existente | Dados/IDs de edição disponíveis e parent scope explícito; AddressTypes tem leitura suficiente para lookup | Precisa shell do pai e lookup de tipos; não requer implementar editor multilíngue de AddressTypes para usar IDs existentes; testar primary/ownership |

Essas são cinco entregas de UI, não cinco novas páginas independentes. Se produto exigir apenas novos aggregates completos com página própria, NEXT4/NEXT5 não são substitutos: ficam reservados até corrigir os blockers dos próximos aggregates. Business value de todos permanece **needs product decision**; não há evidência de receita, adoção ou ROI para inventar High/Medium/Low.

### Critério de prioridade

A matriz principal está estritamente ordenada por readiness decrescente. A fila de entrega considera readiness + valor de negócio quando conhecido + menor complexidade + desbloqueio real de dependências. Como o valor de negócio é desconhecido, ele não recebe um número fictício. Preferência por contratos sem hard cap, pouca inferência e payload explícito; a precedência entre empates é uma decisão de planejamento fundamentada nas FKs/escopo acima, não uma fórmula de ROI.

## Dependências que mudam a fila

**StatusDefinitions deve ser remediado antes de CRUD completo de Clients, Employees, Equipments, Vehicles, Visits e Subscriptions.** Os requests exigem StatusDefinitionId e StatusDomainId, mas as leituras do catálogo não expõem StatusDomainId nem oferecem filtro público por domínio que permita descobrir o par. Nome traduzido não é FK. Não forçar o usuário a digitar números e não parear por nome/posição como workaround.

PartyTypes e AcquisitionSourceTypes já fornecem IDs em leituras de lookup; seus blockers de administração do aggregate não devem ser transferidos indiscriminadamente a toda leitura. O blocker do par de status é diferente: impede construir uma seleção consistente de dados requeridos por criação/edição do consumidor. O TenantId da sessão também não obriga a construir o painel Tenants antes de Teams.

## Quick-fix queue

YES abaixo significa “pode fechar os gaps listados sem redesign de schema”, condicionado a implementar e provar todos eles. Não é previsão de PASS nem estimativa de prazo. Não há estimativas de tempo.

| Resource | Current score | Missing item | Estimated code surface | Risk | Becomes ready after fix? |
|---|---:|---|---|---|---|
| Teams |91| Regressões create/update/conflito/lookup/HTTP; contrato de duplicado400/409 explícito e consistente | AppService de Team e testes de domínio/repository/HTTP; repository se necessário excluir próprio ID no check de Update | Low/Medium | YES, após todos os gaps e gates listados |
| EquipmentTypes |91| Mesmas regressões e consistência de conflito; confirmação de limites e tenant scope | AppService de EquipmentType, teste de CRUD e integração HTTP; query de unicidade se necessário | Low/Medium | YES, sem schema novo, após provas |
| UserPreferences |91| Regressões de defaults/horários, usuário corrente, conflitos e leitura por usuário | AppService/validators somente se regressões demonstrarem defeito; testes específicos e HTTP | Medium | YES no escopo de configurações autorizado, após provas |
| Roles |75| Retornar Description editável; regressões de payload e de leitura após update/lifecycle | Detail DTO, mapping, AppService/mapping tests e HTTP; zero mudança DB | Low/Medium | YES para CRUD principal após fechar testes; grants de RolePermission continuam bloqueados |
| Resources |70| Retornar Description e corrigir Resource versus Resources na autorização de Deactivate; regressões | DTO/mapping, uma declaração de autorização e testes de permission/CRUD | Medium | YES para CRUD principal após todos os gaps; não confundir com grants |
| Fiscal data de Client/Employee |87| Substituir marcadores crus por chaves reais, nos quatro locales; regressões fiscais e ownership | Validators, quatro dicionários e testes; sem alteração de schema | Low/Medium | YES para abas de dados fiscais de pais existentes, após validar contrato e testes |
| AddressTypes |79| Check de nome deve excluir próprio ID; bool de persistência; semântica de locale explícita e regressões | AppService/repository/domain e testes | Medium | NO para editor de todas as traduções: esse contrato público ainda não existe; YES apenas para escopo localizado documentado após provas |
| SubscriptionPlans / Plans |70| Alinhar resource Plans/SubscriptionPlans; leitura versus cultura/fallback e limites de administração de traduções/file-rules | Endpoint, DTO/mapping conforme escopo, AppService e testes | Medium/High | NO por corrigir somente a autorização; ainda há decisão de escopo multilíngue |

Esses recursos não devem ser migrados para Mapperly como condição de prontidão funcional. AutoMapper16.1.1 continua inalterado.

## API remediation backlog

| Resource | Blocker | Layer | Breaking change? | DB change? | Authorization change? | Tests required |
|---|---|---|---|---|---|---|
| PartyTypes | Detail sem Translations/IDs; ações de tradução ausentes; validação/prechecks insuficientes; canonical policy; bool; persistência do delete a comprovar | DTO/mapping, endpoint, AppService/domain/repositories | Leitura aditiva não; política de canonical deve ser explicitada e comunicada | Não necessária para corrigir contrato com schema atual | Sim, escolher política coerente com outros três catálogos | Serialização/IDs/cultura, validators400, duplicates409, ownership410, bool=false, SQL identity/FK/delete e HTTP completo |
| DocumentTypes | Mesmo conjunto de tradução; PUT raiz vazio | DTO/mapping, endpoint, AppService/domain | Leitura aditiva; definir editabilidade sem inventar campos em DTO vazio | Não esperada | Sim | Mesma matriz de tradução; regressão explícita do que PUT deve fazer |
| StatusDomains | Mesmo conjunto de tradução; PUT pt-PT | DTO/mapping, endpoint, AppService/domain | Leitura aditiva; canonical policy explícita | Não esperada | Sim | Traduções/locale/duplicates/ownership/SQL e HTTP |
| StatusDefinitions | Traduções e auth; StatusDomainId ausente das leituras; bloqueio de consumers | DTO/mapping, endpoint, AppService/domain | Adição de FK não quebra consumidores que ignoram novo campo | Não necessária: FK já existe | Sim para translation mutations | GET lista/detail contém relação; fixtures com dois domínios e nomes iguais, sem inferência; matriz completa de tradução |
| Clients / Employees / Equipments / Vehicles / Visits / Subscriptions | Lookup obrigatório de status sem relação recuperável | Contrato de lookup e requests; Client DTO opcional versus EF obrigatório | Adição de campo é compatível; restringir payloads exige política/versionamento quando necessário | Não necessária para expor FK existente | Não para a adição do lookup | Criar/editar usando somente IDs retornados; mismatch de domínio rejeitado com4xx; testes multi-tenant; estados/lifecycle |
| Tenants | Detail omite FKs e textos necessários para preparar Update | DTO/mapping | Adição compatível sob auth existente; revisar exposição de dados pessoais | Não | Não prevista | Mapeamento, serialização, GET→form→PUT sem perda, permissões e410 |
| EmployeeContactPerson / VisitContactPerson | Campos editáveis não retornados | DTO/mapping | Adição compatível | Não | Não prevista | Round-trip integral, sem apagar valores omitidos na UI; ownership e locale de erro |
| RolePermissions / UserRoles | DELETE recebe IDs compostos ausentes do read | DTO/mapping e contratos de leitura | Adição compatível; não substituir silenciosamente rota existente | Não | Preservar checagem; não derivar grants por nomes | Ler associação existente em sessão nova e deletar com IDs retornados; denied401/403; tenant scope |
| TenantAddress / VisitTeamEmployee / Resources / Plans | Resource singular/plural/nome incompatível com ambos os seeds | Endpoint versus autorização provisionada | Payload não muda; grants precisam decisão operacional compatível | Não estrutural | Sim, alinhar identificadores sem bypass | Claims reais pelos artefatos, role permitida sem grant403, grant correto2xx; não considerar só role |
| TenantContactPerson / Subscription / VisitAttachment / JwtKey | GetPrimary, SetAsPrimary, GetActivate, SetPrimary ou Revoke não provisionados | Endpoint, modelo de Actions e política de grants | Escolha de ações deve ser documentada | Só dados se escolhida nova Action; sem execução automática | Sim | Matriz endpoint→resource/action→seed→claim→filter; verificar todos os custom handlers |
| VisitTeamEquipment / VisitTeamVehicle / VisitTeamEmployee | Listas/paginação ignoram o pai da rota | AppService/repository | Restringe resposta ao escopo já indicado pela rota | Não | Preservar auth/RLS; não usar filtro só no browser | Dois pais no mesmo tenant e dois tenants; listas/totalItems/pages corretos; detail/mutations de outro pai410 |
| VisitTeamFunctions | Rota contextual mas modelo de catálogo por tenant sem FK do pai | API contract/route/UX | Pode exigir alias compatível ou mudança de versão | Não presumir FK nova | Preservar recurso/actions existentes | Semântica de catálogo versus pai, IDs/route, leitura e mutation coerentes |
| Catálogos e algumas associações | DomainService retorna true após persistência false | Domain | Corrige falso sucesso; não alterar idempotência vigente | Não | Não | repository=false para Create/Update/Activate/Deactivate/Delete conforme aplicabilidade e repetição de intenção |
| Fiscal data | Erros com marcadores underscore não traduzidos | Validators/locales | Mensagem muda, shape/status preservados | Não | Não | Quatro locales, ausência de keys cruas, respostas de validação e campos preservados |
| User / Subscription | Lookup por ID mapeia null sem notificação/guard explícito | AppService/HTTP | Uniformizar200/null versus404/410 exige revisar consumidores | Não | Não | Ausente/removido, status real via pipeline e contratos declarados; não inventar comportamento atual410 |

Não há correção incluída nesta auditoria. O middleware já reduz várias falhas SQL para4xx; não aplicar cap SQL500 sem um caminho provado. A ausência de integração real mantém hipóteses de persistence/orphan delete como risco, não defeito confirmado.

## Revisões de segurança e qualidade da evidência

- Não foram consultados dados reais nem executado SQL operacional. SQL schema/seed foi apenas lido.
- Não houve mudança de RLS, claims, validações, código de runtime, migrations, seed ou startup.
- Não foram expostos valores de credenciais, JWT, cookies ou conexão nos documentos. Nomes de propriedades e chaves de localização são contrato, não valores secretos.
- Actions continua referência100 do escopo validado; a auditoria não generaliza essa evidência a todos os bulk/sub-resources.
- A regra futura de feature E2E deve classificar auth e tráfego do próprio recurso; prefetch de módulos não implementados não pode falhar um recurso independente. Application-wide smoke é verificação separada.
- Dados de testes de PartyTypes usam TINYINT IDENTITY; nenhuma criação ocorreu nesta auditoria, zero IDs consumidos. Em futuras jornadas, minimizar registros e não presumir recuperação de identity por soft delete.

## Entrega

Documentos: API-WEB-READINESS-MATRIX.md e este arquivo. Baseline completo:1198 PASS,0 FAIL,6 SKIP,1204 descobertos; build/restore PASS. Nenhum SQL/HTTP live novo. git diff --check verificado após geração. Working tree contém somente esses documentos novos, sem commit/push/PR. Conteúdo deve ser revisado por uma pessoa antes de versionar.
