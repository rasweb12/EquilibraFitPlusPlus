# Render

O Blueprint novo `render.yaml` define tres web services Docker em main:
equilibrafit-plusplus-api, equilibrafit-plusplus-admin e equilibrafit-plusplus-ai.
Nao cria banco, disco SQLite, Azure ou Redis obrigatorio.

## Deploy

Depois da revisao e autorizacao explicita de commit/push:

```powershell
git status
git diff --check
git ls-files
# Nao executar commit/push sem autorizacao.
# Mensagem inicial prevista: Initial EquilibraFit++ architecture
```

Conecte exclusivamente rasweb12/EQUILIBRAFIT-PLUSPLUS ao novo Blueprint no painel
Render. Revise nomes, custos e secrets antes de confirmar criacao.
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
Nenhum deploy real foi executado sem conta/repositorio configurados.

## Aceitacao

Apos deploy teste health/ready, cadastro+confirmacao, login+refresh, onboarding,
treino+sessao, alimentacao, evolucao, Coach, Admin, LGPD, logout+novo login.
Com duas contas, prove isolamento e roles. Instale build Android apontando para
a URL HTTPS atribuida usando apenas API_BASE_URL.
