using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
using Practicas.Api.Contratos;
using Practicas.Api.Seguridad;
using Practicas.Application.Autenticacion;

namespace Practicas.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed partial class AuthController(
    IAutenticacionService autenticacion,
    IRevocacionTokens revocacion,
    ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>Inicia sesión como analista y devuelve un token JWT.</summary>
    /// <remarks>Limitado por IP para frenar ataques de fuerza bruta (responde 429 al superar el límite).</remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasLimite.Login)]
    [ProducesResponseType<Sesion>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public ActionResult<Sesion> Login([FromBody] LoginRequest solicitud)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

        var sesion = autenticacion.IniciarSesion(solicitud.Usuario, solicitud.Contrasena);
        if (sesion is null)
        {
            // Se registra la IP, nunca la contraseña ni el usuario intentado (OWASP A09).
            LogLoginFallido(logger, ip);
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Credenciales inválidas",
                detail: "Usuario o contraseña incorrectos.");
        }

        LogLoginExitoso(logger, sesion.Nombre, ip);
        return Ok(sesion);
    }

    /// <summary>Cierra la sesión: el token deja de ser válido aunque aún no haya expirado.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        var exp = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        if (jti is not null && long.TryParse(exp, out var segundos))
        {
            revocacion.Revocar(jti, DateTimeOffset.FromUnixTimeSeconds(segundos));
        }

        LogLogout(logger, User.Identity?.Name ?? "desconocido");
        return NoContent();
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "Seguridad: inicio de sesión fallido desde {Ip}")]
    private static partial void LogLoginFallido(ILogger logger, string ip);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Seguridad: inicio de sesión de {Usuario} desde {Ip}")]
    private static partial void LogLoginExitoso(ILogger logger, string usuario, string ip);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Seguridad: {Usuario} cerró sesión y su token fue revocado")]
    private static partial void LogLogout(ILogger logger, string usuario);
}
