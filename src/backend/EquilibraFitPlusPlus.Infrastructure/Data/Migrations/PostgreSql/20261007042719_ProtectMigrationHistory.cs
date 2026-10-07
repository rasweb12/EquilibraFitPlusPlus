using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquilibraFitPlusPlus.Infrastructure.Data.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class ProtectMigrationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE public."__EFMigrationsHistory" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON public."__EFMigrationsHistory" FROM PUBLIC;
                DO $history_rls$
                DECLARE client_role text;
                BEGIN
                  FOREACH client_role IN ARRAY ARRAY['anon', 'authenticated'] LOOP
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = client_role) THEN
                      EXECUTE format('REVOKE ALL ON public."__EFMigrationsHistory" FROM %I', client_role);
                    END IF;
                  END LOOP;
                END $history_rls$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Migration history protection requires a reviewed forward migration to roll back.");
        }
    }
}
