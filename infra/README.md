# Infraestrutura EquilibraFit++

Runtime Beta: Render (API/Admin/AI) + Supabase (PostgreSQL/Auth).
Nao ha SQL Server, banco Docker local, Azure ou SQLite persistente Render.
Redis e opcional via profile cache. Nao armazene uploads no container efemero.

As imagens e Blueprint sao independentes da origem. Consulte docs/render.md,
docs/supabase.md, docs/google-play-billing.md e .env.example na raiz.

Docker ausente impede validar imagens/Compose localmente. O script
scripts/validate_deployment.py valida schema/paths, nao substitui Docker build/up.
Instale yaml/jsonschema apenas no ambiente de validacao se necessario e rode:

```powershell
src/ai/equilibrafit_plusplus_ai/.venv/Scripts/python scripts/validate_deployment.py
docker compose config --quiet
docker compose build
docker compose up -d
docker compose ps
```

Revisar custos e configurar secrets nos paineis antes de publicar.
Nao fazer commit/push ou usar o projeto original como alvo de deploy.

Supabase EquilibraFit++ foi inicializado remotamente em 2026-10-07, com sete
migrations EF e RLS validada via SQL transacional. infra/supabase/verify-database.sql
reproduz as verificacoes sem manter fixtures; os templates em supabase/templates
sao para o painel Auth/Supabase, nao para duplicar templates Resend.
Consulte docs/supabase-resend-setup.md para DNS, SMTP e User Secrets locais.
