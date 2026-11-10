using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Practicas.Domain;

namespace Practicas.Infrastructure.Persistencia;

public class PracticasDbContext(DbContextOptions<PracticasDbContext> options) : DbContext(options)
{
    public DbSet<Practicante> Practicantes => Set<Practicante>();

    /// <summary>
    /// SQLite no guarda la zona horaria: todas las fechas se almacenan en UTC y al leerlas
    /// se marcan como <see cref="DateTimeKind.Utc"/> para que se serialicen con "Z".
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<FechaUtcConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<FechaUtcNullableConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Practicante>(entidad =>
        {
            entidad.ToTable("Practicantes");
            entidad.HasKey(p => p.Id);

            entidad.Property(p => p.NombreCompleto).HasMaxLength(150).IsRequired();
            entidad.Property(p => p.Correo).HasMaxLength(254).IsRequired();
            entidad.HasIndex(p => p.Correo).IsUnique();
            entidad.Property(p => p.Carrera).HasMaxLength(150).IsRequired();

            entidad.Property(p => p.HojaDeVidaNombreOriginal).HasMaxLength(255).IsRequired();
            entidad.Property(p => p.HojaDeVidaArchivo).HasMaxLength(100).IsRequired();

            entidad.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);
            entidad.HasIndex(p => p.Estado);

            // La evaluación vive en la misma tabla (columnas Evaluacion_*): siempre se lee con el practicante.
            entidad.OwnsOne(p => p.Evaluacion, evaluacion =>
            {
                evaluacion.Ignore(e => e.Calificacion);
                evaluacion.Property(e => e.Observaciones).HasMaxLength(ReglasEvaluacion.LongitudMaximaObservaciones);
                evaluacion.Property(e => e.Evaluador).HasMaxLength(100).IsRequired();
                ComoTexto(evaluacion.Property(e => e.MotivosRechazo));
            });
            entidad.Navigation(p => p.Evaluacion).IsRequired(false);

            // Evaluación en curso (columnas Borrador_*): se guarda mientras el analista califica.
            // Evaluador es obligatorio: así EF distingue "sin borrador" de "borrador sin criterios aún".
            entidad.OwnsOne(p => p.Borrador, borrador =>
            {
                borrador.Ignore(b => b.Calificacion);
                borrador.Property(b => b.Decision).HasConversion<string>().HasMaxLength(20);
                borrador.Property(b => b.Observaciones).HasMaxLength(ReglasEvaluacion.LongitudMaximaObservaciones);
                borrador.Property(b => b.Evaluador).HasMaxLength(100).IsRequired();
                ComoTexto(borrador.Property(b => b.MotivosRechazo));
            });
            entidad.Navigation(p => p.Borrador).IsRequired(false);

            // Historial de auditoría en su propia tabla: una fila por cada evaluación, nunca se borra.
            entidad.OwnsMany(p => p.Historial, historial =>
            {
                historial.ToTable("HistorialEvaluaciones");
                historial.WithOwner().HasForeignKey("PracticanteId");
                historial.Property<int>("Id");
                historial.HasKey("Id");
                historial.Property(h => h.Decision).HasConversion<string>().HasMaxLength(20);
                historial.Property(h => h.Evaluador).HasMaxLength(100).IsRequired();
            });
            entidad.Navigation(p => p.Historial).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }

    /// <summary>Guarda una lista de motivos como texto separado por comas ("ErroresOrtograficos,Otro").</summary>
    private static void ComoTexto(PropertyBuilder<List<MotivoRechazo>> propiedad) =>
        propiedad
            .HasMaxLength(300)
            .HasConversion(
                motivos => string.Join(',', motivos),
                texto => texto.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Enum.Parse<MotivoRechazo>)
                    .ToList(),
                new ValueComparer<List<MotivoRechazo>>(
                    (a, b) => a!.SequenceEqual(b!),
                    lista => lista.Aggregate(0, (hash, motivo) => HashCode.Combine(hash, motivo)),
                    lista => lista.ToList()));
}
