using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquilibraFitPlusPlus.Infrastructure.Data.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class PersistPlanMeals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanosRefeicoes",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanoUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    Calorias = table.Column<int>(type: "integer", nullable: false),
                    ImagemUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AlternativasJson = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: false),
                    ReceitaRapidaJson = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanosRefeicoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanosRefeicoes_PlanosUsuario_PlanoUsuarioId",
                        column: x => x.PlanoUsuarioId,
                        principalSchema: "app",
                        principalTable: "PlanosUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanosRefeicoes_PlanoUsuarioId_Ordem",
                schema: "app",
                table: "PlanosRefeicoes",
                columns: new[] { "PlanoUsuarioId", "Ordem" },
                unique: true);

            migrationBuilder.Sql("""
                ALTER TABLE app."PlanosRefeicoes" ENABLE ROW LEVEL SECURITY;
                REVOKE ALL ON app."PlanosRefeicoes" FROM PUBLIC;
                DO $plan_meals_rls$
                DECLARE client_role text;
                BEGIN
                  FOREACH client_role IN ARRAY ARRAY['anon', 'authenticated'] LOOP
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = client_role) THEN
                      EXECUTE format('REVOKE ALL ON app."PlanosRefeicoes" FROM %I', client_role);
                    END IF;
                  END LOOP;
                END $plan_meals_rls$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanosRefeicoes",
                schema: "app");
        }
    }
}
