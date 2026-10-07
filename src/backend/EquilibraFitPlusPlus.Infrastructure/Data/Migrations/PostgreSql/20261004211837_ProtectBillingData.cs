using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquilibraFitPlusPlus.Infrastructure.Data.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class ProtectBillingData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE app."BillingEvents" ENABLE ROW LEVEL SECURITY;
                DROP POLICY IF EXISTS own_read ON app."Assinaturas";
                DROP POLICY IF EXISTS own_read ON app."Pagamentos";
                REVOKE ALL ON app."BillingEvents", app."Assinaturas", app."Pagamentos" FROM PUBLIC;
                DO $billing_rls$
                DECLARE client_role text;
                BEGIN
                  FOREACH client_role IN ARRAY ARRAY['anon', 'authenticated'] LOOP
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = client_role) THEN
                      EXECUTE format('REVOKE ALL ON app."BillingEvents", app."Assinaturas", app."Pagamentos" FROM %I', client_role);
                    END IF;
                  END LOOP;
                END $billing_rls$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Billing access restrictions require a reviewed forward migration to roll back.");
        }
    }
}
