# Entrega E Validacao - EquilibraFit++

Registro da implementacao e configuracao entre 2026-10-04 e 2026-10-07.
Esta entrega NAO certifica prontidao Beta: gates externos continuam pendentes.

## Projeto

Destino: D:\Curso\APP\EquilibraFit++.
Origem read-only: D:\Curso\APP\EQUILIBRAFIT.
Git local independente inicializado em main, sem commits, remote ou push.
Repositorio remoto pretendido: rasweb12/EQUILIBRAFIT-PLUSPLUS, ainda nao criado.
Nenhuma alteracao foi feita no original; seu Program.cs Admin ja estava modificado
antes desta derivacao e foi preservado.

## Alteracoes

- Nova identidade EquilibraFit++ / EquilibraFitPlusPlus em namespaces, clientes,
  metadata, Swagger, Admin e IA.
- Modelo de negocio portado com Clean Architecture, contratos, repositorios e UoW.
- PostgreSQL via Npgsql, SQLite backend apenas Development/Testing.
- Supabase Auth substitui emissao local/Identity stores; JWT assimetrico/JWKS,
  perfis e tenant confiaveis, confirmacao/recovery/refresh/logout e revogacao.
- Sete migrations PostgreSQL novas, RLS em todas as tabelas app, client writes
  negados e billing/sessoes/auditoria privados.
- SQLite Flutter independente, owner isolation, outbox, idempotencia, 409,
  retry, reinicio, cache e pending AI preservados.
- Google Play Billing com verificacao servidor, tokens cifrados, eventos,
  RTDN autenticado, restore e reconciliacao. Billing desligado por padrao.
- Admin preservado com bootstrap por UUID, logout e refresh protegido contra
  corrida/troca de conta.
- FastAPI preservado com fallback, contexto/memoria/RAG, logs JSON e chave interna.
- Dockerfiles/Compose/Blueprint novos, Redis opcional, sem banco local/SQL/Azure.
- Documentacao de arquitetura, Supabase, Render, billing, seguranca e operacao.

## Arquivos

Inventario completo em [port-inventory.csv](port-inventory.csv), com acao e
referencia por arquivo. Geracao por scripts/New-PortInventory.ps1 compara apenas
arquivos publicaveis e nao modifica a origem.
Inventario atual: 84 criados, 157 copiados e 441 adaptados; o proprio CSV fica fora
da contagem. Nenhum arquivo da origem foi apagado.
Adaptado inclui renomeacao tecnica/branding e formatacao, nao apenas logica nova.

Principais criados:
SupabaseAuthentication/SupabaseAuthenticationService/SupabaseSessionPolicy,
SupabaseAccountController, AuthSession e migrations PostgreSql, GooglePlayBilling*
e BillingTokenProtector, pagina Premium/billing mobile, ResetPasswordPage,
testes Auth/RLS/SQLite/billing/offline/Admin, docs, .env.example, Compose, Blueprint,
scripts de smoke e schema validation, .gitattributes e infra/README.

Principais adaptados:
DbContext/factory, DependencyInjection, Program/health, Usuario/AdminRepository,
controller Premium, AdminApiClient/AdminSessionState/layout, Flutter router,
session/API client/SQLite/outbox/sync/workouts/Coach, metadata/Gradle/manifests,
Dockerfiles, solution/projetos/packages.

Descartados da nova base:
authentication propria JwtOptions/IJwtTokenService/JwtTokenService/
AuthenticationService e stores Identity locais, configuracao SQL/Hangfire,
historico SQL Server, Azure/Bicep, billing antigo e configuracoes de producao.
Nenhum arquivo do original foi removido. Nao houve importacao de dados,
bancos, .env, secrets ou keystores. O inventario descreve a derivacao, nao um
git diff historico do original nem uma migracao de dados.

## Dependencias

