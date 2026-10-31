# Ciclo de vida das chaves JWT

## Assinatura e validação

- `GetActiveKeyAsync` e `HasActiveKeyAsync` usam a mesma consulta SQL: tenant correto, ativa, não removida, não revogada e `ExpiresAt > UtcNow`. A seleção prefere a ativação mais recente.
- A emissão repete `IsValidForSigning()` e verifica tenant, lifetime configurado, `MaxTokenLifetimeMinutes` e `ExpiresAt`. Não encurta silenciosamente o token: sem cobertura integral, retorna notificação localizada e não emite JWT.
- No refresh, a assinatura precisa ser obtida antes de revogar o refresh token anterior. Todas as verificações de subscrição recebem `tenantId`, não `userId`.
- A validação consulta as chaves do tenant e seleciona o `kid` exato. As chaves RSA ficam em `HttpContext.Items`; nenhuma opção compartilhada é modificada por requisição.
- Chaves inativas podem validar durante o prazo original. Chaves removidas, definitivamente revogadas ou expiradas não validam.

## Rotação e histórico

`NextRotationAt = ActivatedAt + RotationPolicyDays - OverlapPeriodDays`.

Quando chega a rotação, a substituição desativa as chaves anteriores e insere a nova em uma transação serializável. Os dois saves respeitam o índice filtrado único de chave ativa por tenant. Falha no INSERT causa rollback; o `KeyId` torna a operação idempotente em retry de um COMMIT com resultado desconhecido.

A anterior deixa de assinar, mas seu `ExpiresAt` permanece intacto. Com política de 90 dias e overlap de 7 dias, uma rotação no dia 83 mantém validação da anterior até o dia 90. Tokens novos usam a substituta. O cleanup faz apenas soft delete de chaves inativas, expiradas e além da retenção de 90 dias.

A agenda deve executar com antecedência suficiente: o overlap precisa superar o intervalo entre execuções mais o maior lifetime permitido dos tokens. O perfil operacional recomendado abaixo é horário/UTC; os defaults são overlap de 7 dias e lifetime máximo de 60 minutos. A defesa na emissão continua impedindo tokens que ultrapassem o prazo da chave, inclusive se o agendador estiver indisponível.

## Provisionamento da agenda

Os jobs são administrados por `JobDefinitions`. Registrar uma classe no DI não cria agenda. Na auditoria local de 30/10/2026, a tabela estava vazia, explicando o log `0 active recurring jobs to sync`.

Não há nova seed, migration, feature flag ou cadastro automático de startup nesta correção. Cada ambiente deve provisionar sua definição pelo fluxo administrativo existente, com autorização apropriada:

```http
POST /v1/admin/job-definition/
Content-Type: application/json

{
  "jobCategory": "Security",
  "jobName": "JwtKeyRotation",
  "description": "JWT key lifecycle rotation",
  "jobPurpose": "Rotate before signing keys expire",
  "jobType": "VianaHub.Global.Gerit.Infra.Job.Jobs.Security.JwtKeyRotationJob",
  "jobMethod": "Execute",
  "cronExpression": "0 * * * *",
  "timeZoneId": "UTC",
  "executeOnlyOnce": false,
  "timeoutMinutes": 10,
  "priority": 1,
  "queue": "default",
  "maxRetries": 3,
  "isSystemJob": true
}
```

Após criar, usar `PATCH /v1/admin/job-definition/{id}/activate` para registrar imediatamente, ou a sincronização existente no próximo startup. Confirmar a agenda no Hangfire; não usar somente `LastRegisteredAt` como evidência. Não criar uma segunda agenda diretamente no Hangfire: a sincronização remove agendas sem definição correspondente.

O cron/timezone da definição é a única agenda: o job não tem mais gate de `DateTime.Now.Hour`. Cada tenant tem escopo DI e `IRequestTenantContext` próprios. Os interceptors aceitam esse contexto explícito sem HTTP; uma claim JWT autenticada continua tendo prioridade e nunca há bypass de RLS.

### Descoberta administrativa sob RLS

`Tenants` também é protegida por RLS. Os três jobs JWT usam `IJwtSystemJobTenantDiscovery`, sem promover `ITenantDataRepository.GetAllAsync()` a uma consulta global. A implementação exige ausência de HTTP e um `JobContext` correspondente exatamente a um desses jobs.

