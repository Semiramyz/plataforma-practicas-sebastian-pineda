using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Practicas.Application.Validacion;
using Practicas.Infrastructure.Almacenamiento;

namespace Practicas.Api.Validacion;

/// <summary>
/// Adapta <see cref="ValidadorPdf"/> a la validación de modelos de ASP.NET Core: así el error
/// llega al cliente como un error del campo, junto con los demás del formulario.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class ArchivoPdfAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // La obligatoriedad la controla [Required]; aquí solo se valida el contenido.
        if (value is not IFormFile archivo)
        {
            return ValidationResult.Success;
        }

        var tamanoMaximo = validationContext.GetService<IOptions<AlmacenamientoOptions>>()?.Value.TamanoMaximoBytes
                           ?? new AlmacenamientoOptions().TamanoMaximoBytes;

        var error = ValidadorPdf.Validar(archivo.FileName, archivo.Length, archivo.OpenReadStream, tamanoMaximo);
        return error is null
            ? ValidationResult.Success
            : new ValidationResult(error, [validationContext.MemberName ?? "HojaDeVida"]);
    }
}
