using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Practicas.Application.Abstracciones;

namespace Practicas.Infrastructure.Almacenamiento;

/// <summary>
/// Guarda los archivos en una carpeta del servidor con nombres generados (GUID),
/// de modo que el nombre que envía el usuario nunca se usa para construir rutas.
/// </summary>
public sealed class AlmacenamientoLocal : IAlmacenamientoArchivos
{
    private readonly string _carpeta;

    public AlmacenamientoLocal(IOptions<AlmacenamientoOptions> opciones, IHostEnvironment entorno)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        ArgumentNullException.ThrowIfNull(entorno);
        var ruta = opciones.Value.RutaHojasDeVida;
        _carpeta = Path.GetFullPath(Path.IsPathRooted(ruta) ? ruta : Path.Combine(entorno.ContentRootPath, ruta));
        Directory.CreateDirectory(_carpeta);
    }

    public string Carpeta => _carpeta;

    public async Task<string> GuardarAsync(Stream contenido, string extension, CancellationToken cancellationToken = default)
    {
        var nombre = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var ruta = Path.Combine(_carpeta, nombre);

        await using var destino = new FileStream(ruta, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            bufferSize: 81920, useAsync: true);
        await contenido.CopyToAsync(destino, cancellationToken);

        return nombre;
    }

    public Stream? Abrir(string nombreArchivo)
    {
        var ruta = RutaSegura(nombreArchivo);
        return ruta is not null && File.Exists(ruta)
            ? new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true)
            : null;
    }

    public void Eliminar(string nombreArchivo)
    {
        var ruta = RutaSegura(nombreArchivo);
        if (ruta is not null && File.Exists(ruta))
        {
            File.Delete(ruta);
        }
    }

    /// <summary>Evita path traversal: la ruta final debe quedar dentro de la carpeta configurada.</summary>
    private string? RutaSegura(string nombreArchivo)
    {
        if (string.IsNullOrWhiteSpace(nombreArchivo) || nombreArchivo != Path.GetFileName(nombreArchivo))
        {
            return null;
        }

        var ruta = Path.GetFullPath(Path.Combine(_carpeta, nombreArchivo));
        return ruta.StartsWith(_carpeta + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? ruta : null;
    }
}
