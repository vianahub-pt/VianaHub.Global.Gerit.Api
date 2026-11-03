# Autenticação de smoke produtivo

Requer Node.js 22 ou superior, acesso HTTPS e identidade autorizada para o recurso.
O smoke mínimo exige leitura de Teams. Execute a partir da raiz da API.

Configure externamente as variáveis abaixo, via ambiente do processo ou arquivo local
ignorado `.env.smoke`. Não preencha scripts versionados nem cole segredos no terminal/chat.

```text
GERIT_SMOKE_BASE_URL=https://gerit-api.vianahub.pt
GERIT_SMOKE_EMAIL=
GERIT_SMOKE_PASSWORD=
```

```powershell
node --env-file=.env.smoke scripts/smoke/auth.mjs
# Com variáveis já disponíveis no processo:
node scripts/smoke/auth.mjs
node --test scripts/smoke/client.test.mjs
```

O padrão anterior do Web carrega `.env.e2e.local` e usa E2E_USER_EMAIL e
E2E_USER_PASSWORD nos formulários de login. A API possui configuração independente:
não lê automaticamente arquivos do Web. O contrato real é POST /v1/auth/login com
email/password. O tenant vem da autenticação; não há etapa adicional de seleção nem
GERIT_SMOKE_TENANT_ID. A resposta fornece accessToken e refreshToken; este helper
retém somente accessToken em memória, sem cookies, persistência, refresh ou retries.

O helper é exclusivamente produtivo: exige HTTPS e host exato gerit-api.vianahub.pt,
sem userinfo, porta explícita, query ou path na base. Redirects são bloqueados,
requests têm timeout de 30 segundos e só aceitam caminhos locais /v1/.
Ambientes alternativos exigem outra implementação explicitamente revisada.

Toda chamada envia Accept-Language; default pt-PT e overrides pt-BR, en-US, es-ES.
Falha de configuração ou login interrompe antes de retornar o cliente. Não há senha
default. Um 401 encerra o cliente, sem repetição automática de autenticação.

O smoke mínimo faz login e GET /v1/teams, sem criar dados. O login pode gerar sessão
operacional conforme a aplicação. Sucesso exige 200 e lista JSON. Nenhum body,
header, URL/query, token, cookie ou erro original do transporte é registrado.
O cliente retorna dados ao script: nunca os imprima integralmente, nem registre
objetos de configuração, credenciais ou respostas de autenticação.

## Reutilização e cleanup obrigatório

Importe createAuthenticatedSmokeClient de scripts/smoke/client.mjs. Métodos:
get(path, options), post/put/patch(path, body, options), delete(path, options).
Options admite locale; retornos têm status e data. Valide status e contratos em cada
cenário: o cliente não transforma automaticamente todo HTTP não-2xx em exceção.

Execute cada jornada dentro de client.run(async client => { ... }). Assim que uma
criação retornar um ID válido, registre client.deferCleanup(async () => { ... })
para remover somente aquele ID pela API e verificar o resultado. Não presuma IDs.
Cleanup roda em finally, em ordem inversa, tentando todos os callbacks mesmo se um
falhar. Callback deve lançar erro se a remoção/verificação falhar. Ao final, o cliente
é fechado e solta a referência ao token. Uma falha de cleanup deve ser reportada
como incompleta, nunca PASS; indisponibilidade/revogação de sessão pode impedir a
remoção. O operador deve reconciliar esses IDs pela API autorizada, nunca por SQL.

O helper não autoriza mutações por si só: cada smoke precisa de escopo aprovado,
nomes temporários identificáveis e mínimo de registros. Soft delete é suficiente
quando confirmado por 410 e ausência nas leituras normais. Não alterar permissões
produtivas para fabricar um 403. Este mecanismo não inicia nenhum resource smoke.

Arquivos .env, .env.local, .env.smoke, *.smoke.env e .smoke-secrets.* são ignorados.
Nunca adicionar com git add -f. Logs devem limitar-se a método, status, locale,
resultado e IDs temporários necessários para cleanup. Nenhum segredo é versionado.
