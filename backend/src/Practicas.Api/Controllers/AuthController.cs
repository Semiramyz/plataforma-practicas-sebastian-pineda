using Microsoft.AspNetCore.Mvc;
using Practicas.Api.Auth;
using Practicas.Api.Contracts;

namespace Practicas.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IAutenticacionService autenticacion) : ControllerBase
{
    /// <summary>Inicia sesión como analista y devuelve un token JWT.</summary>
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest solicitud)
    {
        var sesion = autenticacion.IniciarSesion(solicitud.Usuario, solicitud.Contrasena);
        if (sesion is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Credenciales inválidas",
                detail: "Usuario o contraseña incorrectos.");
        }

        return Ok(sesion);
    }
}
