using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquilibraFitPlusPlus.Infrastructure.Data.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class GlobalSessionRevocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SessoesRevogadasAntesDe",
                schema: "app",
                table: "Usuarios",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessoesRevogadasAntesDe",
                schema: "app",
                table: "Usuarios");
        }
    }
}
