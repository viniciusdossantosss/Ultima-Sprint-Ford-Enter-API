using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NatacaoAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentRegistrationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataNascimento",
                table: "Usuarios",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NivelPedagogico",
                table: "Usuarios",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModalidadeSugerida",
                table: "Usuarios",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefone",
                table: "Usuarios",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomeResponsavel",
                table: "Usuarios",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefoneResponsavel",
                table: "Usuarios",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DocumentacaoSaudeEntregue",
                table: "Usuarios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProblemasSaude",
                table: "Usuarios",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataNascimento",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "NivelPedagogico",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ModalidadeSugerida",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Telefone",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "NomeResponsavel",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TelefoneResponsavel",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "DocumentacaoSaudeEntregue",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ProblemasSaude",
                table: "Usuarios");
        }
    }
}
