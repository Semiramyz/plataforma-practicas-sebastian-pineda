using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Practicas.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class EvaluacionConRubrica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Evaluacion_Evaluador",
                table: "Practicantes",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Evaluacion_Experiencia",
                table: "Practicantes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Evaluacion_Formacion",
                table: "Practicantes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Evaluacion_Habilidades",
                table: "Practicantes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Evaluacion_MotivosRechazo",
                table: "Practicantes",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Evaluacion_Observaciones",
                table: "Practicantes",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Evaluacion_Ortografia",
                table: "Practicantes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Evaluacion_Presentacion",
                table: "Practicantes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAutorizacionDatosUtc",
                table: "Practicantes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Evaluacion_Evaluador",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "Evaluacion_Experiencia",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "Evaluacion_Formacion",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "Evaluacion_Habilidades",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "Evaluacion_MotivosRechazo",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "Evaluacion_Observaciones",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "Evaluacion_Ortografia",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "Evaluacion_Presentacion",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "FechaAutorizacionDatosUtc",
                table: "Practicantes");
        }
    }
}
