# Auditoria tecnica do EquilibraFit++
Data: 2026-10-09. Repositorio: rasweb12/EquilibraFitPlusPlus. Branch: main.
Base auditada: cff44d0c78ff5153fe3f5de71a4d6ff77bd2adc3 (IA4.1).
Diretorio confirmado: D:/Curso/APP/EquilibraFit++. Estado inicial: limpo.

> Snapshot da primeira etapa da auditoria. Para correcoes posteriores do incidente 502, testes finais (312 .NET / 65 Python / 60 Flutter), evidencias do Render, fechamento dos filtros/licenca e hash do APK Debug mais recente, consultar [relatorio do incidente](D:/Curso/APP/EquilibraFit++/docs/audit/2026-10-09-incidente-ai-502.md), especialmente a secao 9. O APK Debug foi reconstruido; o hash registrado abaixo pertence ao build anterior.

## A. Resumo executivo

**Nao aprovado para lancamento em producao/Google Play nesta auditoria.**
Foram inspecionados os componentes principais, executadas as suites locais e corrigidos defeitos confirmados.
O codigo local passou em 61 testes Python, 235 testes .NET e 56 testes Flutter.
Dois testes de banco remoto ficaram explicitamente ignorados por falta de projetos de teste dedicados.

A API, IA e Admin publicados responderam aos checks descritos abaixo; isso nao comprova seus fluxos completos.
A primeira resposta da API levou 56,080 s, incompativel com o timeout comum de 20 s do mobile.
Ha pendencias P1 de idempotencia concorrente, exportacao completa de dados e homologacao de Release/Google Play.
As correcoes desta auditoria sao locais: nenhum commit, push, PR, deploy, compra, migration remota ou alteracao de secrets foi feito.
O EquilibraFit original nao foi alterado nem usado como destino de comandos.

A revisao de seguranca foi priorizada e sequencial, sem revisores independentes e com cobertura parcial.
Inventariar 696 arquivos nao significa ler integralmente todos eles. Nao se certifica ausencia absoluta de vulnerabilidades.
O scanner informou indisponibilidade de resultados protegidos Daybreak. O artefato selado refere-se ao commit de base, antes das correcoes locais:
[Relatorio de seguranca](C:/Users/RASS/.codex/state/plugins/codex-security/scans/EquilibraFit/cff44d0c78ff5153fe3f5de71a4d6ff77bd2adc3_20261009T160434Z_mbt0mwz1/report.md).
Medicao retornada pelo scanner: 2.467.976 tokens totais; 2.456.844 de entrada; 2.326.912 de entrada em cache. Sao metadados da ferramenta, nao uma estimativa de cobranca.

## B. Inventario tecnico

| Camada | Implementacao encontrada | Integracoes |
|---|---|---|
| Mobile | Flutter/Dart, Riverpod, GoRouter, Dio, sqflite, flutter_secure_storage, in_app_purchase | API HTTPS; SQLite local; Play Billing |
| Backend | .NET 10, Clean Architecture, MediatR, FluentValidation, EF Core | Supabase Auth/JWKS, PostgreSQL, AI HTTP, Google Play Developer API |
| Persistencia | Npgsql EF Core 10.0.1; EF Core SQLite 10.0.5; schema app | Supabase PostgreSQL 17; SQLite apenas development/testes |
| IA | Python 3.13, FastAPI, Pydantic, OpenAI Responses, google-genai | OpenAI/Gemini; conhecimento local; fallback deterministico; YOLO opcional |
| Admin | Blazor Interactive Server, MudBlazor, sessao no servidor | API, sem service_role no navegador |
| Infra | Tres Dockerfiles, entrypoint com EF bundle, Compose, Render Blueprint | Supabase remoto; Redis opcional |
| Billing | IPaymentProvider, GooglePlayBillingProvider, token cifrado/hash, RTDN e reconciliation worker | Verificacao/acknowledgement no servidor |
| Testes | Seis projetos .NET, pytest, Flutter test | Fixtures SQLite, JWT assinado de teste, mocks HTTP/SDK |

