using Microsoft.Extensions.FileProviders;

namespace Practicas.Api.Hosting;

/// <summary>
/// Modo de un solo servidor: si "Frontend:Ruta" apunta a la carpeta generada por <c>ng build</c>,
/// la API también sirve la aplicación web (iniciar.ps1, iniciar.sh y Docker). En desarrollo normal
/// se deja vacío y el frontend corre con <c>ng serve</c> y su proxy.
/// </summary>
public static class FrontendIntegrado
{
    public static PhysicalFileProvider? UseFrontendIntegrado(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var archivos = CrearProveedor(app.Configuration["Frontend:Ruta"], app.Environment.ContentRootPath);
        if (archivos is not null)
        {
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = archivos });
            app.UseStaticFiles(new StaticFileOptions { FileProvider = archivos });
        }

        return archivos;
    }

    /// <summary>
    /// Las rutas de /api que no existen siguen respondiendo 404; el resto (/registro, /analista...)
    /// las resuelve el enrutador de Angular a partir de index.html.
    /// </summary>
    public static void MapFrontendIntegrado(this WebApplication app, PhysicalFileProvider? archivos)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (archivos is null)
        {
            return;
        }

        app.MapFallback("/api/{**resto}", () => Results.NotFound()).AllowAnonymous();
        app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = archivos }).AllowAnonymous();
    }

    private static PhysicalFileProvider? CrearProveedor(string? ruta, string raizContenido)
    {
        if (string.IsNullOrWhiteSpace(ruta))
        {
            return null;
        }

        var carpeta = Path.GetFullPath(ruta, raizContenido);
        if (!File.Exists(Path.Combine(carpeta, "index.html")))
        {
            throw new InvalidOperationException(
                $"Frontend:Ruta apunta a '{carpeta}', pero ahí no hay un index.html. Compila el frontend con 'npm run build'.");
        }

        return new PhysicalFileProvider(carpeta);
    }
}