A descoberta abre uma conexão dedicada e habilita `IsSuperAdmin` apenas para a consulta fixa de `Id`, `IsActive` e `IsDeleted`. Em `finally`, limpa `IsSuperAdmin` e `TenantId` com cleanup não cancelável. Se esse cleanup falhar, descarta o pool da conexão e propaga a falha. O resultado só chega ao job depois da remoção do privilégio; as operações de chave continuam nos escopos individuais dos tenants.

O interceptor de conexão limpa as duas flags antes de configurar um tenant e antes de fechar ou descartar conexões, nos caminhos síncronos e assíncronos. Se não conseguir limpar no fechamento, a conexão é marcada para descarte do pool. O lookup administrativo já existente de tenant por email também tem `finally`, preservando a consulta de login e removendo seu contexto antes de fechar a conexão.

Os testes `SqlServerIntegration` usam SQL Server/RLS reais e um pool exclusivo com tamanho 1. Definir `GERIT_RLS_TEST_CONNECTIONSTRING` no processo de teste, sem versionar seu valor. Sem essa variável, os testes SQL são explicitamente ignorados; isso não constitui evidência de RLS. A execução local deve registrar os testes efetivamente executados, os SPIDs e o estado final das flags. Um cenário negativo usa leitura em `master`, sem criar objetos, para provocar erro 208 após o grant e provar o cleanup em erro.

## Revisão CodeQL do PR #437

O fixture SQL/RLS exige `Encrypt=true`, mesmo quando a connection string de entrada informa outro valor. Nenhuma configuração de produção foi alterada.

Na validação local de 31/10/2026, a tentativa com `Encrypt=true` e sem a opção de confiança explícita falhou exclusivamente no handshake TLS por cadeia emitida por autoridade não confiável. A execução SQL/RLS aprovada usa a opção `TrustServerCertificate=true` que já existia na entrada local do fixture. Essa opção não foi adicionada ao código, nem a qualquer configuração de produção; a criptografia é sempre exigida pelo fixture. Um ambiente com certificado confiável pode fornecer a conexão sem essa opção.

Cada uma das três threads inline existentes foi auditada:

| Alerta / comentário | Classificação | Decisão |
|---|---|---|
| #377 / 4432721132 — conexão SQL | `valid bug` | Exigir criptografia no fixture. |
| #379 / 4432721140 — catch síncrono | `false positive` para remoção do catch | Manter captura e descarte seguro do pool. |
| #380 / 4432721145 — catch assíncrono | `false positive` para remoção do catch | Manter captura e cleanup não cancelável. |

O diagnóstico de catch genérico é correto, mas a captura é intencional para segurança. Falhas de comando, estado da conexão ou provider, inclusive as que não derivam de `DbException`, precisam marcar a conexão para descarte antes do retorno ao pool. O cleanup continua não cancelável e mantém `ClearPool` nos caminhos síncrono e assíncrono. Restringir os tipos capturados perderia essa garantia.

A condição `tenantId.HasValue` posterior à guarda de rejeição era redundante (`valid improvement`); somente esse ramo inalcançável foi removido. O filtro da limpeza de chaves foi convertido para `Where` com avaliação lazy, mesma ordem e curto-circuito, mantendo as mutações dentro do loop (`valid improvement`).

O catch do job também é intencional: a recuperação da chave pode falhar em banco, criptografia ou providers. A captura mantém o comportamento existente de registrar a falha antes da tentativa de rotação. O `finally` externo restaura o contexto do tenant mesmo se a rotação posterior falhar; as conexões continuam protegidas pelo cleanup do interceptor. Limitar a captura mudaria o lifecycle sem necessidade. Esses três Notes não são suprimidos nem dispensados no GitHub.

## Recuperação de uma chave expirada

Invocar `IJwtKeyDomainService.EnsureKeyExistsAsync(tenantId, systemUserId, ct)` em escopo com tenant explícito. Se não houver signing key válida, o domínio gera novo par RSA, criptografa a chave privada pelo provider existente e usa a substituição atômica. Não editar `ExpiresAt`, reativar a expirada ou apagar o histórico.

## Evidência de regressão

Os testes cobrem repository, emissão, expiração, lifetime, overlap, reconciliação, jobs, contexto RLS e HTTP real com Kestrel: login real, `JwtBearerHandler`, `RequireAuthorization` e `CustomAuthorize`.

Antes da correção de seleção, o resolver vazio foi chamado e o caso positivo retornou 200 pelo fallback `IssuerSigningKeys`. Contudo, dois testes negativos aceitaram tenant cruzado e `kid` desconhecido. A resolução passou a ser determinística por requisição e não depende desse fallback compartilhado. O problema original continuava sendo a assinatura com chave expirada, não a lista vazia isoladamente.
