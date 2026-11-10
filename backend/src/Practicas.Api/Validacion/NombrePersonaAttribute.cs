using System.ComponentModel.DataAnnotations;
using Practicas.Application.Validacion;

namespace Practicas.Api.Validacion;

/// <summary>Adapta <see cref="ValidadorNombre"/> a la validación de modelos de ASP.NET Core.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NombrePersonaAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string texto || string.IsNullOrWhiteSpace(texto))
        {
            return ValidationResult.Success; // La obligatoriedad la controla [Required].
        }

        var error = ValidadorNombre.Validar(texto);
        return error is null
            ? ValidationResult.Success
            : new ValidationResult(error, [validationContext.MemberName ?? "NombreCompleto"]);
    }
}
