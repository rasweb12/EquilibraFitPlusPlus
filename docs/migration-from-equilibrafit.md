# Derivacao independente do EquilibraFit++

Origem de referencia: `D:\Curso\APP\EQUILIBRAFIT`.
Destino independente: `D:\Curso\APP\EquilibraFit++`.
Repositorio planejado: `rasweb12/EQUILIBRAFIT-PLUSPLUS`.

O projeto de origem nao recebe alteracoes. O destino ja continha uma copia
parcial de `src` e `tests` quando a implementacao foi iniciada.

## Diagnostico

| Acao | Modulos |
| --- | --- |
| Reutilizar | Dominio, regras de negocio, contratos, casos de uso, treinos, alimentacao, evolucao, habitos, LGPD, auditoria, Coach, testes de ownership |
| Adaptar | API, Admin, Flutter, contexto de IA, autenticacao, perfil/tenant, premium, concorrencia otimista, filtros de indices |
| Substituir | SQL Server por Npgsql/PostgreSQL; autenticacao propria por Supabase Auth; cobranca por Google Play Billing |
| Descartar da nova base | Infraestrutura Azure, migrations SQL Server, configuracao de producao antiga, credenciais, dados e bancos da origem |

O SQLite do Flutter permanece responsavel por `sync_outbox`, `workout_cache`,
`workout_session_cache`, `ai_pending_request` e recuperacao apos reinicio.
O backend podera usar SQLite exclusivamente em desenvolvimento e testes.

## Migrations da origem

As migrations abaixo permanecem somente no repositorio original:

- 20260731210342_InitialCreate
- 20260731212614_Etapas4A7ModulosUsuario
- 20260731221403_Etapas8A10IaAdminDevOps
- 20260801125122_TreinoExercicioConclusao
- 20260808060137_RegistrosHabitos
- 20260808065037_AddProgressiveWorkoutTracking
- 20260808205243_AddWorkoutExerciseSoftDelete
- 20260813214915_AddAiCoachMemory
- 20260813221750_AddAiExecutionLog
- 20260813231432_AddOfflineIdempotencyAndWorkoutSessionOperationId
- 20260815220353_FixWorkoutExerciseUniqueIndexForSoftDelete
- 20260820222150_EnforceWorkoutExerciseUniqueIndexFilter

O destino nao continha migrations. Foram criadas sete migrations PostgreSQL
independentes a partir do modelo portado, sem executar o historico SQL Server.
Testes SQLite usarao seu proprio schema; nunca executarao migrations PostgreSQL.
O baseline foi aplicado e reaplicado com autorizacao no novo Supabase em
2026-10-07; o teste SQL remoto de RLS/indices/constraints passou com rollback.
Testes xUnit remotos ainda exigem connection strings dedicadas. Veja supabase.md
e supabase-resend-setup.md para resultados e gates de verificacao.
Nenhum dado real da origem sera importado automaticamente.

## Jobs

A analise encontrou apenas registro de Hangfire e dashboard; nenhum uso de
`BackgroundJob` ou `RecurringJob`. A nova base pode remover essa configuracao
sem perder jobs existentes. Novos jobs exigirao uma decisao documentada sobre
worker e persistencia PostgreSQL; nao ha dependencia obrigatoria de Redis.

## Limites de validacao

Esta documentacao registra a estrategia, nao certifica prontidao para Beta.
Schema/RLS remotos foram validados via MCP. Conexao Npgsql local, autenticacao,
SMTP, Google Play sandbox e Render ainda precisam de configuracao externa e E2E.
Commit e push dependem da revisao e autorizacao explicita do usuario.
