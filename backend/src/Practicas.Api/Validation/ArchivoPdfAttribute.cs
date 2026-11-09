using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Practicas.Api.Options;

namespace Practicas.Api.Validation;

/// <summary>
/// Valida que el archivo recibido sea un PDF real (extensión y firma "%PDF-")
/// y que no supere el tamaño máximo configurado.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class ArchivoPdfAttribute : ValidationAttribute
{
    private static readonly byte[] FirmaPdf = "%PDF-"u8.ToArray();

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // La obligatoriedad la controla [Required]; aquí solo se valida el contenido.
        if (value is not IFormFile archivo)
        {
            return ValidationResult.Success;
        }

        var tamanoMaximo = validationContext.GetService<IOptions<AlmacenamientoOptions>>()?.Value.TamanoMaximoBytes
                           ?? new AlmacenamientoOptions().TamanoMaximoBytes;

        var error = Validar(archivo, tamanoMaximo);
        return error is null
            ? ValidationResult.Success
            : new ValidationResult(error, [validationContext.MemberName ?? "HojaDeVida"]);
    }

    /// <summary>Devuelve el mensaje de error o <c>null</c> si el archivo es válido.</summary>
    public static string? Validar(IFormFile archivo, long tamanoMaximoBytes)
    {
        if (archivo.Length == 0)
        {
            return "La hoja de vida está vacía.";
        }

        if (archivo.Length > tamanoMaximoBytes)
        {
            return $"La hoja de vida no puede superar {tamanoMaximoBytes / (1024 * 1024)} MB.";
        }

        if (!string.Equals(Path.GetExtension(archivo.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "La hoja de vida debe ser un archivo PDF.";
        }

        Span<byte> encabezado = stackalloc byte[FirmaPdf.Length];
        using var stream = archivo.OpenReadStream();
        var leidos = stream.ReadAtLeast(encabezado, encabezado.Length, throwOnEndOfStream: false);

        if (leidos < FirmaPdf.Length || !encabezado.SequenceEqual(FirmaPdf))
        {
            return "El archivo no tiene un formato PDF válido.";
        }

        return null;
    }
}
