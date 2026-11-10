namespace Practicas.Application.Autenticacion;

/// <summary>Valida las credenciales del analista y emite su sesión.</summary>
public interface IAutenticacionService
{
    /// <summary>Devuelve la sesión o <c>null</c> si las credenciales no son válidas.</summary>
    Sesion? IniciarSesion(string usuario, string contrasena);
}

/// <summary>Sesión emitida al analista (token JWT y su expiración).</summary>
public sealed record Sesion(string Token, DateTime ExpiraUtc, string Nombre);