Removidas do novo backend:
Microsoft.EntityFrameworkCore.SqlServer, Hangfire.SqlServer,
Hangfire.AspNetCore e Microsoft.AspNetCore.Identity.EntityFrameworkCore.
Stripe/Azure nao fazem parte da nova runtime; nenhum SDK novo deles foi adicionado.

Adicionadas:
Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1,
Microsoft.EntityFrameworkCore.Sqlite 10.0.5,
SQLitePCLRaw.lib.e_sqlite3 3.50.3 e Google.Apis.Auth 1.77.0.
Flutter: in_app_purchase 3.3.1, crypto 3.0.7 e sqflite_common_ffi 2.4.3 para testes.
supabase_flutter nao foi necessario: adapter da API conserva clientes maduros.
jsonschema foi instalado somente na .venv de validacao, nao na runtime AI.

## Migrations

1. 20261004165529_InitialPostgreSql
2. 20261004165930_SupabaseAuthProfiles
3. 20261004170424_SupabaseRowLevelSecurity
4. 20261004182432_GooglePlayBilling
5. 20261004211837_ProtectBillingData
6. 20261004215736_GlobalSessionRevocation
7. 20261007042719_ProtectMigrationHistory

Baseline independente, modelo completo, seeds e indices parciais preservados.
Aplicar sequencia inteira em banco novo. A segunda migration remove stores
Identity transitorios do primeiro baseline; estado final e Supabase Auth.
Nenhuma migration SQL Server da origem e executada no novo banco.
Model check confirma ausencia de diferencas pendentes.
SQLite usa EnsureCreated em testes/dev, nunca migrations PostgreSQL.
Leia [Supabase](supabase.md) para bootstrap, TLS, RLS e testes remotos.

## Testes Executados

| Verificacao | Resultado |
| --- | --- |
| dotnet restore | Aprovado |
| dotnet build -c Release | Aprovado, zero erros |
| dotnet test -c Release | 183 aprovados, 2 SKIP remotos, zero falhas apos correcao do fixture |
| Flutter pub get | Aprovado, sem upgrade indiscriminado |
| Flutter analyze | Sem problemas |
| Flutter test | 32 aprovados |
| Android APK debug | Regenerado em 2026-10-07, instalado no emulator-5554 e inicializacao visual confirmada |
| Ruff AI | Aprovado |
| pytest AI | 26 aprovados |
| EF has-pending-model-changes | Sem diferencas |
| Bundle PostgreSQL | Windows e Linux x64 regenerados com sete migrations; execucao dos binarios remotos pendente |
| Smoke API/Admin/AI | Health/ready, Swagger, login Admin e fallback AI aprovados |
| Schemas Compose/Render | Aprovados pelos JSON Schemas oficiais |
| Docker compose config/build/up | Nao executados: Docker ausente |
| Supabase/PostgreSQL real via MCP | Sete migrations aplicadas e reaplicadas; schema, RLS A/B, soft delete, operation ID, FK/checks aprovados |
| Npgsql local real | CA configurada com VerifyFull; readiness 200 no host/emulador e sete migrations sem pendencias |
| Auth / Admin autenticado reais | Login 200, refresh 200, dashboard 200, logout 204, sessao revogada 401; bootstrap desabilitado |
| SMTP / email / Admin UI / mobile E2E | Pendentes; o smoke HTTP nao automatiza as interfaces nem entrega de email |
| Google Play sandbox / RTDN real | Nao executado: conta/produtos/credenciais ausentes |
| Render deploy | Nao executado: repositorio/conta/configuracao pendentes |

.NET: Domain 3, Application 66, Architecture 4, Infrastructure 52 (+2 SKIP),
API 53 e Admin 5. SKIP explicito e diferente de aprovacao remota.
Testes locais usam SQLite/in-memory, HTTP providers simulados e JWT RSA de teste.
RLS tem teste estatico e teste SQL remoto SET LOCAL ROLE aprovado em 2026-10-07,
independente dos dois testes xUnit ainda ignorados. Nenhum SKIP foi contado como
teste aprovado. Nenhuma senha/API key foi necessaria ao teste SQL via MCP.

