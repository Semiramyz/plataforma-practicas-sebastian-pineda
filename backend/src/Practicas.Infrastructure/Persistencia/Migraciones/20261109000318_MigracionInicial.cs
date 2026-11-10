using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Practicas.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class MigracionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Practicantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombreCompleto = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Correo = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    Carrera = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Semestre = table.Column<int>(type: "INTEGER", nullable: false),
                    HojaDeVidaNombreOriginal = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    HojaDeVidaArchivo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    HojaDeVidaTamanoBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FechaRegistroUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaEvaluacionUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Practicantes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Practicantes_Correo",
                table: "Practicantes",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Practicantes_Estado",
                table: "Practicantes",
                column: "Estado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Practicantes");
        }
    }
}
