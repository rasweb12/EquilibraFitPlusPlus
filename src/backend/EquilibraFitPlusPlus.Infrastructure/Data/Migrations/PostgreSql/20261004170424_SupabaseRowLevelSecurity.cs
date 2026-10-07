using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquilibraFitPlusPlus.Infrastructure.Data.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class SupabaseRowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $rls$
                DECLARE
                  relation record;
                  predicate text;
                  parent_table text;
                  parent_key text;
                BEGIN
                  FOR relation IN SELECT table_name FROM information_schema.tables
                    WHERE table_schema = 'app' AND table_type = 'BASE TABLE'
                  LOOP
                    EXECUTE format('ALTER TABLE app.%I ENABLE ROW LEVEL SECURITY', relation.table_name);
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'anon') THEN
                      EXECUTE format('REVOKE ALL ON app.%I FROM anon', relation.table_name);
                    END IF;
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'authenticated') THEN
                      EXECUTE format('REVOKE ALL ON app.%I FROM authenticated', relation.table_name);
                    END IF;
                  END LOOP;

                  -- Standalone PostgreSQL has no Supabase roles/functions; keep default-deny RLS.
                  IF to_regprocedure('auth.uid()') IS NULL OR
                    NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'authenticated') THEN
                    RETURN;
                  END IF;
                  GRANT USAGE ON SCHEMA app TO authenticated;
                  CREATE POLICY own_profile_read ON app."Usuarios" FOR SELECT TO authenticated
                    USING ("IdentityUserId" = (SELECT auth.uid()) AND "ExcluidoEm" IS NULL);
                  GRANT SELECT ON app."Usuarios" TO authenticated;

                  FOR relation IN SELECT table_name FROM information_schema.tables
                    WHERE table_schema = 'app' AND table_type = 'BASE TABLE'
                      AND table_name NOT IN ('Usuarios', 'AuthSessions', 'RefreshTokens',
                        'IdempotencyRecords', 'Auditorias', 'AiExecutionLogs',
                        'FeatureFlags', 'ConfiguracoesSistema', 'Tenants')
                  LOOP
                    predicate := NULL;
                    parent_table := NULL;
                    parent_key := NULL;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'app'
                        AND table_name = relation.table_name AND column_name = 'UsuarioId') THEN
                      predicate := format('EXISTS (SELECT 1 FROM app."Usuarios" u WHERE
                        u."Id" = %I."UsuarioId" AND u."TenantId" = %I."TenantId"
                        AND u."IdentityUserId" = (SELECT auth.uid()) AND u."ExcluidoEm" IS NULL)',
                        relation.table_name, relation.table_name);
                    ELSE
                      CASE relation.table_name
                        WHEN 'TreinosExercicios' THEN parent_table := 'TreinosUsuario'; parent_key := 'TreinoUsuarioId';
                        WHEN 'TreinosSeriesRealizadas' THEN parent_table := 'TreinosSessoes'; parent_key := 'TreinoSessaoId';
                        WHEN 'ItensAlimentares' THEN parent_table := 'RegistrosAlimentares'; parent_key := 'RegistroAlimentarId';
                        WHEN 'MedidasCorporais' THEN parent_table := 'RegistrosEvolucao'; parent_key := 'RegistroEvolucaoId';
                        WHEN 'MetasNutricionais' THEN parent_table := 'PlanosUsuario'; parent_key := 'PlanoUsuarioId';
                        WHEN 'ChatMessages' THEN parent_table := 'ChatSessions'; parent_key := 'ChatSessionId';
                        WHEN 'Pagamentos' THEN parent_table := 'Assinaturas'; parent_key := 'AssinaturaId';
                        ELSE NULL;
                      END CASE;
                      IF parent_table IS NOT NULL THEN
                        predicate := format('EXISTS (SELECT 1 FROM app.%I p JOIN app."Usuarios" u
                          ON u."Id" = p."UsuarioId" AND u."TenantId" = p."TenantId"
                          WHERE p."Id" = %I.%I AND p."TenantId" = %I."TenantId"
                          AND u."IdentityUserId" = (SELECT auth.uid()) AND u."ExcluidoEm" IS NULL)',
                          parent_table, relation.table_name, parent_key, relation.table_name);
                      END IF;
                    END IF;
                    IF predicate IS NOT NULL THEN
                      EXECUTE format('CREATE POLICY own_read ON app.%I FOR SELECT TO authenticated USING (%s)',
                        relation.table_name, predicate);
                      EXECUTE format('GRANT SELECT ON app.%I TO authenticated', relation.table_name);
                    END IF;
                    -- Client INSERT/UPDATE/DELETE stay denied; all writes pass through the API.
                  END LOOP;
                END $rls$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("RLS rollback requires a reviewed forward migration; it must not silently expose application data.");
        }
    }
}
