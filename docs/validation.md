# Entrega E Validacao - EquilibraFit++

Registro da implementacao e configuracao entre 2026-10-04 e 2026-10-07.
Esta entrega NAO certifica prontidao Beta: gates externos continuam pendentes.

## Projeto

Destino: D:\Curso\APP\EquilibraFit++.
Origem read-only: D:\Curso\APP\EQUILIBRAFIT.
Git independente em main. Remoto atual confirmado em 2026-10-07:
rasweb12/EquilibraFitPlusPlus. A derivacao inicialmente nao tinha commits/remote;
o operador configurou o repositorio e publicou os servicos posteriormente.
Nenhum commit/push foi feito na verificacao de URLs desta tarefa.
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
flutter run --dart-define=API_BASE_URL=https://equilibrafit-plusplus-api-4lkw.onrender.com
```

A URL da Beta foi confirmada em [Render](render.md). Release exige ID publico e
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
- Alterar/publicar codigo somente com autorizacao; TLS/health Render aprovados
  em 2026-10-07, fluxos autenticados no deploy ainda precisam de validacao.
- Validar OpenAI real e cold starts/always-on para AI e worker de billing.
- Futuro: OAuth, Storage privado/signed URLs, iOS/browser recovery, troca de
  plano com prorata na UI, exclusao definitiva auth.users/LGPD revisada,
  politica de protecao/backup SQLite e atualizacao Kotlin/toolchain.

Nao declarar Supabase, billing, Docker ou Render funcionando antes de comprovar
os respectivos gates. Nenhum secret real deve ser enviado pelo chat.

## Verificacao De URLs Render - 2026-10-07

URLs fornecidas pelo operador e verificadas com HTTPS/TLS normal:

| Check | Resultado |
| --- | --- |
| https://equilibrafit-plusplus-api-4lkw.onrender.com/health | 200 Healthy |
| https://equilibrafit-plusplus-api-4lkw.onrender.com/health/ready | 200 Healthy; banco, seed e migrations prontos |
| https://equilibrafit-plusplus-ai-4lkw.onrender.com/health | 200 Healthy |
| https://equilibrafit-plusplus-admin-4lkw.onrender.com/health | 200 Healthy |
| https://equilibrafit-plusplus-admin-4lkw.onrender.com/login | 200; EquilibraFit++ Admin |
| API /api/v1/admin/dashboard sem token | 401 |
| AI /api/v1/coach/chat sem chave interna | 401 |

Supabase MCP confirmou EquilibraFit++, ref knaubynyytqnyyrdrkfi, ACTIVE_HEALTHY,
URL https://knaubynyytqnyyrdrkfi.supabase.co. Configuracao local conferida sem
exibir credenciais: session pooler aws-0-sa-east-1.pooler.supabase.com:5432,
SSL Mode VerifyFull. Vero e projeto original nao foram alterados.

Arquivos criados nesta verificacao:
- src/backend/EquilibraFitPlusPlus.Api/appsettings.Production.json
- src/admin/EquilibraFitPlusPlus.Admin/appsettings.Production.json
- src/mobile/equilibrafit_plusplus_app/test/core/config/app_config_test.dart
- tests/EquilibraFitPlusPlus.Architecture.Tests/Deployment/ServiceUrlConfigurationTests.cs

Arquivos adaptados nesta verificacao:
- .env.example: distinguir destinos locais dos valores Render.
- README.md: URL Beta, comando debug e repositorio realmente configurado.
- docs/render.md: URLs, overrides, verificacoes e limites observados.
- docs/supabase-resend-setup.md: atualizar estado do frontend publicado.
- docs/validation.md: este registro e comandos atualizados.
- src/mobile/equilibrafit_plusplus_app/lib/core/config/app_config.dart: default
  debug HTTPS da Beta; API_BASE_URL explicita ainda tem prioridade.
- src/mobile/equilibrafit_plusplus_app/lib/core/http/api_client.dart: remover
  instrucao de usar IP local no erro de uma API remota.
- src/mobile/equilibrafit_plusplus_app/test/core/http/api_client_test.dart:
  regressao da mensagem de falha de conexao remota, sem chamada de rede real.

Nenhum arquivo copiado, removido ou descartado nesta verificacao. Nenhuma
dependencia ou migration adicionada/removida. SQLite, outbox, sync, idempotencia,
conflitos e autenticacao nao foram reescritos. Alteracao preexistente no Program.cs
da API nova preservada; Program.cs Admin do original continua com a alteracao
preexistente, sem edicoes desta tarefa.

localhost e IPs loopback restantes estao em configuracao de desenvolvimento,
launch profiles, fixtures, scripts locais e health checks/binds Docker. O
Blueprint usa DNS/URLs de servicos, e os arquivos Production usam URLs HTTPS.
Guarda de release ainda rejeita localhost/10.0.2.2. Registros historicos de
testes locais foram preservados como historico, nao como destinos da Beta.

Testes repetidos nesta verificacao:
- dotnet restore: PASS.
- dotnet build -c Release: PASS, zero erros no build final. O primeiro build
  completo exibiu avisos XML CS1591 preexistentes e dois erros no teste novo,
  corrigidos antes da repeticao; build incremental final sem avisos.
- dotnet test -c Release: 187 PASS, 2 SKIP, zero falhas; seis TRX conferidos
  em artifacts/test-results/render-urls. SKIPs continuam sendo os bancos
  descartaveis PostgreSQL/RLS nao configurados, nao aprovacao desses testes.
- flutter pub get: PASS; nenhuma atualizacao de dependencias solicitada.
- flutter analyze: PASS apos corrigir virgula exigida no teste novo.
- flutter test: 39 PASS, incluindo offline/outbox; teste de configuracao
  repetido com API_BASE_URL=http://10.0.2.2:5158: mais seis PASS.
- Ruff: PASS. pytest: 26 PASS.
- scripts/validate_deployment.py: PASS nos schemas oficiais Render e Compose,
  referencias de servicos/secrets/banco remoto e Redis opcional. Isso NAO
  substitui docker compose config/build/up; Docker nao esta instalado aqui.
- git diff --check: PASS. Triagem de padroes de chaves privadas nos arquivos
  alterados sem achados; nao equivale a uma auditoria completa do repositorio.

APK debug recompilado com:
flutter build apk --debug --dart-define=API_BASE_URL=https://equilibrafit-plusplus-api-4lkw.onrender.com
Instalado com adb install -r no emulator-5554, sem limpar dados. Package
br.com.equilibrafit.app.plusplus.dev, versao 1.0.0+1, tela inicial aberta e
branding conferido. Nenhum aparelho fisico conectado; nenhum release gerado.
APK: src/mobile/equilibrafit_plusplus_app/build/app/outputs/flutter-apk/app-debug.apk.
SHA256: F574D740CF7E4B292695BC7616160CF3552772A56DA75E5C343FB4B10B075BA5.
Kernel Dart do APK contem a URL HTTPS e nao contem os antigos destinos
http://10.0.2.2:5158 ou http://localhost:5158. Captura em
artifacts/equilibrafit-plusplus-render.png, ignorada pelo Git.

Limites: a primeira resposta da API levou 57 segundos; isso e compativel com
cold start e pode exceder o timeout mobile de 20 segundos. Nao foram alterados
timeouts/retries nem contratados recursos pagos. Variaveis privadas efetivas do
Render nao foram inspecionadas: overrides tem prioridade sobre appsettings.
Conferir AiCoach__BaseUrl, AdminApi__BaseUrl e chave interna comum conforme
docs/render.md. Health checks nao comprovam login/Admin -> API/API -> AI
autenticados, OpenAI, entrega SMTP, billing ou fluxo mobile completo.
Nao houve commit, push ou redeploy automatico nesta tarefa.

## Cadastro No Emulador - Timeout De Resposta

Atualizacao posterior a verificacao de URLs, em 2026-10-07. O operador confirmou
que a captura era do emulador durante cadastro. Log filtrado do emulator-5554:
POST /api/v1/auth/cadastrar iniciado as 19:30:00.998 UTC e receiveTimeout as
19:30:22.104 UTC, aproximadamente 21 segundos. Nenhum token, senha ou corpo do
cadastro foi impresso. Rede do emulador VALIDATED, sem proxy global configurado.
Health da API 200 em 6,17 segundos; ready 200 em 0,79 segundo. POST com campos
vazios retornou 400 de validacao em 1,52 segundo, sem criar usuario/enviar email.

Cold start e uma explicacao compativel: Render Free documenta aproximadamente
um minuto para iniciar, e antes foi observada resposta de 57 segundos.
Nao se comprovou a causa no log do Render. Consulta somente leitura dos logs
Auth do Supabase nao encontrou /signup no horario dessa tentativa; havia
registros antigos 504 request_timeout e 200. Isso nao prova que entrega SMTP
esteja resolvida nem que a conta do operador tenha sido criada.

Correcoes limitadas ao cliente mobile:
- AuthApi.authenticationTimeout = 90 segundos somente para cadastro/login,
  usando o mecanismo existente de timeout por request do ApiClient.
- Conexao, timeout normal de requests/logout (20 segundos), IA (75 segundos),
  SQLite/outbox/offline, payloads de consentimento e sessao foram preservados.
- receiveTimeout agora tem mensagem de demora do servidor, distinta de uma
  falha de conexao; codigo network_unavailable preservado por compatibilidade.
- Nenhum retry automatico de cadastro adicionado.

Criado: src/mobile/equilibrafit_plusplus_app/test/features/auth/auth_api_test.dart.
Adaptados: lib/features/auth/data/auth_api.dart, lib/core/http/api_client.dart e
test/core/http/api_client_test.dart dentro do projeto Flutter; docs/render.md e
docs/validation.md. Nenhum arquivo copiado/descartado/removido, dependency ou
migration alterada. Backend/Supabase/Render/original nao foram modificados.

flutter analyze: PASS depois de corrigir duas virgulas exigidas nos testes.
flutter test: 46 PASS, zero falhas, incluindo seis casos AuthApi novos e uma
regressao da mensagem de receiveTimeout. Teste usa servidor HTTP descartavel
loopback com resposta atrasada alem do timeout normal reduzido do fixture e
confirma uma unica chamada; nao envia cadastro para servicos reais.
Nao foram repetidos .NET/Python/Docker nesta etapa, pois nao sofreram alteracoes.
Build APK debug: PASS; aviso Kotlin/toolchain preexistente permanece.

APK recompilado com a API HTTPS publicada, instalado com adb install -r em
emulator-5554, sem limpar dados. Kernel verificado: URL Beta e simbolo
authenticationTimeout presentes; antigos destinos de API locais ausentes.
SHA256 atual: 1952EDDDD8ED8EB89487ABA29C9542C5D41A1CBF790E90987A070741BA241A9D.
Package br.com.equilibrafit.app.plusplus.dev e applicationId do original intacto.
Nenhum release, commit, push, redeploy ou recurso pago criado. Cadastro real,
confirmacao e entrega de email ainda devem ser tentados pelo operador; este
ajuste evita o corte prematuro do cliente, nao certifica todos os fluxos Auth.

## Email De Confirmacao E Retorno Android

Em 2026-10-07, o operador recebeu o email padrao em ingles com redirect_to
apontando para localhost:3000. O link/token encaminhado NAO foi aberto,
reutilizado, copiado em arquivos ou enviado a ferramentas de navegacao.

Adaptados: infra/supabase/templates/confirmation.html (portugues, branding,
CTA e ConfirmationURL individual), AndroidManifest.xml (intent exato /login,
preservando /recovery), docs/supabase.md e docs/supabase-resend-setup.md.
Criado: tests/EquilibraFitPlusPlus.Architecture.Tests/Deployment/EmailConfirmationConfigurationTests.cs.
Nenhum arquivo copiado/removido, dependencia/migration alterada, endpoint novo
ou mudanca no original. Login continua exigindo senha pela API; nao se cria
sessao com tokens recebidos no deep link.

Publicacao remota PENDENTE: em Authentication -> Email -> Templates -> Confirm
sign up, salvar assunto Confirme seu e-mail - EquilibraFit++ e o HTML preparado.
Em URL Configuration, Site URL equilibrafitplusplus://auth/login e redirects
exatos de login/recovery. As ferramentas MCP nao editam esses campos e nao ha
Management API token configurado. Arquivos locais/Render nao os publicam.
Emails ja enviados conservam o link anterior. Nao desabilitar confirmacao.

Testes desta etapa:
- dotnet test do projeto Architecture em Release: 12 PASS, zero falhas finais.
  Primeira execucao teve duas falhas do parser XML por DOCTYPE minusculo;
  template corrigido e suite repetida. Quatro casos novos cobrem branding,
  ConfirmationURL e os dois destinos Android. TRX em artifacts/test-results/email-confirmation.
- flutter analyze: PASS, zero issues.
- flutter test: 46 PASS, incluindo offline/outbox e autenticacao.
- flutter build apk --debug com API HTTPS publicada: PASS. Aviso Kotlin
  preexistente continua; nao foi alterado o toolchain nesta tarefa.
- adb install -r emulator-5554: Success, dados locais preservados.
- Package manager resolve /login e /recovery para MainActivity do debug
  br.com.equilibrafit.app.plusplus.dev. URI de teste nao contem credencial.
- Abertura quente de equilibrafitplusplus://auth/login: Status ok, rotulo
  Entrar confirmado. Captura de abertura fria mostrou login, mas o Android
  exibiu Process system isn't responding; duas esperas am start -W expiraram.
  Nao certificar desempenho de abertura fria ou E2E real a partir deste teste.
  Escolhido Wait, sem reiniciar emulador/apagar dados/fechar o aplicativo original.

SHA256 do APK atual:
A9014F39ED28FA64C540A0E79101028C96D20CE980BA3BD21B66A1847E5DC308.
Artefatos APK/TRX/capturas ignorados pelo Git. Nenhum release, email enviado,
conta confirmada, secret salvo, recurso pago, commit ou push nesta etapa.
Backend completo/Python/Docker nao foram repetidos: runtime dessas camadas
nao foi alterado. Confirmacao real e novo email com URL correta dependem da
publicacao do template/URL pelo operador no Supabase.

## Preparacao De Assinaturas E Release - 2026-10-07

Escopo desta etapa: preparar o release independente e corrigir falhas de Billing.
Nao declarar Beta pronta: keystore Android, produtos/credenciais Play, sandbox
e deploy do codigo novo continuam pendentes. Original e Vero nao alterados.

### Alteracoes

- ID autorizado pelo operador: br.com.equilibrafit.app.plusplus, configurado
  em android/gradle.properties. Debug recebe .dev e preserva a identidade
  br.com.equilibrafit.app.plusplus.dev. Release nunca usa a chave debug.
- Mobile preserva entitlement obtido do servidor durante falhas da loja,
  impede comandos concorrentes, trata erros de purchase stream/restore/buy
  e somente completa recibos com resposta valida e productId correspondente.
  Nao existe desbloqueio local/falso de Premium nem produto ficticio publicado.
- Backend valida configuracao habilitada antes do startup. Falhas de credenciais
  e transporte Google nao propagam detalhes privados; outages/ack retornam 503,
  timeout 504, resposta invalida 502, compra/owner invalidos 400.
- Blueprint preparado para escolha manual GooglePlay__Enabled e secrets,
  package aprovado, audience RTDN HTTPS e ADC em /etc/secrets/google-play.json.
  Nao modifica variaveis reais do Render nem cria produtos/conta Google/PubSub.
- Script de release executa pub get/analyze/test antes de APK e AAB, com
  preflight HTTPS/assinatura e interrupcao na primeira falha. Caminho de
  keystore no exemplo corrigido para absoluto, sem credencial verdadeira.
- SQLite/offline/outbox/sync/conflitos/IA e regras de negocio preservados.

Criados:
```text
docs/android-release.md
scripts/Build-AndroidRelease.ps1
tests/EquilibraFitPlusPlus.Architecture.Tests/Deployment/AndroidReleaseConfigurationTests.cs
tests/EquilibraFitPlusPlus.Infrastructure.IntegrationTests/Billing/GooglePlayOptionsTests.cs
tests/EquilibraFitPlusPlus.Infrastructure.IntegrationTests/Billing/GooglePlayBillingProviderTests.cs
```

Adaptados:
```text
.env.example
render.yaml
docs/google-play-billing.md
docs/render.md
docs/validation.md
src/backend/EquilibraFitPlusPlus.Api/Middleware/ExceptionHandlingMiddleware.cs
src/backend/EquilibraFitPlusPlus.Infrastructure/Billing/GooglePlayOptions.cs
src/backend/EquilibraFitPlusPlus.Infrastructure/Billing/GooglePlayBillingProvider.cs
src/mobile/equilibrafit_plusplus_app/android/app/build.gradle.kts
src/mobile/equilibrafit_plusplus_app/android/gradle.properties
src/mobile/equilibrafit_plusplus_app/android/key.properties.example
src/mobile/equilibrafit_plusplus_app/lib/features/premium/data/google_play_billing.dart
src/mobile/equilibrafit_plusplus_app/lib/features/premium/presentation/pages/premium_page.dart
src/mobile/equilibrafit_plusplus_app/test/features/premium/google_play_billing_test.dart
tests/EquilibraFitPlusPlus.Api.IntegrationTests/Middleware/ExceptionHandlingMiddlewareTests.cs
```

Nenhum arquivo copiado, removido ou descartado; nenhuma dependencia ou migration
adicionada/removida. Google Play Billing nativo continua na versao 8.0.0 trazida
pelo plugin resolvido, sem upgrade amplo de dependencias. Nenhum secret real
escrito, conta criada, email enviado, compra executada ou infraestrutura paga.

### Verificacoes

| Comando/check | Resultado |
| --- | --- |
| dotnet restore | PASS |
| dotnet build -c Release --no-restore | PASS, zero erros/avisos no build final incremental |
| dotnet test -c Release --no-build | 227 PASS, 2 SKIP, zero falhas |
| flutter pub get | PASS, sem alteracao de versoes |
| flutter analyze | PASS apos corrigir sete avisos de estilo novos |
| flutter test | 56 PASS, incluindo 14 testes Billing |
| Ruff check | PASS |
| pytest | 26 PASS |
| scripts/validate_deployment.py | PASS nos schemas oficiais Render/Compose |
| Script release: parser, HTTP/loopback e assinatura ausente | PASS, bloqueios esperados |
| flutter build apk --release com API HTTPS | BLOQUEADO: credenciais de assinatura ausentes |
| docker compose config | INDISPONIVEL: docker nao encontrado |
| docker compose build/up | NAO EXECUTADOS: Docker ausente |
| API /health/ready, AI /health, Admin /health publicados | HTTPS 200 nos tres |
| git diff --check e triagem de chaves privadas nos arquivos alterados/novos | PASS |

TRX conferidos: artifacts/test-results/release-check, seis arquivos, 229 casos,
227 aprovados e dois ignorados. SKIPs: PostgreSqlMigrationTests e SupabaseRlsTests,
sem TEST_POSTGRES_CONNECTION_STRING/TEST_SUPABASE_DB_CONNECTION_STRING dedicadas.
Nunca executar esses testes destrutivos contra o banco real da Beta.
O primeiro build focado exibiu CS1591 existentes e nos testes publicos novos;
nao confundir o build incremental sem avisos com eliminacao de toda essa divida.
Triagem de chaves privadas nao equivale a auditoria completa de secrets.

### Bloqueios E Proximos Passos

Certificate contem apenas prod-ca-2021.crt, CA TLS do banco, nao keystore Android.
Nao havia android/key.properties nem as quatro variaveis ANDROID_KEY* configuradas.
A tentativa real de assembleRelease terminou com a guarda de assinatura no
build.gradle.kts; nenhum APK/AAB release foi produzido ou instalado nesta etapa.
Nao usar assinatura debug como substituta. Criacao/backup da chave e build:
`docs/android-release.md` e `scripts/Build-AndroidRelease.ps1`.

Ativacao Play exige app novo com o package aprovado, produtos/base plans ativos,
ADC com permissoes Play, AES com backup, RTDN OIDC e license testers. Produtos e
credenciais reais nao foram informados; nao habilitado remotamente e nao testado
em sandbox. Usar pagamentos de teste: teste interno sozinho nao evita cobranca.
Publicar AAB no teste interno so apos configurar assinatura propria.

Deploy segue docs/render.md apos revisao/autorizacao de commit/push no repositorio
independente rasweb12/EquilibraFitPlusPlus. Nao houve commit/push ou redeploy.
Health 200 comprova servicos ja publicados, nao a publicacao destas alteracoes,
Billing funcionando, OpenAI real ou fluxo completo de cadastro/confirmacao.
Email/URL Configuration no Supabase e E2E real/offline, dois testes remotos e
Docker build/up continuam gates. UI de troca de plano/prorata permanece futura.

APK debug atualizado nesta etapa: build PASS, package
br.com.equilibrafit.app.plusplus.dev, versao 1.0.0+1, target SDK 36,
branding EquilibraFit++ e permissao com.android.vending.BILLING conferidos.
apksigner verify PASS (v2); isso e assinatura DEBUG, nao chave de publicacao.
Kernel contem a API HTTPS publicada e a guarda billing.invalid_response;
nao contem http://10.0.2.2:5158 nem http://localhost:5158.
SHA256: FBA0EFDCAD3379B7331B076F38313AD766F7A795E813CA952A79AE5000D78CB4.
Arquivo: src/mobile/equilibrafit_plusplus_app/build/app/outputs/flutter-apk/app-debug.apk.
Nao instalado nesta etapa, sem limpar/reiniciar emulador ou dados do app.
Aviso preexistente de migracao futura Built-in Kotlin permanece. Este APK
nao comprova compra Play do package de release, nem substitui APK/AAB assinado.
