namespace Practicas.Api.Storage;

/// <summary>
/// Abstracción del lugar donde se guardan las hojas de vida. La implementación
/// actual usa una carpeta del servidor, pero podría cambiarse por un blob storage
/// sin tocar la lógica de negocio.
/// </summary>
public interface IAlmacenamientoArchivos
{
    /// <summary>Guarda el contenido y devuelve el nombre generado del archivo.</summary>
    Task<string> GuardarAsync(Stream contenido, string extension, CancellationToken cancellationToken = default);

    /// <summary>Abre el archivo para lectura o devuelve <c>null</c> si no existe.</summary>
    Stream? Abrir(string nombreArchivo);

    void Eliminar(string nombreArchivo);
}
