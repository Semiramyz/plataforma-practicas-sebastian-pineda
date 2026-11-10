using System.ComponentModel.DataAnnotations;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Practicas.Api.Seguridad;

public static class PoliticasLimite
{
    public const string Login = "login";
    public const string Registro = "registro";
}

/// <summary>Límites de solicitudes por dirección IP (sección "LimitesSolicitudes").</summary>
public class LimitesSolicitudesOptions
{
    public const string Seccion = "LimitesSolicitudes";

    /// <summary>Intentos de inicio de sesión permitidos por IP en cada ventana.</summary>
    [Range(1, 10_000)]
    public int IntentosLogin { get; set; } = 5;

    [Range(1, 1440)]
    public int MinutosVentanaLogin { get; set; } = 5;

    /// <summary>Registros de practicantes permitidos por IP en cada ventana (evita spam y llenar el disco).</summary>
    [Range(1, 10_000)]
    public int RegistrosPorVentana { get; set; } = 10;

    [Range(1, 1440)]
    public int MinutosVentanaRegistro { get; set; } = 60;
}

public static class LimitesSolicitudesExtensions
{
    public static IServiceCollection AddLimitesSolicitudes(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LimitesSolicitudesOptions>()
            .Bind(configuration.GetSection(LimitesSolicitudesOptions.Seccion))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        static LimitesSolicitudesOptions Limites(HttpContext contexto) =>
            contexto.RequestServices.GetRequiredService<IOptions<LimitesSolicitudesOptions>>().Value;

        return services.AddRateLimiter(opciones =>
        {
            opciones.AddPolicy(PoliticasLimite.Login, contexto => VentanaFijaPorIp(
                contexto, Limites(contexto).IntentosLogin, TimeSpan.FromMinutes(Limites(contexto).MinutosVentanaLogin)));

            opciones.AddPolicy(PoliticasLimite.Registro, contexto => VentanaFijaPorIp(
                contexto, Limites(contexto).RegistrosPorVentana, TimeSpan.FromMinutes(Limites(contexto).MinutosVentanaRegistro)));

            opciones.OnRejected = async (contexto, cancellationToken) =>
            {
                var respuesta = contexto.HttpContext.Response;
                respuesta.StatusCode = StatusCodes.Status429TooManyRequests;

                var detalle = "Hiciste demasiados intentos. Espera unos minutos e inténtalo de nuevo.";
                if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                {
                    var segundos = (int)Math.Ceiling(espera.TotalSeconds);
                    respuesta.Headers.RetryAfter = segundos.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    detalle = $"Hiciste demasiados intentos. Intenta de nuevo en {Math.Max(1, (int)Math.Ceiling(segundos / 60d))} minuto(s).";
                }

                await respuesta.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Demasiadas solicitudes",
                    Detail = detalle
                }, options: null, contentType: "application/problem+json", cancellationToken);
            };
        });
    }

    private static RateLimitPartition<string> VentanaFijaPorIp(HttpContext contexto, int permitidas, TimeSpan ventana) =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitidas,
                Window = ventana,
                QueueLimit = 0
            });
}