Fluxo atual de autenticacao: Flutter -> API -> Supabase Auth. Nao existe acesso generalizado do Flutter ao banco.
Fluxos de negocio: Flutter/Admin -> API -> Supabase PostgreSQL; API -> FastAPI -> provedor de IA.
Offline: SQLite -> Outbox/Pending AI -> Sync Engine -> API. A API continua responsavel por usuario/tenant/autorizacao.
Nenhuma dependencia operacional SQL Server, Azure SQL, Stripe ou Hangfire.SqlServer foi encontrada em src/infra de execucao revisados.
A unica ocorrencia SqlServer na busca de src/tests/config de execucao foi uma assercao negativa no teste de migration.
Storage de arquivos, migracao de dados do produto antigo e publicacao na Play nao foram realizados.

## C. Matriz de auditoria

Status: OK = comprovado no escopo indicado; Atencao = pendencia relevante; Critico = impede o alvo indicado; Nao verificado = sem evidencia suficiente.

| Componente | Status | Evidencia / arquivo e linha | Impacto | Acao |
|---|---|---|---|---|
| Planos IA | OK local | [PlanService:12][plan], baseline falhava; agora contrato Gemini simulado passa | Antes: AttributeError em toda geracao | Corrigido; homologar provedor real |
| Roteamento/modelo/fallback | OK local | [Router:14][router], [Settings:180][settings]; testes de seis features e concorrencia | Metadados por chamada; sem escolha implicita de outro provedor | Manter fallback false; configurar env por feature |
| Coach/treinos OpenAI | Atencao | [Coach:28][coach], [Workout:64][workout], [OpenAI:24][openai] | Caminhos de sucesso/falha simulados; nao comprova conta/modelo remoto | Smoke autorizado com contas de teste |
| Refeicoes/rotulos Gemini | Atencao | [Meals:105][meals], [Labels:87][labels], [Gemini:99][gemini] | MIME, JSON, revisao e metadados testados; precisao real nao certificada | Homologar fotos e rotulos controlados |
| Seguranca nutricional | OK local | [Meals:120][meals-review], [Schemas:25][meal-schema] | YOLO nao gera macros ficticios; texto desconhecido nao vira refeicao arbitraria | Manter revisao humana, nao chamar estimativas de medicoes |
| Auth/JWT/roles | Atencao | [SupabaseAuthentication:24][auth], [AuthorizationTests:28][auth-tests] | JWT/roles/tenant/revogacao e tentativa de claims forjados cobertos por mocks | Completar email/recuperacao/login reais |
| Rate limiting | OK local | [AuthRateLimitPolicy:12][limiter], [Program:83][program] | Bucket global substituido por quota por endereco resolvido | Validar cadeia de proxies confiaveis no Render |
| Logs API-IA | OK local | [AiCoachHttpClient:312][ai-http-log] | Erros 422 nao sao mais registrados com corpo contendo inputs | Teste de nao vazamento adicionado |
| Contrato de rotulos | OK local | [Handler:51][label-handler], [HTTP client:152][ai-http-label] | Contexto nao substitui OCR/imagem | Campo interno opcional label_context; API publica preservada |
| Erro JSON/listas IA | OK local | [HTTP client:79][ai-http] | Resposta malformada nao causa Select sobre null | Rejeicao segura testada |
| PostgreSQL/RLS | Atencao | Metadados remotos: 39/39 tabelas app e history com RLS; [RLS tests:23][rls-tests] | Habilitacao comprovada; isolamento real de dois sujeitos nao certificado | Rodar testes em Supabase separado |
| Migrations | Atencao | Sete IDs no banco publicado; [MigrationTests:42][migration-tests] | Script PostgreSQL gerado; nao testado banco remoto vazio nesta execucao | Aplicar/reaplicar em projeto vazio de teste |
| Outbox/cache/restart | OK local parcial | [OfflineIsolation:16][offline-tests], [LocalDatabase:16][local-db] | Persistencia real SQLite/reabertura e troca de usuario cobertas | Testar processo Android encerrado e sync HTTP real |
| Idempotencia | Atencao P1 | [Middleware:85][idempotency], [Food handler:63][food-handler] | Consulta antes da mutacao, registro depois; janela concorrente | Reserva atomica e teste paralelo entre instancias |
| Exportacao LGPD | Atencao P1 | [LgpdRepository:39][lgpd] | Exporta perfil/consentimentos, mas historicos so como contagens | Definir/implementar exportacao completa com isolamento |
| Mobile URL/tokens | OK no codigo | [AppConfig:12][app-config], [SecureTokenStore:35][tokens] | URL unica HTTPS, tokens fora do SQLite, guards de owner no HTTP | Homologar refresh/logout/deep link no dispositivo |
| Telas/navegacao | Nao verificado integralmente | [widget_test.dart:3][widget-test] verifica somente string | 56 testes nao sao teste visual/de fluxo completo | Criar integration_test de login/onboarding/dashboard/offline |
| Admin | Atencao | [Admin Program:16][admin], testes de sessao aprovados | Compila; health nao prova permissoes ou operacao completa | Homologacao autenticada, auditoria e HTTPS de configuracao |
| Render | Atencao P1 | [render.yaml:10][render], [AppConfig:24][app-timeout]; primeira resposta 56 s | Timeout normal mobile 20 s; API/IA podem inicializar em momentos diferentes | Medir ponta a ponta e decidir disponibilidade sem ativar custos aqui |
| Docker | Nao verificado por execucao | [entrypoint:5][entrypoint], [Compose:17][compose] | Sem CLI/daemon Docker nesta maquina | Validar config/build/up isoladamente, migrations desligadas durante smoke |
| Assinatura Release/AAB | Critico para publicacao | [Gradle:35][gradle], [Release script:15][release-script] | key.properties e env de assinatura ausentes; preflight bloqueou | Configurar chave propria fora do Git; nao reutilizar original |
| Play Billing | Atencao P1 | [Provider:30][billing], [Flutter verifier:63][billing-mobile] | Mocks cobrem verificacao/restore; sem compra real ou RTDN homologado | Track interno e license testers |
| Secrets | Atencao / evidencia limitada | .gitignore/.dockerignore e busca literal no Git atual | Nenhum padrao de chave privada/provedor encontrado; historico/logs remotos nao certificados | Scanner de historico e revisao de secrets externos sem exibir valores |

