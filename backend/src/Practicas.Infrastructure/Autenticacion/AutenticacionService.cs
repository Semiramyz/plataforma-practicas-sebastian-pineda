using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Practicas.Application.Autenticacion;

namespace Practicas.Infrastructure.Autenticacion;

/// <summary>
/// Autenticación básica del analista (usuario y hash de contraseña configurados) que entrega
/// un JWT con el rol <see cref="Roles.Analista"/> para consumir los endpoints protegidos.
/// </summary>
public sealed class AutenticacionService(IOptions<AutenticacionOptions> opciones, TimeProvider reloj) : IAutenticacionService
{
    private readonly AutenticacionOptions _opciones = opciones.Value;

    public Sesion? IniciarSesion(string usuario, string contrasena)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        ArgumentNullException.ThrowIfNull(contrasena);
        var analista = _opciones.Analista;

        // Se verifican siempre ambos (operador | sin cortocircuito): el tiempo de respuesta es el mismo
        // con un usuario inexistente que con una contraseña errada, y no revela qué usuarios existen.
        if (!SonIguales(usuario.Trim(), analista.Usuario) | !HashContrasena.Verificar(contrasena, analista.ContrasenaHash))
        {
            return null;
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var expira = ahora.AddMinutes(_opciones.MinutosExpiracion);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _opciones.Emisor,
            Audience = _opciones.Audiencia,
            IssuedAt = ahora,
            NotBefore = ahora,
            Expires = expira,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, analista.Usuario),
                new Claim(ReclamosJwt.Nombre, analista.NombreVisible),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim(ReclamosJwt.Rol, Roles.Analista)
            ]),
            SigningCredentials = new SigningCredentials(CrearClave(_opciones.ClaveFirma), SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new Sesion(token, expira, analista.NombreVisible);
    }

    public static SymmetricSecurityKey CrearClave(string clave) => new(Encoding.UTF8.GetBytes(clave));

    /// <summary>Comparación en tiempo constante para no filtrar información por tiempos de respuesta.</summary>
    private static bool SonIguales(string recibido, string esperado) =>
        !string.IsNullOrEmpty(esperado) &&
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(recibido)),
            SHA256.HashData(Encoding.UTF8.GetBytes(esperado)));
}

/// <summary>Nombres de los claims del JWT (cortos y sin mapear a los de WS-Federation).</summary>
public static class ReclamosJwt
{
    public const string Nombre = "name";
    public const string Rol = "role";
}