Smoke reproduzivel: scripts/Test-LocalServices.ps1 inicia processos ocultos,
usa SQLite descartavel, testa e encerra apenas processos criados.
Nao testa login real, PostgreSQL, compras ou OpenAI.
Schemas estaticos nao substituem Docker build/up.

### Regressao De Timeout Auth

Em 2026-10-07, restore e build Release aprovados (avisos XML existentes),
19 testes focados do adapter e 24 testes focados HTTP/middleware aprovados.
Adicionado depois um caso de cancelamento apos headers enviados; a suite API
completa final tem 53 aprovados, incluindo esse caso.
A primeira suite completa apresentou uma falha de startup de fixture por
concorrencia no logger global Serilog; o erro foi reproduzido com TRX e corrigido
agrupando somente os dois conjuntos de WebApplicationFactory na mesma collection.
Suite API repetida: 53 PASS. Suite completa repetida sem filtros: 183 PASS,
2 SKIP remotos, zero falhas. TRX locais em artifacts/test-results, ignorados no Git.

Criados AuthProviderFailureTests.cs e Middleware/ExceptionHandlingMiddlewareTests.cs.
Adaptados SupabaseAuthenticationService/DependencyInjection, ApiControllerBase,
ExceptionHandlingMiddleware e seus testes, mais a collection em
SupabaseAuthorizationTests. Documentacao e inventario atualizados.
Nenhuma dependency, migration, schema, conta, chave ou APK mudou nessa correcao.
Flutter/Python nao foram reexecutados nesta correcao exclusiva do backend;
os resultados anteriores continuam registrados separadamente acima.
Os processos Debug do Visual Studio do usuario nao foram encerrados; parar a
depuracao, recompilar e iniciar novamente para carregar o backend atualizado.

## Problemas Encontrados E Corrigidos

- Connection alias vazio ocultando conexao real.
- Concorrencia/unique constraints, RowVersion e datas especificas do provider.
- BillingEvents sem RLS e acesso cliente a tokens de assinatura.
- Rotacao de sessao/outbox/pending AI durante troca de conta.
- Cadastro sem sessao/confirmacao assumindo formato errado de resposta Auth.
- Validacao MVC de records aplicada ao local incorreto.
- Deep link recovery descartado durante restauracao da sessao.
- Revogacao global de session_id inedito e refresh com autenticacao antiga.
- Logs Google contendo token no path de erros de transporte.
- Whitespace/EOF apontados por git diff --check, corrigidos somente no destino.
- Conflito Android debug HTTP versus manifest principal HTTPS: tools:replace
  somente em debug; release permanece sem cleartext.
- Grants automaticos em public do Supabase poderiam expor o historico EF:
  migration forward ProtectMigrationHistory habilita RLS e revoga grants cliente.
- Refresh real retornava 400: validador herdado exigia minimo de 32 caracteres.
  Tokens Supabase sao opacos; removido minimo, mantidos NotEmpty e limite 4096.
  Sete testes novos aprovados; autenticidade continua validada pelo provider.
- Cadastro apresentou TaskCanceledException; Auth remoto registrou /signup 504
  request_timeout, seguido de um registro 200. Adapter agora trata timeout/rede/
  5xx/JSON invalido com erros seguros 504/503, sem retry ou provisionamento falso.
  HttpClient Auth limitado a 15 segundos, antes dos 20 do Flutter; cancelamento
  do cliente propagado e RequestAborted tratado sem escrever JSON/500.
  Nenhum signup/email real foi disparado pelo agente neste diagnostico; causa
  SMTP e entrega nao foram presumidas. Recompilar/reiniciar a API no Visual Studio.
- A ampliacao dos testes HTTP expos corrida entre dois WebApplicationFactory:
  o logger bootstrap Serilog global falhava com The logger is already frozen.
  As duas classes de host usam a mesma collection xUnit; somente esses hosts
  executam em sequencia. Nenhum teste foi removido ou flexibilizado.

