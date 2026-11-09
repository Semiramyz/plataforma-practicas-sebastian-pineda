using Practicas.Api.Contracts;

namespace Practicas.Api.Auth;

public interface IAutenticacionService
{
    /// <summary>Valida las credenciales del analista y emite un JWT, o devuelve <c>null</c>.</summary>
    LoginResponse? IniciarSesion(string usuario, string contrasena);
}
