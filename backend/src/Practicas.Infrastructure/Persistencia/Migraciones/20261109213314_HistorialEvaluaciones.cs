using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Practicas.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class HistorialEvaluaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistorialEvaluaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Decision = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PuntajeTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    Evaluador = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PracticanteId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialEvaluaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialEvaluaciones_Practicantes_PracticanteId",
                        column: x => x.PracticanteId,
                        principalTable: "Practicantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEvaluaciones_PracticanteId",
                table: "HistorialEvaluaciones",
                column: "PracticanteId");

            // Las evaluaciones hechas antes de esta versión pasan al historial (puntaje = (suma - 5) * 5).
            migrationBuilder.Sql("""
                INSERT INTO HistorialEvaluaciones (PracticanteId, Decision, PuntajeTotal, Evaluador, FechaUtc)
                SELECT Id, Estado,
                       (Evaluacion_Ortografia + Evaluacion_Presentacion + Evaluacion_Formacion
                        + Evaluacion_Experiencia + Evaluacion_Habilidades - 5) * 5,
                       Evaluacion_Evaluador, FechaEvaluacionUtc
                FROM Practicantes
                WHERE Evaluacion_Evaluador IS NOT NULL AND FechaEvaluacionUtc IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialEvaluaciones");
        }
    }
}
