using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Practicas.Application.Abstracciones;
using Practicas.Application.Autenticacion;
using Practicas.Infrastructure.Almacenamiento;
using Practicas.Infrastructure.Autenticacion;
using Practicas.Infrastructure.Persistencia;

namespace Practicas.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra los adaptadores: base de datos, almacenamiento de archivos y seguridad.</summary>
    public static IServiceCollection AddInfraestructura(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AlmacenamientoOptions>()
            .Bind(configuration.GetSection(AlmacenamientoOptions.Seccion))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AutenticacionOptions>()
            .Bind(configuration.GetSection(AutenticacionOptions.Seccion))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // La cadena se resuelve al crear el contexto, así las pruebas pueden reemplazar la configuración.
        services.AddDbContext<PracticasDbContext>((sp, opciones) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var entorno = sp.GetRequiredService<IHostEnvironment>();
            opciones.UseSqlite(ResolverCadenaSqlite(config.GetConnectionString("Practicas"), entorno.ContentRootPath));
        });

        services.AddScoped<IPracticanteRepositorio, PracticanteRepositorio>();
        services.AddSingleton<IAlmacenamientoArchivos, AlmacenamientoLocal>();
        services.AddSingleton<IAutenticacionService, AutenticacionService>();
        services.AddMemoryCache();
        services.AddSingleton<IRevocacionTokens, RevocacionTokensEnMemoria>();

        return services;
    }

    /// <summary>Aplica las migraciones pendientes (crea la base de datos si no existe).</summary>
    public static async Task AplicarMigracionesAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<PracticasDbContext>().Database.MigrateAsync();
    }

    /// <summary>Permite rutas relativas en la cadena de conexión, resueltas contra la raíz del proyecto.</summary>
    internal static string ResolverCadenaSqlite(string? cadena, string raizContenido)
    {
        var constructor = new SqliteConnectionStringBuilder(cadena ?? "Data Source=App_Data/practicas.db");
        if (!string.IsNullOrWhiteSpace(constructor.DataSource)
            && constructor.DataSource != ":memory:"
            && !Path.IsPathRooted(constructor.DataSource))
        {
            constructor.DataSource = Path.GetFullPath(Path.Combine(raizContenido, constructor.DataSource));
        }

        if (Path.GetDirectoryName(constructor.DataSource) is { Length: > 0 } carpeta)
        {
            Directory.CreateDirectory(carpeta);
        }

        return constructor.ToString();
    }
}