## D. Problemas encontrados

### P0
Nenhum P0 confirmado nas superficies revisadas. Isso nao equivale a prova de inexistencia.

### P1 antes do lancamento
1. **PlanService quebrado**: inicializava _openai e chamava _provider. Reproduzido no baseline; corrigido.
2. **Estimativas nutricionais ficticias**: YOLO atribuia sempre 100 g/180 kcal/macros fixos; fallback de texto desconhecido atribuia 350 kcal. Corrigido: classificacao continua em mensagem para revisao, sem nutrientes inventados. Conhecidos por regras continuam explicitamente estimativas.
3. **Rotulo com contexto ignorava imagem**: o backend enviava Contexto como extracted_text. Corrigido com label_context opcional e OCR separado.
4. **Idempotencia nao atomica**: duas primeiras requisicoes com mesma chave podem atravessar a consulta antes de haver resposta armazenada e executar a mutacao duas vezes. O handler de alimentacao cria novo ID sem operation ID unico. O indice unico de IdempotencyRecords protege o registro posterior, nao a mutacao anterior. A expiracao de 24 h nao remove o registro unico antigo; o comportamento depois dela tambem precisa de teste. Confirmacao por fluxo de fonte, sem experimento concorrente remoto. Nao foi introduzido lock local que finja protecao multi-instancia. Requer transacao/reserva e testes dedicados.
5. **Exportacao incompleta de dados**: contagens nao contem o historico exportavel de alimentacao, treinos, planos ou conversas. Avaliacao tecnica, nao parecer de conformidade juridica. Nao alterada por exigir contrato/escopo e testes de dados.
6. **Disponibilidade/timeout**: 56,080 s na primeira chamada API e 22,829 s na primeira IA; normal mobile = 20 s, AI mobile/API = 75 s, provider AI = 60 s. Soma de inicializacoes e processamento nao tem orcamento ponta a ponta validado. A causa de cold start e uma inferencia, nao foi confirmada em logs.
7. **Publicacao sem assinatura e homologacao**: nao ha APK Release/AAB, credencial de assinatura local, compra de teste ou validacao de renovacao/cancelamento/RTDN real.

