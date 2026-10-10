# Arquitetura EquilibraFit++

## Decisao

Nova base independente: Flutter + SQLite, .NET 10 + Clean Architecture,
Supabase PostgreSQL + Supabase Auth, FastAPI e Blazor Admin. Render hospeda os
tres servicos. Azure nao e dependencia atual; sua avaliacao pertence ao futuro.
O original nao foi alterado para viabilizar esta arquitetura.

## Responsabilidades

| Camada | Responsabilidade |
| --- | --- |
| Domain | Entidades, invariantes e enums independentes do banco |
| Application | Casos de uso, contratos de repositorios, validacao, entitlement |
| Contracts / Shared | DTOs preservados, erros, paginacao |
| Infrastructure | EF Core, Npgsql/SQLite, Supabase Auth, AI e Google Play adapters |
| API | Autorizacao, contexto usuario/tenant, rate limiting, idempotencia, LGPD |
| Mobile | UI existente, sessao persistente, SQLite, outbox e sync |
| Admin | Blazor Server, roles obtidas da API, auditoria e operacao |
| AI | Contexto por finalidade, memoria/RAG, timeout, fallback seguro, OpenAI e Gemini |

Coach permite escolher OpenAI/Gemini por mensagem; as demais funcionalidades
usam Gemini. Veja [roteamento de IA](ai-provider-routing.md).

## Dados E Auth

Modelo de negocio completo reutilizado: treino/exercicios/sessoes/series,
alimentacao/planos, evolucao, habitos, premium/marketplace, notificacoes,
consentimento/LGPD, auditoria, IA, conflitos e idempotencia.

Supabase Auth e a autoridade de senha e token. O adapter preserva cadastro,
login e refresh da API; a API valida assinaturas ES256/RS256 via JWKS, issuer,
audience, validade, subject e session_id. Projetos usando apenas HS256 devem
migrar para signing key assimetrica no Supabase, nao adicionar segredo ao app.

`auth.users.id` associa-se logicamente a `app.Usuarios.IdentityUserId`; novos
perfis usam o mesmo UUID como Id. Nao ha FK cross-schema gerenciada pelo EF.
Tenant e role sao carregados do perfil, nunca de user_metadata ou claims
enviadas pelo cliente. AuthSessions registra revogacao local.
Um marco por usuario bloqueia autenticacoes anteriores a revogacao global,
incluindo session_ids ainda nao vistos e refresh com novo iat mas amr antigo.

A API conecta com credencial confiavel PostgreSQL que pode ignorar RLS.
Portanto filtros de tenant/usuario, verificacao de ownership e policies da API
sao obrigatorios. RLS e defesa adicional para acessos com roles cliente.

## Offline

Online: Flutter -> API -> Supabase.
Offline: Flutter -> SQLite -> outbox -> API na reconexao.
IA offline: pedido SQLite -> reconexao -> API -> FastAPI.

Banco local independente `equilibrafit_plusplus.db`, schema v5. Outbox e pedidos
AI sao associados a usuario/tenant; linhas legadas sem dono ficam em quarentena.
Troca A -> logout -> B nao envia pendencias de A com o token de B.
OperationId mantem idempotencia, 409 preserva conflito e retry permanece explicito.
RowVersion do backend e token de concorrencia gerado pela aplicacao.

## Jobs E Cache

Hangfire original tinha apenas registro/dashboard e nenhum job agendado
identificado. Nao foi portado. Reconciliacao Google Play usa hosted worker
periodico, alem de RTDN/restore. Render com suspensao nao garante worker continuo;
hospedagem always-on/scheduler deve ser validada antes de SLA de cobranca.
Redis continua opcional; sem configuracao utiliza cache em memoria.

## Nao Portado

SQL Server, migrations T-SQL, Azure/Bicep de runtime, Stripe e configuracoes de
producao antigas. Nenhum dado real ou secret foi copiado. Supabase Storage foi
avaliado, nao implementado nesta etapa: buckets privados/signed URLs exigem
adapter e testes proprios, sem persistir uploads importantes no container.
