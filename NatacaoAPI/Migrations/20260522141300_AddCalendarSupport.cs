using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NatacaoAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiaSemana",
                table: "Turmas");

            migrationBuilder.DropColumn(
                name: "HorarioFim",
                table: "Turmas");

            migrationBuilder.DropColumn(
                name: "HorarioInicio",
                table: "Turmas");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataHoraFim",
                table: "Turmas",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DataHoraInicio",
                table: "Turmas",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataHoraFim",
                table: "Turmas");

            migrationBuilder.DropColumn(
                name: "DataHoraInicio",
                table: "Turmas");

            migrationBuilder.AddColumn<string>(
                name: "DiaSemana",
                table: "Turmas",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "HorarioFim",
                table: "Turmas",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "HorarioInicio",
                table: "Turmas",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }
    }
}
