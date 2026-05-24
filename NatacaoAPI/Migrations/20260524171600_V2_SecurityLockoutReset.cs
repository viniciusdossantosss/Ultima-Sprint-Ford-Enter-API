using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NatacaoAPI.Migrations
{
    /// <inheritdoc />
    public partial class V2_SecurityLockoutReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─── Account Lockout Fields ─────────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "TentativasLoginFalhas",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ContaBloqueada",
                table: "Usuarios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "BloqueioAte",
                table: "Usuarios",
                type: "datetime(6)",
                nullable: true);

            // ─── Password Reset Fields ──────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "Usuarios",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "Usuarios",
                type: "datetime(6)",
                nullable: true);

            // Índice para busca rápida por reset token
            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_ResetToken",
                table: "Usuarios",
                column: "ResetToken");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_ResetToken",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TentativasLoginFalhas",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ContaBloqueada",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "BloqueioAte",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiry",
                table: "Usuarios");
        }
    }
}
