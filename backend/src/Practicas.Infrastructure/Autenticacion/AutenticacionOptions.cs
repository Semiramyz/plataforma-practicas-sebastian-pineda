using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;


namespace Practicas.Infrastructure.Autenticacion;

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

    /// <summary>Duración de la sesión del analista. Corta a propósito: 60 minutos por defecto.</summary>
    [Range(5, 12 * 60)]
    public int MinutosExpiracion { get; set; } = 60;

    [Required, ValidateObjectMembers]
    public CredencialAnalista Analista { get; set; } = new();
}

public class CredencialAnalista : IValidatableObject
{
    [Required]
    public string Usuario { get; set; } = string.Empty;

    /// <summary>
    /// Hash PBKDF2 de la contraseña (nunca la contraseña en texto plano). Se genera con:
    /// <c>dotnet run --project backend/src/Practicas.Api -- --generar-hash "contraseña"</c>.
    /// </summary>
    [Required]
    public string ContrasenaHash { get; set; } = string.Empty;

    public string NombreVisible { get; set; } = "Analista de selección";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(ContrasenaHash) && !HashContrasena.TieneFormatoValido(ContrasenaHash))
        {
            yield return new ValidationResult(
                "ContrasenaHash no tiene el formato PBKDF2-SHA256$iteraciones$sal$hash. Genéralo con --generar-hash.",
                [nameof(ContrasenaHash)]);
        }
    }
}
