using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquilibraFitPlusPlus.Infrastructure.Data.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class GooglePlayBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AmbienteTeste",
                schema: "app",
                table: "Assinaturas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoRenovacao",
                schema: "app",
                table: "Assinaturas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CanceladoEm",
                schema: "app",
                table: "Assinaturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstadoCompra",
                schema: "app",
                table: "Assinaturas",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiracaoUtc",
                schema: "app",
                table: "Assinaturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InicioUtc",
                schema: "app",
                table: "Assinaturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderId",
                schema: "app",
                table: "Assinaturas",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Plataforma",
                schema: "app",
                table: "Assinaturas",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductId",
                schema: "app",
                table: "Assinaturas",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseTokenEncrypted",
                schema: "app",
                table: "Assinaturas",
                type: "character varying(6000)",
                maxLength: 6000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseTokenHash",
                schema: "app",
                table: "Assinaturas",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UltimoEventoId",
                schema: "app",
                table: "Assinaturas",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UltimoProcessamentoEm",
                schema: "app",
                table: "Assinaturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillingEvents",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssinaturaId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    State = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingEvents_Assinaturas_AssinaturaId",
                        column: x => x.AssinaturaId,
                        principalSchema: "app",
                        principalTable: "Assinaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_PurchaseTokenHash",
                schema: "app",
                table: "Assinaturas",
                column: "PurchaseTokenHash",
                unique: true,
                filter: "\"PurchaseTokenHash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BillingEvents_AssinaturaId",
                schema: "app",
                table: "BillingEvents",
                column: "AssinaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingEvents_EventId",
                schema: "app",
                table: "BillingEvents",
                column: "EventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingEvents",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_Assinaturas_PurchaseTokenHash",
                schema: "app",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "AmbienteTeste",
                schema: "app",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "AutoRenovacao",
                schema: "app",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "CanceladoEm",
                schema: "app",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "EstadoCompra",
                schema: "app",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "ExpiracaoUtc",
                schema: "app",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "InicioUtc",
                schema: "app",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "OrderId",
                schema: "app",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "Plataforma",
                schema: "app",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "ProductId",
                schema: "app",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "PurchaseTokenEncrypted",
                schema: "app",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "PurchaseTokenHash",
                schema: "app",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "UltimoEventoId",
                schema: "app",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "UltimoProcessamentoEm",
                schema: "app",
                table: "Assinaturas");
        }
    }
}
