using System.ComponentModel.DataAnnotations;

namespace Practicas.Api.Contracts;

public class LoginRequest
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Contrasena { get; set; } = string.Empty;
}

public record LoginResponse(string Token, DateTime ExpiraUtc, string Nombre);
