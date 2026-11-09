using System.ComponentModel.DataAnnotations;
using Practicas.Api.Domain;
using Practicas.Api.Validation;

namespace Practicas.Api.Contracts;

/// <summary>
/// Datos que envía el practicante desde el formulario (multipart/form-data).
/// </summary>
public class RegistrarPracticanteRequest
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(254, ErrorMessage = "El correo no puede superar 254 caracteres.")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La carrera universitaria es obligatoria.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "La carrera debe tener entre 2 y 150 caracteres.")]
    public string Carrera { get; set; } = string.Empty;

    [Required(ErrorMessage = "El semestre actual es obligatorio.")]
    [Range(1, 12, ErrorMessage = "El semestre debe estar entre 1 y 12.")]
    public int? Semestre { get; set; }

    [Required(ErrorMessage = "La hoja de vida (PDF) es obligatoria.")]
    [ArchivoPdf]
    public IFormFile? HojaDeVida { get; set; }
}

public record RegistroPracticanteResponse(int Id, string NombreCompleto, DateTime FechaRegistroUtc);

public record PracticanteResponse(
    int Id,
    string NombreCompleto,
    string Correo,
    string Carrera,
    int Semestre,
    EstadoPracticante Estado,
    DateTime FechaRegistroUtc,
    DateTime? FechaEvaluacionUtc,
    string HojaDeVidaNombre,
    long HojaDeVidaTamanoBytes)
{
    public static PracticanteResponse Desde(Practicante p) => new(
        p.Id,
        p.NombreCompleto,
        p.Correo,
        p.Carrera,
        p.Semestre,
        p.Estado,
        p.FechaRegistroUtc,
        p.FechaEvaluacionUtc,
        p.HojaDeVidaNombreOriginal,
        p.HojaDeVidaTamanoBytes);
}

public class ActualizarEstadoRequest
{
    [Required(ErrorMessage = "El estado es obligatorio.")]
    [AllowedValues(EstadoPracticante.Viable, EstadoPracticante.NoViable,
        ErrorMessage = "El estado debe ser 'Viable' o 'NoViable'.")]
    public EstadoPracticante? Estado { get; set; }
}