O primeiro build Android falhou no manifest; o build de confirmacao passou.
aapt confirmou package br.com.equilibrafit.app.plusplus.dev e label EquilibraFit++.
APK: src/mobile/equilibrafit_plusplus_app/build/app/outputs/flutter-apk/app-debug.apk.
Em 2026-10-07, pub get/analyze/test foram repetidos: zero problemas e 32 testes
aprovados. APK debug regenerado com API_BASE_URL=http://10.0.2.2:5158 e instalado
com adb install -r no emulator-5554. aapt confirmou package/label/activity e
ABIs ARM32/ARM64/x86_64; processo e tela inicial foram conferidos.
SHA256: 17E2AEE11AE60FA3546625F942076D335C7921706297A0F3EF2E8C6B945E7653.
Captura local ignorada: artifacts/equilibrafit-plusplus-debug.png.
Um aviso Android Settings Services foi fechado; nao era erro do processo Flutter.
Usuario optou por apenas debug por enquanto; release/publicacao nao executados.
Nenhum aplicativo original foi substituido nem dados de aplicativos apagados.
Clean builds emitiram CS1591 (documentacao XML ausente) em tipos novos/testes;
o build Release final passou sem erros, com avisos XML existentes. Nao sao falhas
funcionais, mas permanecem
como trabalho de documentacao. Flutter informa futuras mudancas de Kotlin Gradle
Plugin e dependencias mais novas; nenhum upgrade amplo foi feito nesta Sprint.

## Historico De Diagnostico

Os paragrafos abaixo registram a ordem das verificacoes, incluindo falhas depois
resolvidas. O estado atual e o da tabela acima e do fechamento Auth abaixo.

