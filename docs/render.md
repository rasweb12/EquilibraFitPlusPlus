# Render

O Blueprint novo `render.yaml` define tres web services Docker em main:
equilibrafit-plusplus-api, equilibrafit-plusplus-admin e equilibrafit-plusplus-ai.
Nao cria banco, disco SQLite, Azure ou Redis obrigatorio.

## URLs Publicadas

| Servico | URL HTTPS |
| --- | --- |
| API | https://equilibrafit-plusplus-api-4lkw.onrender.com |
| AI | https://equilibrafit-plusplus-ai-4lkw.onrender.com |
| Admin | https://equilibrafit-plusplus-admin-4lkw.onrender.com/login |
| Supabase | https://knaubynyytqnyyrdrkfi.supabase.co |

O sufixo -4lkw faz parte dos hostnames publicados; o nome previsto sem esse
sufixo nao deve ser usado como URL do app. SUPABASE_URL nao e a URL da API .NET.

No painel Render, conferir os valores efetivos:

```text
# API
SUPABASE_URL=https://knaubynyytqnyyrdrkfi.supabase.co
AiCoach__BaseUrl=https://equilibrafit-plusplus-ai-4lkw.onrender.com

# Admin
AdminApi__BaseUrl=https://equilibrafit-plusplus-api-4lkw.onrender.com
```

AiCoach__ApiKey deve ser igual a EQUILIBRAFIT_AI_API_KEY do servico AI; configurar
como segredo, nunca no Flutter. O Blueprint ja referencia os servicos corretos
por fromService/RENDER_EXTERNAL_URL, sem depender do sufixo atribuido pelo Render.
[Variaveis oficiais do Render](https://render.com/docs/environment-variables).

appsettings.Production.json fornece as URLs publicadas para API -> AI e
Admin -> API. Variaveis de ambiente continuam sobrescrevendo esses valores:
remover/corrigir overrides localhost ou 10.0.2.2 no Render. .env.example e para
desenvolvimento local e nao deve ser importado integralmente no ambiente remoto.
localhost/127.0.0.1 dos health checks Docker e 0.0.0.0 de bind nao sao destinos
remotos do aplicativo e permanecem. Compose usa DNS interno dos containers.

Validacao HTTPS em 2026-10-07: API /health e /health/ready 200, AI /health 200,
Admin /health e /login 200, com branding EquilibraFit++ Admin. Sem credenciais,
API /api/v1/admin/dashboard e AI /api/v1/coach/chat retornaram 401.
Supabase confirmou projeto EquilibraFit++ ACTIVE_HEALTHY e a URL acima.
Esses checks nao comprovam os valores efetivos das variaveis no painel Render,
login completo, Admin -> API autenticado ou API -> AI com a chave interna.

Para Android debug, no diretorio Flutter:

```powershell
flutter build apk --debug --dart-define=API_BASE_URL=https://equilibrafit-plusplus-api-4lkw.onrender.com
adb install -r build/app/outputs/flutter-apk/app-debug.apk
```

Sem dart-define, debug usa a API publicada. Desenvolvimento local continua
possivel com --dart-define=API_BASE_URL=http://10.0.2.2:5158 no emulador.
Nenhum IP local e necessario no aparelho para usar a API publicada.

## Deploy

Depois da revisao e autorizacao explicita de commit/push:

```powershell
git status
git diff --check
git ls-files
# Nao executar commit/push sem autorizacao.
# Nenhum commit/push e feito automaticamente nesta verificacao.
```

O remoto atual e rasweb12/EquilibraFitPlusPlus; conecte somente esse projeto
independente ao Blueprint no painel Render. Revise nomes, custos e secrets.
Nao reutilize o Blueprint/repo/banco original.
[Referencia oficial](https://render.com/docs/blueprint-spec).

Preencha secrets da API SUPABASE_DB_CONNECTION_STRING, SUPABASE_URL e
SUPABASE_PUBLISHABLE_KEY. API recebe AI URL e chave interna por fromService;
Admin recebe API URL por fromService/RENDER_EXTERNAL_URL.
AI gera sua chave interna; configure EQUILIBRAFIT_AI_OPENAI_API_KEY somente
no servico AI. Sem OpenAI, fallback seguro continua disponivel.

## Runtime

API em Production exige PostgreSQL. RUN_DB_MIGRATIONS=true executa efbundle
antes de iniciar; role do banco precisa criar schema/tabelas/policies.
Falha em migration deve interromper startup. Nao execute migrations
concorrentemente em varios deploys sem coordenacao operacional.
Considere migration em job dedicado quando houver multiplas replicas.

Render termina HTTPS e encaminha requests ao container.
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true esta destinado somente a esse proxy
confiavel; nao abra Kestrel diretamente a clientes que possam forjar headers.
Para outra hospedagem configure KnownProxies/KnownNetworks explicitamente.

Health API /health e /health/ready, Admin /health, AI /health.
Ready testa conexao/banco com todas as migrations aplicadas; health AI nao
certifica acesso OpenAI. Swagger API e docs AI sao desligados em producao.

## Billing E Bootstrap

Billing fica desabilitado ate configurar Play Console, credenciais ADC em secret
file no Render, chave AES, produtos e RTDN. Veja google-play-billing.md.
Bootstrap administrativo fica desligado; habilite temporariamente com UUID
confirmado, confira auditoria e desabilite novamente.

## Limites

Plano free e escolha inicial de testes, nao garantia de disponibilidade:
cold start pode exceder timeouts API/AI e interromper worker de reconciliacao.
Valide plano always-on antes de confiar em renovacoes ou atendimento continuo.
A primeira resposta observada da API publicada levou 57 segundos; respostas
seguintes foram rapidas. Isso e compativel com cold start, mas nao identifica
sozinho a causa. O timeout normal do mobile continua 20 segundos. Apos um
receiveTimeout de cadastro comprovado no emulador, cadastro/login passaram a
aguardar resposta por ate 90 segundos; conexao continua com limite de 20 segundos.
Logout e demais requests mantiveram seus limites. Isso acomoda cold starts
comuns, mas nao garante disponibilidade nem corrige falhas de SMTP/Auth.
Nao fazer retry automatico de
cadastro/refresh. [Limites do plano free](https://render.com/docs/free).

## Aceitacao

Apos deploy teste health/ready, cadastro+confirmacao, login+refresh, onboarding,
treino+sessao, alimentacao, evolucao, Coach, Admin, LGPD, logout+novo login.
Com duas contas, prove isolamento e roles. Instale build Android apontando para
a URL HTTPS atribuida usando apenas API_BASE_URL.
