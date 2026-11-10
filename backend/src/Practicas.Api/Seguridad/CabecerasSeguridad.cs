namespace Practicas.Api.Seguridad;

/// <summary>
/// Agrega cabeceras de seguridad a todas las respuestas:
/// <list type="bullet">
/// <item><b>Content-Security-Policy</b>: solo se ejecutan scripts del propio sitio (mitiga XSS y el robo del token).</item>
/// <item><b>frame-ancestors 'none'</b> y <b>X-Frame-Options</b>: nadie puede incrustar la app en un iframe (clickjacking).</item>
/// <item><b>X-Content-Type-Options: nosniff</b>: el navegador no reinterpreta un archivo subido como HTML o script.</item>
/// <item><b>Cache-Control: no-store</b> en la API: datos personales y hojas de vida no quedan en la caché del navegador.</item>
/// </list>
/// </summary>
public static class CabecerasSeguridad
{
    public const string PoliticaContenido =
        "default-src 'self'; " +
        "script-src 'self'; " +
        // Angular inserta los estilos de cada componente en etiquetas <style>.
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        // Visor de la hoja de vida: el PDF se muestra desde un blob creado por la propia página.
        "frame-src 'self' blob:; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    public static IApplicationBuilder UseCabecerasSeguridad(this IApplicationBuilder app) =>
        app.Use(async (contexto, siguiente) =>
        {
            contexto.Response.OnStarting(() =>
            {
                var cabeceras = contexto.Response.Headers;
                cabeceras.ContentSecurityPolicy = PoliticaContenido;
                cabeceras.XContentTypeOptions = "nosniff";
                cabeceras.XFrameOptions = "DENY";
                cabeceras["Referrer-Policy"] = "no-referrer";
                cabeceras["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
                cabeceras["Cross-Origin-Opener-Policy"] = "same-origin";

                if (contexto.Request.Path.StartsWithSegments("/api"))
                {
                    cabeceras.CacheControl = "no-store";
                    cabeceras.Pragma = "no-cache";
                }

                return Task.CompletedTask;
            });

            await siguiente(contexto);
        });
}