Atualizacao 2026-10-07: conectores Supabase/Resend acessiveis. Organizacao rasweb;
projeto EquilibraFit++ knaubynyytqnyyrdrkfi ativo em sa-east-1, distinto de Vero.
Banco vazio inicializado via SQL EF gerado (idempotente, sem transacoes internas)
e MCP apply_migration; historico EF permanece autoritativo. Um registro tecnico
Supabase documenta apenas o bootstrap, nao uma segunda estrategia de evolucao.
Reaplicacao inicialmente bloqueada pelo revisor por DROP condicionais Identity;
apos autorizacao explicita do usuario, guard de historico/lock/transacao,
reaplicacao PASS com tenant/usuarios preservados.
Confirmados 39 tabelas app com RLS, 22 policies, 34 foreign keys, 34 checks e
tres indices parciais. verify-database.sql remoto aprovou isolamento A/B,
SELECT proprio, CRUD cliente negado, billing/sessoes/auditoria/historico privados,
soft delete, operation ID, FK e checks, revertendo todos os fixtures.
Security advisor possui somente INFO de default-deny sem policies em tabelas
privadas; performance aponta indices ainda nao usados. Remediacoes e contexto
em [Supabase/Resend](supabase-resend-setup.md); nao criar policies permissivas.
JWKS publico ES256 conferido; login/refresh/recovery Auth reais ainda pendentes.
User Secrets API receberam SUPABASE_URL, DATABASE_PROVIDER=PostgreSQL e a
publishable key fornecida pelo usuario; chave mantida fora do repositorio.
GET Auth settings com a chave aprovado: email/cadastro habilitados, confirmacao
obrigatoria. Sem login, cadastro ou email real nessa consulta.
Usuario preenchera a senha/conexao fora do Git/chat. URI Direct connection
fornecida ainda contem YOUR-PASSWORD e nao foi salva como conexao funcional.
A maquina nao tem IPv6 global: obter Session pooler no painel Connect, sem
inventar hostname. .env nao e importado automaticamente no Visual Studio.
Atualizacao com campos fornecidos: Session pooler aws-0-sa-east-1.pooler.supabase.com,
5432, postgres.knaubynyytqnyyrdrkfi; TCP aprovado. ConnectionStrings:Default
preparada nos User Secrets com PREENCHA_SUA_SENHA_AQUI, sem sobrescrever conexao
existente. EF dbcontext info aprovou sintaxe/provider/database/data source;
nao abriu conexao autenticada. Senha, TLS real e readiness Npgsql pendentes.
Verificacao posterior: o placeholder foi substituido localmente pelo usuario,
sem exibir credenciais. GET /health/ready retornou 503; logs e inspecao TLS sem
autenticacao confirmaram RemoteCertificateChainErrors/UntrustedRoot na CA Supabase.
O hostname do certificado corresponde ao pooler; nao houve bypass TLS.
EF migrations list imprime as sete migrations mesmo sem conectar ao banco:
exit code zero dessa ferramenta NAO comprova autenticacao/migrations aplicadas.
A API na porta 5158 estava parada quando o mobile mostrou erro de conexao.
Foi iniciada em background com perfil http; /health retornou 200 tanto no host
como via 10.0.2.2 a partir do emulador. /health/ready permanece 503.
API em execucao deve ser encerrada antes de iniciar outra instancia nessa porta.
Consulta remota confirmou zero contas Auth, perfis e administradores.
Usuario escolheu admin@equilibrafit.local: conta/senha ainda nao criadas;
bootstrap administrativo nao foi habilitado para UUID inexistente.
Atualizacao posterior: CA oficial fornecida em Certificate/prod-ca-2021.crt,
publica e sem chave privada. Cadeia/hostname validados; connection string nos
User Secrets recebeu Root Certificate mantendo VerifyFull. Nenhum bypass TLS
ou alteracao de confianca global do Windows. Pasta Certificate/ ignorada no Git.
Readiness real retornou 200 no host e no emulator-5554. EF migrations list
conectou via Npgsql, confirmou sete IDs e zero pendencias, sem erro de leitura.
Testes .NET repetidos com --no-build --no-restore: 137 PASS, 2 SKIP, zero falhas.
Admin iniciado em background em http://localhost:5029/login, pagina HTTP 200;
login administrativo real nao validado. Usuario informou criacao da conta,
mas a consulta do projeto knaubynyytqnyyrdrkfi ainda confirmou zero contas Auth.
Solicitada verificacao no painel correto; nenhuma role ou senha foi presumida.
Fechamento posterior: conta encontrada e confirmada no projeto correto, UUID
b6e9b851-bed3-4262-987f-45084849d38b. Primeiro login real retornou 200 e provisionou
app.Usuarios. Bootstrap temporario promoveu somente esse perfil a Administrador,
com exatamente uma auditoria admin.bootstrap; depois foi desabilitado e a API
reiniciada. Role 2, status Ativo e perfil nao excluido confirmados por SQL.
Smoke autenticado repetido apos corrigir o validador de refresh: login 200,
refresh 200, dashboard 200, logout 204 e dashboard com sessao revogada 401.
Uma sessao revogada confirmada no banco. Tokens/senha ficaram somente em memoria,
sem gravacao em arquivos ou exibicao em logs. Senha temporaria deve ser trocada
pelo operador diretamente no Supabase; .local e apenas desenvolvimento.
Sete testes de regressao adicionados ao validador. Build Release aprovado e
suite completa final: 144 PASS, 2 SKIP, zero falhas. API/Admin permanecem em
background nas portas 5158/5029 com bootstrap desabilitado. Login UI/mobile
e envio/recovery email nao foram automatizados por esse smoke HTTP.
Resend equilibrafitplus.com.br existente, sending enabled, tracking false,
verificacao disparada e ainda pending; DNS/NS nao encontrados na consulta local.
O conector nao expoe SMTP/templates/Auth config nem painel DNS: configuracao
manual preparada, ainda nao salva. Nenhum email foi enviado ou chave secreta SMTP
lida; somente a chave publica fornecida foi registrada e validada nessa etapa.
Criados templates confirmation/recovery e teste SQL; testes backend repetidos
com 137 PASS +2 SKIP e build Release zero erros (avisos XML CS1591 existentes).
Arquivo Android bugreport ZIP preexistente preservado e ignorado no Git por
conter potencialmente dados de diagnostico pessoais; conteudo nao foi aberto.

