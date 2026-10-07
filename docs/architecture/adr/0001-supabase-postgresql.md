# ADR 0001 - Supabase PostgreSQL e Auth

Status: aceito para a nova base EquilibraFit++; verificacao cloud pendente.
Data: 2026-10-04.

## Contexto

EquilibraFit++ e independente do original; precisa preservar regras maduras
sem depender de SQL Server/Azure/Stripe. O original conserva seu historico e dados.

## Decisao

Supabase PostgreSQL e banco remoto Beta. Supabase Auth gerencia credenciais
e sessoes, consumido pelo adapter da API. EF Core continua com Npgsql e migrations
novas. SQLite backend e somente dev/testes, SQLite mobile continua offline.
RLS e obrigatoria, writes pelo backend; Data API/Storage/Realtime somente mediante
uso concreto e policies revisadas. Azure pertence ao futuro, nao ao runtime Beta.

## Motivos

PostgreSQL gerenciado reduz infraestrutura e elimina SQL Server/Azure SQL.
Supabase oferece caminho de integracao Flutter/Auth/Storage/Realtime.
Adequacao de plano inicial deve ser confirmada quanto a quotas/disponibilidade,
sem prometer gratuidade permanente ou capacidade de producao.

## Consequencias

Baseline PostgreSQL independente; filtros parciais/UUID/datas/concorrencia
adaptados e testados. Auth migra porque esta e a especificacao nova independente,
diferente da proposta anterior de migracao em-place.
Hangfire SQL Server nao e portado: nao foram encontrados jobs existentes.
Billing usa Google Play com adapter, worker e RTDN.
API conserva autorizacao, tenant, LGPD, sync e idempotencia.
Testes remotos, Docker/Render e Play sandbox sao gates antes de Beta.
