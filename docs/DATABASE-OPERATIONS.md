# Banco da aplicação e startup da API

O startup configura serviços, constrói a aplicação e registra middleware/endpoints antes de executar o host. Não aplica migrations EF, não cria o banco, não processa `docs/sql/Create-Tables.sql`, não reconcilia tabelas, colunas, índices, functions ou security policies e não executa seed de dados de negócio.

O fluxo anterior chamava `InitializeDatabaseAsync`: a opção `Database:AutoMigrate` controlava migrations e reconciliação de schema, mas o seed era executado mesmo com a opção desativada. A chamada, a opção e os helpers sem consumidores foram removidos. Não há flag substituta.

O banco e os dados necessários devem existir antes de iniciar a API. Provisionamento, migração e seed dependem de operação explícita, revisada, com as permissões adequadas. O processo operacional/CI-CD para essas ações será definido separadamente; iniciar ou reiniciar a API não o substitui.

As migrations existentes e os scripts de `docs/sql/`, incluindo `Create-Tables.sql` e os arquivos `Initial_BackOffice*.sql`, permanecem artefatos operacionais. Nenhum desses scripts foi alterado ou executado por esta mudança. Os relatórios históricos de auditoria podem mencionar o antigo código de inicialização; essas referências não representam o runtime atual.

O storage interno do Hangfire é uma preocupação separada: `PrepareSchemaIfNecessary=true` permanece inalterado. O servidor Hangfire, seus hosted services e a sincronização operacional das definições de jobs continuam funcionando como antes; suas escritas operacionais não constituem seed de dados de negócio da aplicação. Esta decisão não altera autenticação, autorização, JWT lifecycle, RLS, jobs, endpoints ou entidades.

Os testes `DatabaseStartupArchitectureTests` inspecionam os assemblies de produção para impedir a reintrodução dos helpers removidos e de referências às APIs de provisioning do EF (`Migrate`, `MigrateAsync`, `EnsureCreated`, `EnsureCreatedAsync`). Também validam que os arquivos `appsettings*.json` da API não exponham as flags de mutation automática. Eles não executam migrations nem seed.