### P2
- Bucket unico de autenticacao permitia dez requests de um cliente bloquearem todos na instancia por um minuto. Corrigido e testado; proxy real ainda deve resolver endereco confiavel.
- Coach reportava modelo fornecido pelo cliente/config OpenAI, ignorando provedor e fallback efetivos. Corrigido; o modelo informado e o alias enviado ao SDK, nao um snapshot remoto comprovado por chamada paga.
- Metadados mutaveis last_model/last_provider/fallback_used foram eliminados. Rotas atuais criavam service/router por request, reduzindo o alcance da corrida anterior; teste agora cobre uso compartilhado concorrente.
- Fallback igual ao principal escolhia automaticamente o outro provedor. Corrigido para obedecer Settings. Fallback automatico permanece desabilitado.
- OpenAI nao passava limite de output e SDK podia retry. Limite configurado e max_retries=0 adicionados; deadline total no router. Erros SDK que retornam None sao classificados genericamente como invalid_response, nao como diagnostico preciso de causa.
- Compose e .env.example nao continham Gemini/roteamento; pyproject nao declarava google-genai apesar de requirements. Corrigido, sem mudar secrets.
- HTTP client .NET registrava corpo de erros de IA; listas null podiam disparar excecao. Corrigidos, com testes.
- Dependency bounds Python sem lock permitem builds diferentes. SDKs sao instanciados por request, sem ciclo de fechamento explicito; avaliar lifespan/cleanup e carga antes de escalar.
- Exportacao de resposta idempotente acima de 20.000 caracteres armazena corpo vazio; replay pode retornar sucesso sem JSON. Middleware de replay antecede autorizacao; revisar rechecagem de politica. Sem exploracao remota confirmada.
- Android nao explicita politica de backup para SQLite de saude/secure storage; revisar backup, restauracao e retencao por usuario.
- Supabase advisor mostrou leaked-password protection desativada. Recurso depende do plano; nao foi habilitado nem contratado. [Documentacao de seguranca de senhas](https://supabase.com/docs/guides/auth/password-security#password-strength-and-leaked-password-protection).
- Advisor informou 18 tabelas RLS sem policy (INFO). A maioria e deliberadamente restrita ao backend; nao criar policy permissiva para eliminar aviso. [Remediacao do advisor](https://supabase.com/docs/guides/database/database-linter?lint=0008_rls_enabled_no_policy).
- Build .NET apresentou 240 warnings, predominantemente CS1591. Build Android avisou sobre futura migracao Kotlin; nao atualizar AGP/Kotlin nesta auditoria.

### P3
Capturar versao resolvida de modelos, consolidar telemetria por feature/custo sem conteudo pessoal e realizar testes de carga/longevidade dos SDKs.
Storage, Google OAuth e outros provedores sao evolucoes futuras, nao requisito implementado nesta auditoria.

## E. Correcoes e verificacao

### Arquivos criados
- [image_input.py](D:/Curso/APP/EquilibraFit++/src/ai/equilibrafit_plusplus_ai/app/providers/image_input.py): validacao comum de base64/MIME/tamanho.
- [conftest.py](D:/Curso/APP/EquilibraFit++/src/ai/equilibrafit_plusplus_ai/tests/conftest.py): isolamento de credenciais nos testes.
- [test_ai_provider_router.py](D:/Curso/APP/EquilibraFit++/src/ai/equilibrafit_plusplus_ai/tests/test_ai_provider_router.py): roteamento, fallback, deadline, concorrencia, cancelamento, JSON, SDKs.
- [test_ai_service_contracts.py](D:/Curso/APP/EquilibraFit++/src/ai/equilibrafit_plusplus_ai/tests/test_ai_service_contracts.py): contratos e seguranca nutricional.
- [AuthRateLimitPolicy.cs][limiter]: quota por cliente.
- [AuthRateLimitPolicyTests.cs](D:/Curso/APP/EquilibraFit++/tests/EquilibraFitPlusPlus.Api.IntegrationTests/AuthRateLimitPolicyTests.cs): isolamento de quotas/headers.
- [AiCoachHttpContractTests.cs](D:/Curso/APP/EquilibraFit++/tests/EquilibraFitPlusPlus.Infrastructure.IntegrationTests/AiCoach/AiCoachHttpContractTests.cs): contexto/JSON/logs.
- Este relatorio.

### Arquivos adaptados
- Python: config.py, main.py (lint de imports), ai_provider_router.py, openai_provider.py, gemini_provider.py.
- Python schemas: common.py, meals.py, labels.py, plans.py; valores nao finitos/calorias invalidas/limites invertidos rejeitados.
- Python services: coach_service.py, workout_service.py, plan_service.py, meal_recognition_service.py, label_service.py.
- Python pyproject.toml: google-genai declarado tambem nesta forma de instalacao.
- .NET: Program.cs, IAiCoachClient.cs, ReconhecerRotuloImagemCommandHandler.cs, AiCoachHttpClient.cs.
- Infra: docker-compose.yml e .env.example; Gemini e roteamento alinhados ao Render.
- Nenhum arquivo removido, migration criada/alterada ou dependencia .NET/Flutter trocada.
- google-genai ja estava em requirements; foi adicionado a pyproject e instalado no venv ignorado. pip tambem foi preparado nesse venv.
- As mudancas sao restritas ao novo projeto e nao foram publicadas.

### Testes executados

| Comando | Resultado | Limite |
|---|---|---|
| Python pytest baseline | 25 passaram / 1 falhou | Reproduziu defeito PlanService |
| python -m compileall -q app | Exit 0 | Sintaxe |
| python -m ruff check app tests | All checks passed | Lint |
| python -m pytest -q | 61 passaram | Provedores pagos mockados; keys removidas apenas do processo de teste |
| dotnet restore EquilibraFitPlusPlus.sln | Exit 0 | Dependencias restauradas |
| dotnet build -c Release --no-restore | Exit 0, 0 erros, 240 warnings | Inclui Admin e projetos de teste |
| dotnet test -c Release --no-build --no-restore | 235 passaram / 2 ignorados / 0 falhas | 3 Domain, 66 Application, 85 Infrastructure, 62 API, 14 Architecture, 5 Admin |
| dotnet list package --vulnerable --include-transitive --no-restore | Nenhum vulneravel nas fontes NuGet consultadas | Nao certifica Python/Dart ou vulnerabilidades desconhecidas |
| flutter pub get | Exit 0 | Lock nao alterado; avisos de novas versoes nao motivaram upgrade |
| flutter analyze | Sem problemas | Analise estatica |
| flutter test --reporter expanded | 56 passaram | Inclusive SQLite reaberto, outbox/owner, auth e billing com mocks |
| flutter build apk --debug --dart-define=API_BASE_URL=... | Exit 0 | APK Debug; sem instalar/publicar |
| aapt2 dump badging | ID .plusplus.dev, label EquilibraFit++, version 1.0.0+1, target SDK 36 | Metadata do artefato |
| apksigner verify --verbose | Verifies; assinatura v2; um signer | Apenas assinatura Debug existente |
| Build-AndroidRelease.ps1 | Bloqueio esperado antes do build | Ausencia de key.properties/ANDROID_KEYSTORE_PATH; nao gerou APK/AAB Release |
| git diff --check | Exit 0 | Sem erros de whitespace |
| Busca literal de secrets no Git atual | Nenhum padrao de chave privada/provedor encontrado | Heuristica limitada; historico e logs remotos nao certificados |

Os testes PostgreSQL vazio/reaplicacao/soft-delete e Supabase role A/B/escrita/client-billing nao foram executados: exigem bancos novos dedicados.
Nao apontar TEST_POSTGRES_CONNECTION_STRING nem TEST_SUPABASE_DB_CONNECTION_STRING para a Beta; esses testes executam migrations.
Docker compose config/build/up nao foi executado: Docker CLI/Desktop/daemon ausentes. Nao houve substituicao por banco local em Docker.
Nao houve chamada paga real OpenAI/Gemini, envio de email, alteracao de senha, compra ou escrita em dados publicados.
A consulta remota inicial a history usou coluna version e falhou de forma nao destrutiva; a consulta corrigida a MigrationId confirmou os sete IDs.

### Checks publicados, somente leitura/negacao de acesso

| Check | HTTP | Duracao |
|---|---|---|
| API /health | 200 | 56,080 s |
| API /health/ready | 200 | 1,021 s |
| API /api/v1/premium/status sem token | 401 | 0,635 s |
| AI /health | 200 | 22,829 s |
| AI /api/v1/plans/generate sem chave, corpo vazio | 401 | 0,296 s; sem geracao |
| Admin /health | 200 | 12,796 s |

Supabase EquilibraFit++ (knaubynyytqnyyrdrkfi), nao Vero: ACTIVE_HEALTHY, PostgreSQL 17.
Consultas de catalogo confirmaram 39/39 tabelas app e 1/1 public com RLS. Nao foram lidas linhas de usuarios.
History: InitialPostgreSql, SupabaseAuthProfiles, SupabaseRowLevelSecurity, GooglePlayBilling, ProtectBillingData, GlobalSessionRevocation, ProtectMigrationHistory.
Isso nao substitui testes reais das policies e dos fluxos.

### APK
[APK Debug](D:/Curso/APP/EquilibraFit++/src/mobile/equilibrafit_plusplus_app/build/app/outputs/flutter-apk/app-debug.apk).
SHA256: 6D025B9E9E0BCE696B78E604F2A8F8B27AB6F0CFE3C5D238D8980B75664A6CCE.
Nao instalado nesta auditoria. APK Release e AAB inexistentes; assinatura Release nao verificada.
Namespace Kotlin antigo nao e applicationId; IDs de instalacao Debug/Release sao independentes do original.
Nenhuma chave privada foi criada/substituida/versionada; utilizou-se apenas a assinatura Debug padrao ja existente.

## F. Prontidao por alvo

| Alvo | Veredito | Pendencia de aceite |
|---|---|---|
| API .NET | Atencao | Fonte/tests locais OK; idempotencia, exportacao e timeout; deploy das correcoes ainda nao feito |
| Python | Atencao | Suites OK; integrar adapter de rotulo com API no mesmo ciclo de deploy e homologar SDKs reais |
| OpenAI | Atencao | Coach OK relatado pelo usuario; geracao de treinos real e limites/modelo ainda nao homologados aqui |
| Gemini | Atencao | Refeicoes OK relatado; plano, imagem e rotulo reais nao certificados; mocks OK |
| Supabase | Atencao | Metadata/RLS/history OK; banco vazio e role A/B reais pendentes |
| Admin | Atencao | Build/tests/health OK; operacoes autenticadas e permissoes ponta a ponta pendentes |
| Flutter Debug | OK para testes controlados | APK gerado/verificado; testes locais OK; nao validacao visual/E2E de todas as telas |
| Flutter Release | Bloqueado | Assinatura propria fora do Git, build e testes no dispositivo |
| Google Play | Bloqueado | AAB assinado, track interno, license testers, RTDN, politica de privacidade/Data Safety e fluxo de exclusao revisados |

Configuracao esperada da IA permanece: coach/workouts OpenAI, plans/meals/meal_text/labels Gemini; modelos gpt-4.1-mini e gemini-2.5-flash; timeout 60 s; fallback false.
Secrets permanecem apenas em ambiente: AI_INTERNAL_API_KEY/EQUILIBRAFIT_AI_API_KEY, OPENAI_API_KEY e GEMINI_API_KEY prefixadas corretamente.
sync:false no Blueprint nao comprova valor/aplicacao no painel. Render env efetivo, logs, ultimo commit implantado e Google Play Console nao foram acessados.
O banco permanece Supabase remoto, Redis opcional, sem SQLite definitivo no Render.
Mesmo que health seja 200, uma geracao pode voltar fallback por chave ausente. Verificar model/fallback_used na homologacao.

## G. Sprints de fechamento

1. **Integridade e dados**: reserva atomica de idempotencia vinculada ao payload; teste de duas requisicoes simultaneas em duas instancias com um unico efeito; replay expirado/grande e crash apos commit; exportacao completa de um usuario sem dados de B.
2. **Homologacao isolada**: banco Supabase de teste vazio -> sete migrations -> reaplicacao sem perda -> soft delete unico -> A/B/RLS SELECT/INSERT/UPDATE/DELETE; testes de API tenant/roles; nao usar dados reais.
3. **Autenticacao e IA E2E**: confirmar cadastro/email/deep links/recuperacao/logout/refresh/revogacao com contas de teste; executar seis features com autorizacao para chamadas reais e conferir modelo, fallback, JSON, persistencia e revisao humana. Aceite: nenhuma resposta tratada como medicao clinica.
4. **Disponibilidade e observabilidade**: medir sessao apos ociosidade e indisponibilidade de cada servico; budget ponta a ponta; garantir sync sem duplicacao e retentativas seguras; traces sem secrets/PII. Escolha de plano/worker externo somente com aprovacao de custo.
5. **Release e Play**: responsavel fornece/configura assinatura propria em arquivo ignorado; executar script de Release/APK/AAB; testar dispositivo e restore; track interno com renovacao/cancelamento/hold/expiracao/revogacao/RTDN; nunca publicar automaticamente.
6. **Infra reproduzivel**: Docker config/build/up, RUN_DB_MIGRATIONS=false no smoke que usa banco publicado; migration bundle apenas em banco de teste; auditar env real do Render sem mostrar valores; lock Python e migracao Kotlin separados e testados.

### Comandos de verificacao (PowerShell, raiz do novo projeto)
```powershell
dotnet restore EquilibraFitPlusPlus.sln
dotnet build EquilibraFitPlusPlus.sln -c Release
# Para suites locais: mantenha os dois TEST_* de banco vazios.
dotnet test EquilibraFitPlusPlus.sln -c Release
& ./scripts/Build-AndroidRelease.ps1 -FlutterCommand C:/develop/flutter/bin/flutter.bat
# Quando houver Docker e ambiente isolado configurado:
docker compose config --quiet
docker compose build
```
Para deploy: revisar estas mudancas, obter autorizacao para commit/push e implantar API/AI em conjunto; verificar env no painel e homologar antes de promover. Nenhuma publicacao foi executada.

### Referencias oficiais consultadas
- [OpenAI Responses: max_output_tokens](https://developers.openai.com/api/reference/python/resources/responses/methods/create): limite adicionado; nao houve troca de modelo.
- [Supabase Flutter](https://supabase.com/docs/guides/getting-started/quickstarts/flutter) e [RLS](https://supabase.com/docs/guides/database/postgres/row-level-security): nao exigem conectar todas as tabelas diretamente no app.
- [Render Free](https://render.com/docs/free): idle/cold start e filesystem efemero; Blueprint free nao certifica operacao continua de worker.
- [Play subscriptions](https://developer.android.com/google/play/billing/subscriptions) e [app signing](https://developer.android.com/studio/publish/app-signing): homologacao/assinatura sao gates distintos de compilar.
- [Flutter Kotlin migration](https://docs.flutter.dev/release/breaking-changes/migrate-to-built-in-kotlin/for-app-developers): aviso do build, evolucao separada.

[router]: D:/Curso/APP/EquilibraFit++/src/ai/equilibrafit_plusplus_ai/app/providers/ai_provider_router.py:14
[settings]: D:/Curso/APP/EquilibraFit++/src/ai/equilibrafit_plusplus_ai/app/core/config.py:180
[plan]: D:/Curso/APP/EquilibraFit++/src/ai/equilibrafit_plusplus_ai/app/services/plan_service.py:12
[coach]: D:/Curso/APP/EquilibraFit++/src/ai/equilibrafit_plusplus_ai/app/services/coach_service.py:28
[workout]: D:/Curso/APP/EquilibraFit++/src/ai/equilibrafit_plusplus_ai/app/services/workout_service.py:64
[openai]: D:/Curso/APP/EquilibraFit++/src/ai/equilibrafit_plusplus_ai/app/providers/openai_provider.py:24
[gemini]: D:/Curso/APP/EquilibraFit++/src/ai/equilibrafit_plusplus_ai/app/providers/gemini_provider.py:99
[meals]: D:/Curso/APP/EquilibraFit++/src/ai/equilibrafit_plusplus_ai/app/services/meal_recognition_service.py:105
[meals-review]: D:/Curso/APP/EquilibraFit++/src/ai/equilibrafit_plusplus_ai/app/services/meal_recognition_service.py:120
[labels]: D:/Curso/APP/EquilibraFit++/src/ai/equilibrafit_plusplus_ai/app/services/label_service.py:87
[meal-schema]: D:/Curso/APP/EquilibraFit++/src/ai/equilibrafit_plusplus_ai/app/schemas/meals.py:25
[auth]: D:/Curso/APP/EquilibraFit++/src/backend/EquilibraFitPlusPlus.Infrastructure/Authentication/SupabaseAuthentication.cs:24
[auth-tests]: D:/Curso/APP/EquilibraFit++/tests/EquilibraFitPlusPlus.Api.IntegrationTests/SupabaseAuthorizationTests.cs:28
[limiter]: D:/Curso/APP/EquilibraFit++/src/backend/EquilibraFitPlusPlus.Api/Authentication/AuthRateLimitPolicy.cs:12
[program]: D:/Curso/APP/EquilibraFit++/src/backend/EquilibraFitPlusPlus.Api/Program.cs:83
[ai-http-log]: D:/Curso/APP/EquilibraFit++/src/backend/EquilibraFitPlusPlus.Infrastructure/AiCoach/AiCoachHttpClient.cs:312
[ai-http-label]: D:/Curso/APP/EquilibraFit++/src/backend/EquilibraFitPlusPlus.Infrastructure/AiCoach/AiCoachHttpClient.cs:152
[ai-http]: D:/Curso/APP/EquilibraFit++/src/backend/EquilibraFitPlusPlus.Infrastructure/AiCoach/AiCoachHttpClient.cs:79
[label-handler]: D:/Curso/APP/EquilibraFit++/src/backend/EquilibraFitPlusPlus.Application/Features/Alimentacao/Commands/ReconhecerRotuloImagem/ReconhecerRotuloImagemCommandHandler.cs:51
[rls-tests]: D:/Curso/APP/EquilibraFit++/tests/EquilibraFitPlusPlus.Infrastructure.IntegrationTests/Data/SupabaseRlsTests.cs:23
[migration-tests]: D:/Curso/APP/EquilibraFit++/tests/EquilibraFitPlusPlus.Infrastructure.IntegrationTests/Data/PostgreSqlMigrationTests.cs:42
[offline-tests]: D:/Curso/APP/EquilibraFit++/src/mobile/equilibrafit_plusplus_app/test/core/sync/offline_isolation_test.dart:16
[local-db]: D:/Curso/APP/EquilibraFit++/src/mobile/equilibrafit_plusplus_app/lib/core/storage/local_database.dart:16
[idempotency]: D:/Curso/APP/EquilibraFit++/src/backend/EquilibraFitPlusPlus.Api/Middleware/IdempotencyMiddleware.cs:85
[food-handler]: D:/Curso/APP/EquilibraFit++/src/backend/EquilibraFitPlusPlus.Application/Features/Alimentacao/Commands/CriarRegistroAlimentar/CriarRegistroAlimentarCommandHandler.cs:63
[lgpd]: D:/Curso/APP/EquilibraFit++/src/backend/EquilibraFitPlusPlus.Infrastructure/Repositories/LgpdRepository.cs:39
[app-config]: D:/Curso/APP/EquilibraFit++/src/mobile/equilibrafit_plusplus_app/lib/core/config/app_config.dart:12
[app-timeout]: D:/Curso/APP/EquilibraFit++/src/mobile/equilibrafit_plusplus_app/lib/core/config/app_config.dart:24
[tokens]: D:/Curso/APP/EquilibraFit++/src/mobile/equilibrafit_plusplus_app/lib/core/storage/secure_token_store.dart:35
[widget-test]: D:/Curso/APP/EquilibraFit++/src/mobile/equilibrafit_plusplus_app/test/widget_test.dart:3
[admin]: D:/Curso/APP/EquilibraFit++/src/admin/EquilibraFitPlusPlus.Admin/Program.cs:16
[render]: D:/Curso/APP/EquilibraFit++/render.yaml:10
[entrypoint]: D:/Curso/APP/EquilibraFit++/src/backend/EquilibraFitPlusPlus.Api/entrypoint.sh:5
[compose]: D:/Curso/APP/EquilibraFit++/docker-compose.yml:17
[gradle]: D:/Curso/APP/EquilibraFit++/src/mobile/equilibrafit_plusplus_app/android/app/build.gradle.kts:35
[release-script]: D:/Curso/APP/EquilibraFit++/scripts/Build-AndroidRelease.ps1:15
[billing]: D:/Curso/APP/EquilibraFit++/src/backend/EquilibraFitPlusPlus.Infrastructure/Billing/GooglePlayBillingProvider.cs:30
[billing-mobile]: D:/Curso/APP/EquilibraFit++/src/mobile/equilibrafit_plusplus_app/lib/features/premium/data/google_play_billing.dart:63
