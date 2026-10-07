# EquilibraFit++

Projeto independente derivado tecnicamente do EquilibraFit. A origem
`D:\Curso\APP\EQUILIBRAFIT` permanece intacta; nenhum banco, usuario,
credencial ou historico Git foi importado.

Repositorio pretendido: `rasweb12/EQUILIBRAFIT-PLUSPLUS`.
O repositorio remoto e o primeiro commit ainda dependem de revisao/autorizacao.

## Arquitetura

```text
Flutter --HTTPS--> API .NET 10 --Npgsql--> Supabase PostgreSQL
                        |                    |
                        |              app: dados + RLS
                        +--> Supabase Auth: senhas, JWT, refresh
                        +--> FastAPI AI --> OpenAI
                        +--> Redis opcional

Flutter --> SQLite --> cache / outbox / sync / pending AI
Admin Blazor --> API
Android --> Google Play Billing --> API --> Google Play Developer API
```

Autenticacao usa Supabase Auth por um adapter da API, preservando os contratos
dos clientes maduros. Flutter nao acessa tabelas diretamente e nao recebe
service role, credenciais Google ou chave OpenAI.

## Executar Com Containers

Requer Docker com Compose e um projeto Supabase exclusivo deste aplicativo.
Configure os placeholders de `.env.example` em um `.env` local ignorado,
principalmente URL/chave publica Supabase, connection string e chave interna AI.
Nunca reutilize o banco do aplicativo original.

```powershell
docker compose config --quiet
docker compose up --build
```

API: http://localhost:5158/health e /health/ready.
Admin: http://localhost:8080/login. AI: http://localhost:8001/health.
Migrations PostgreSQL executam antes da API quando `RUN_DB_MIGRATIONS=true`.
Nao existe SQL Server ou PostgreSQL local no Compose.

Redis e opcional: configure `ConnectionStrings__Redis=redis:6379` e use
`docker compose --profile cache up --build`. Sem Redis, cache em memoria.

## Executar Sem Containers

Requer .NET 10, Flutter e Python 3.13. A API nao importa automaticamente
o arquivo `.env`: configure variaveis no processo/IDE/gerenciador de secrets.

```powershell
dotnet restore
dotnet tool restore
dotnet ef database update --project src/backend/EquilibraFitPlusPlus.Infrastructure --startup-project src/backend/EquilibraFitPlusPlus.Api
dotnet run --project src/backend/EquilibraFitPlusPlus.Api --urls http://localhost:5158
dotnet run --project src/admin/EquilibraFitPlusPlus.Admin --urls http://localhost:8080
```

Em outro terminal, dentro de `src/ai/equilibrafit_plusplus_ai`:

```powershell
python -m venv .venv
.venv/Scripts/python -m pip install -r requirements.txt
.venv/Scripts/python -m uvicorn app.main:app --host 127.0.0.1 --port 8001
```

Para prototipo local da API apenas: ambiente Development,
`DATABASE_PROVIDER=Sqlite` e `ConnectionStrings__Default=Data Source=equilibrafit-pp-dev.db`.
SQLite usa EnsureCreated, nunca as migrations PostgreSQL. Supabase Auth continua
necessario para login real. Production rejeita SQLite.

## Flutter

Dentro de `src/mobile/equilibrafit_plusplus_app`:

```powershell
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5158
flutter run --dart-define=API_BASE_URL=https://equilibrafit-plusplus-api.onrender.com
```

O segundo endereco e o nome previsto do Blueprint; confirme a URL atribuida no
Render. Release exige HTTPS publico e applicationId proprio. O ID de debug
`br.com.equilibrafit.app.plusplus.dev` permite coexistir com o aplicativo antigo.
Defina `EQUILIBRAFIT_PLUSPLUS_APPLICATION_ID` para o ID aprovado no Play Console
e configure assinatura propria em `android/key.properties` ignorado.

## Testes

```powershell
dotnet build -c Release
dotnet test -c Release
# No diretorio AI:
.venv/Scripts/ruff check .
.venv/Scripts/python -m pytest -q
# No diretorio Flutter:
flutter analyze
flutter test
```

Testes PostgreSQL/RLS exigem bancos remotos dedicados; ausencia de configuracao
gera SKIP, nao certificacao de compatibilidade real. Consulte
[Supabase](docs/supabase.md) e [validacao](docs/validation.md).

## Documentacao

- [Arquitetura](docs/architecture.md)
- [Derivacao e inventario](docs/migration-from-equilibrafit.md)
- [Supabase, Auth, RLS e migrations](docs/supabase.md)
- [Configuracao Visual Studio e Supabase/Resend](docs/supabase-resend-setup.md)
- [Render e deploy](docs/render.md)
- [Google Play Billing](docs/google-play-billing.md)
- [Seguranca](docs/security.md)
- [Relatorio da entrega e pendencias](docs/validation.md)

A implementacao local nao equivale a Beta aprovada: Supabase real, email/deep
links, compras sandbox, Docker e deploy Render precisam de comprovacao externa.