## Ambiente E Operacao

Secrets em .env local/IDE/Render, nunca Git. .env.example contem apenas placeholders.
API: SUPABASE_URL, SUPABASE_PUBLISHABLE_KEY, SUPABASE_DB_CONNECTION_STRING ou
ConnectionStrings__Default, AiCoach__BaseUrl/ApiKey/TimeoutSeconds, CORS,
AdminBootstrap__Enabled/SupabaseUserId e configuracoes Google Play.
AI: EQUILIBRAFIT_AI_API_KEY, EQUILIBRAFIT_AI_OPENAI_API_KEY/MODEL e flags existentes.
Admin: AdminApi__BaseUrl. Redis opcional: ConnectionStrings__Redis.
Nao ha JWT secret proprio ou senha bootstrap local.

Depois de configurar projeto Supabase dedicado, em shell com variaveis:

```powershell
dotnet tool restore
dotnet ef database update --project src/backend/EquilibraFitPlusPlus.Infrastructure --startup-project src/backend/EquilibraFitPlusPlus.Api
docker compose config --quiet
docker compose build
docker compose up -d
docker compose ps
```

Mobile:
```powershell
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5158
flutter run --dart-define=API_BASE_URL=https://equilibrafit-plusplus-api.onrender.com
```

Confirme a URL realmente atribuida pelo Render. Release exige ID publico e
assinatura proprios; debug usa br.com.equilibrafit.app.plusplus.dev.
Nenhum applicationId ou keystore do original foi alterado.

Deploy: apos revisao e autorizacao, criar repositorio independente, commit
Initial EquilibraFit++ architecture e push main. Conectar NOVO Blueprint ao
novo repo no painel Render, configurar secrets, validar health e fluxos.
Nenhum comando de commit/push e executado automaticamente.

## Gates Beta E Proxima Sprint

- Instalar/disponibilizar Docker, provar config/build/up dos tres containers.
- Schema Supabase/SQL RLS/reaplicacao aprovados por MCP, e Npgsql local/readiness
  reais aprovados com CA e VerifyFull. Auth assimetrico e Admin por HTTP aprovados;
  validar DNS, email/SMTP e fluxos UI completos.
- TEST_POSTGRES_CONNECTION_STRING e TEST_SUPABASE_DB_CONNECTION_STRING em bancos
  dedicados, executar os dois testes atualmente ignorados.
- E2E real: cadastro/confirmacao/login/refresh/recovery/onboarding/treino/sessao,
  alimentacao/evolucao/Coach/Admin/LGPD/logout/novo login, isolamento A/B.
- E2E Android offline: fechar/abrir, trocar usuario, reconectar, idempotencia,
  409 e processamento pending AI. Testes automatizados locais nao substituem
  essa verificacao no aparelho.
- Definir applicationId Play publico, assinatura propria, produtos/license
  testers, ADC/RTDN/chave AES e validar cenarios de assinatura sandbox.
- Publicar novo repo somente com autorizacao; deploy Render e TLS/health reais.
- Validar OpenAI real e cold starts/always-on para AI e worker de billing.
- Futuro: OAuth, Storage privado/signed URLs, iOS/browser recovery, troca de
  plano com prorata na UI, exclusao definitiva auth.users/LGPD revisada,
  politica de protecao/backup SQLite e atualizacao Kotlin/toolchain.

Nao declarar Supabase, billing, Docker ou Render funcionando antes de comprovar
os respectivos gates. Nenhum secret real deve ser enviado pelo chat.
