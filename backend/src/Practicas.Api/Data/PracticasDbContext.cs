using Microsoft.EntityFrameworkCore;
using Practicas.Api.Domain;

namespace Practicas.Api.Data;

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
        });
    }
}
