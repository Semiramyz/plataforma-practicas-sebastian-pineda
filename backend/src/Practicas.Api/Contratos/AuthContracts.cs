using System.ComponentModel.DataAnnotations;

namespace Practicas.Api.Contratos;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [StringLength(100, ErrorMessage = "El usuario no puede superar 100 caracteres.")]
    public string Usuario { get; set; } = string.Empty;

    // Límite de longitud: evita que una contraseña enorme haga costoso el cálculo del hash.
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(200, ErrorMessage = "La contraseña no puede superar 200 caracteres.")]
    public string Contrasena { get; set; } = string.Empty;
}
