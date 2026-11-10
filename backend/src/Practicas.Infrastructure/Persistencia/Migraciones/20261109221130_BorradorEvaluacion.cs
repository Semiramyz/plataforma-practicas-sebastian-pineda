using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Practicas.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class BorradorEvaluacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Borrador_Decision",
                table: "Practicantes",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Borrador_Evaluador",
                table: "Practicantes",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Borrador_Experiencia",
                table: "Practicantes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Borrador_FechaActualizacionUtc",
                table: "Practicantes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Borrador_Formacion",
                table: "Practicantes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Borrador_Habilidades",
                table: "Practicantes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Borrador_MotivosRechazo",
                table: "Practicantes",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Borrador_Observaciones",
                table: "Practicantes",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Borrador_Ortografia",
                table: "Practicantes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Borrador_Presentacion",
                table: "Practicantes",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Borrador_Decision",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "Borrador_Evaluador",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "Borrador_Experiencia",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "Borrador_FechaActualizacionUtc",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "Borrador_Formacion",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "Borrador_Habilidades",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "Borrador_MotivosRechazo",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "Borrador_Observaciones",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "Borrador_Ortografia",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "Borrador_Presentacion",
                table: "Practicantes");
        }
    }
}
