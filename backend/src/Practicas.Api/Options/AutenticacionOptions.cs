using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Practicas.Api.Options;

public class AutenticacionOptions
{
    public const string Seccion = "Autenticacion";

    [Required]
    public string Emisor { get; set; } = "Practicas.Api";

    [Required]
    public string Audiencia { get; set; } = "Practicas.Web";

    /// <summary>Clave simétrica para firmar los JWT (mínimo 32 caracteres).</summary>
    [Required, MinLength(32)]
    public string ClaveFirma { get; set; } = string.Empty;

    [Range(1, 24 * 60)]
    public int MinutosExpiracion { get; set; } = 480;

    [Required, ValidateObjectMembers]
    public CredencialAnalista Analista { get; set; } = new();
}

public class CredencialAnalista
{
    [Required]
    public string Usuario { get; set; } = string.Empty;

    [Required]
    public string Contrasena { get; set; } = string.Empty;

    public string NombreVisible { get; set; } = "Analista de selección";
}
