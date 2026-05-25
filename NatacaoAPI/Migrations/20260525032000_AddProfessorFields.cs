using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NatacaoAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessorFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cref",
                table: "Usuarios",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CrefAtivo",
                table: "Usuarios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AptoBebes",
                table: "Usuarios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AptoInfantil",
                table: "Usuarios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AptoAdulto",
                table: "Usuarios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AptoAltaPerformance",
                table: "Usuarios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AptoHidroginastica",
                table: "Usuarios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AptoPcd",
                table: "Usuarios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidadeSalvamentoAquatico",
                table: "Usuarios",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidadePrimeirosSocorros",
                table: "Usuarios",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cref",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CrefAtivo",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "AptoBebes",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "AptoInfantil",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "AptoAdulto",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "AptoAltaPerformance",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "AptoHidroginastica",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "AptoPcd",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ValidadeSalvamentoAquatico",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ValidadePrimeirosSocorros",
                table: "Usuarios");
        }
    }
}
