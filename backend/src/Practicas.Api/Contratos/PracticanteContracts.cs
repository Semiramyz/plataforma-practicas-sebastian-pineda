using System.ComponentModel.DataAnnotations;
using Practicas.Api.Validacion;
using Practicas.Application.Practicantes;
using Practicas.Domain;

namespace Practicas.Api.Contratos;

/// <summary>Datos que envía el practicante desde el formulario (multipart/form-data).</summary>
public sealed class RegistrarPracticanteRequest
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
    [NombrePersona]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [StringLength(254, ErrorMessage = "El correo no puede superar 254 caracteres.")]
    [RegularExpression(@"^\s*[^@\s]+@[^@\s]+\.[A-Za-z]{2,}\s*$", ErrorMessage = "El correo electrónico no es válido.")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La carrera universitaria es obligatoria.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "La carrera debe tener entre 2 y 150 caracteres.")]
    [RegularExpression(@"^\s*\p{L}[\p{L}\p{M}0-9 ,.()&/-]*$",
        ErrorMessage = "La carrera solo puede contener letras, números y signos básicos de puntuación.")]
    public string Carrera { get; set; } = string.Empty;

    [Required(ErrorMessage = "El semestre actual es obligatorio.")]
    [Range(1, 12, ErrorMessage = "El semestre debe estar entre 1 y 12.")]
    public int? Semestre { get; set; }

    [Required(ErrorMessage = "La hoja de vida (PDF) es obligatoria.")]
    [ArchivoPdf]
    public IFormFile? HojaDeVida { get; set; }

    /// <summary>Autorización de tratamiento de datos personales (Ley 1581 de 2012).</summary>
    [Range(typeof(bool), "true", "true",
        ErrorMessage = "Debes autorizar el tratamiento de tus datos personales para registrarte.")]
    public bool AutorizaTratamientoDatos { get; set; }

    /// <summary>Convierte la solicitud HTTP (ya validada) en el comando de la aplicación.</summary>
    public RegistrarPracticanteComando AComando() => new(
        NombreCompleto,
        Correo,
        Carrera,
        Semestre!.Value,
        new ArchivoSubido(HojaDeVida!.FileName, HojaDeVida.Length, HojaDeVida.OpenReadStream));
}

/// <summary>Calificación enviada por el analista; cada criterio de 1 a 5.</summary>
public sealed class CalificacionRequest
{
    private const string Mensaje = "Califica este criterio de 1 a 5.";

    [Required(ErrorMessage = Mensaje), Range(1, 5, ErrorMessage = Mensaje)]
    public int? Ortografia { get; set; }

    [Required(ErrorMessage = Mensaje), Range(1, 5, ErrorMessage = Mensaje)]
    public int? Presentacion { get; set; }

    [Required(ErrorMessage = Mensaje), Range(1, 5, ErrorMessage = Mensaje)]
    public int? Formacion { get; set; }

    [Required(ErrorMessage = Mensaje), Range(1, 5, ErrorMessage = Mensaje)]
    public int? Experiencia { get; set; }

    [Required(ErrorMessage = Mensaje), Range(1, 5, ErrorMessage = Mensaje)]
    public int? Habilidades { get; set; }

    public Calificacion ADominio() =>
        new(Ortografia ?? 0, Presentacion ?? 0, Formacion ?? 0, Experiencia ?? 0, Habilidades ?? 0);
}

/// <summary>Evaluación completa de la hoja de vida: rúbrica, decisión y argumentos.</summary>
public sealed class EvaluarPracticanteRequest : IValidatableObject
{
    [Required(ErrorMessage = "Selecciona la decisión: 'Viable' o 'NoViable'.")]
    [AllowedValues(EstadoPracticante.Viable, EstadoPracticante.NoViable,
        ErrorMessage = "La decisión debe ser 'Viable' o 'NoViable'.")]
    public EstadoPracticante? Decision { get; set; }

    [Required(ErrorMessage = "La calificación de la rúbrica es obligatoria.")]
    public CalificacionRequest? Calificacion { get; set; }

    [MaxLength(7, ErrorMessage = "Hay demasiados motivos de rechazo.")]
    public IList<MotivoRechazo> MotivosRechazo { get; init; } = [];

    [StringLength(ReglasEvaluacion.LongitudMaximaObservaciones,
        ErrorMessage = "Las observaciones no pueden superar 1000 caracteres.")]
    public string? Observaciones { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Las reglas de negocio se evalúan cuando los datos básicos ya son válidos.
        if (Decision is null || Calificacion is null || !Calificacion.ADominio().EsValida)
        {
            yield break;
        }

        var errores = ReglasEvaluacion.Validar(Decision.Value, Calificacion.ADominio(), [.. MotivosRechazo], Observaciones);
        foreach (var (campo, mensajes) in errores)
        {
            foreach (var mensaje in mensajes)
            {
                yield return new ValidationResult(mensaje, [campo]);
            }
        }
    }

    public EvaluarPracticanteComando AComando(string evaluador) =>
        new(Decision!.Value, Calificacion!.ADominio(), [.. MotivosRechazo], Observaciones, evaluador);
}

/// <summary>
/// Avance de la evaluación: todos los campos son opcionales porque se envía mientras el analista
/// califica. Solo se valida el formato; las reglas completas se aplican al guardar la evaluación.
/// </summary>
public sealed class BorradorEvaluacionRequest
{
    private const string Mensaje = "Cada criterio se califica de 1 a 5.";

    [Range(1, 5, ErrorMessage = Mensaje)]
    public int? Ortografia { get; set; }

    [Range(1, 5, ErrorMessage = Mensaje)]
    public int? Presentacion { get; set; }

    [Range(1, 5, ErrorMessage = Mensaje)]
    public int? Formacion { get; set; }

    [Range(1, 5, ErrorMessage = Mensaje)]
    public int? Experiencia { get; set; }

    [Range(1, 5, ErrorMessage = Mensaje)]
    public int? Habilidades { get; set; }

    [AllowedValues(null, EstadoPracticante.Viable, EstadoPracticante.NoViable,
        ErrorMessage = "La decisión debe ser 'Viable' o 'NoViable'.")]
    public EstadoPracticante? Decision { get; set; }

    [MaxLength(7, ErrorMessage = "Hay demasiados motivos de rechazo.")]
    public IList<MotivoRechazo> MotivosRechazo { get; init; } = [];

    [StringLength(ReglasEvaluacion.LongitudMaximaObservaciones,
        ErrorMessage = "Las observaciones no pueden superar 1000 caracteres.")]
    public string? Observaciones { get; set; }

    public GuardarBorradorComando AComando(string evaluador) => new(
        new CalificacionParcial(Ortografia, Presentacion, Formacion, Experiencia, Habilidades),
        Decision,
        [.. MotivosRechazo],
        Observaciones,
        evaluador);
}
