namespace Practicas.Domain;

/// <summary>Calificación a medio hacer: cada criterio puede seguir sin calificar (<c>null</c>).</summary>
public sealed record CalificacionParcial(int? Ortografia, int? Presentacion, int? Formacion, int? Experiencia, int? Habilidades)
{
    public IEnumerable<(string Criterio, int? Puntaje)> Criterios =>
    [
        (nameof(Ortografia), Ortografia),
        (nameof(Presentacion), Presentacion),
        (nameof(Formacion), Formacion),
        (nameof(Experiencia), Experiencia),
        (nameof(Habilidades), Habilidades)
    ];
}

/// <summary>
/// Evaluación en curso que se guarda automáticamente mientras el analista califica. No cambia el
/// estado del practicante ni cuenta como decisión: solo evita perder el trabajo si se sale de la
/// página. Se descarta al guardar la evaluación definitiva.
/// </summary>
public sealed class BorradorEvaluacion
{
    // Constructor para EF Core.
    private BorradorEvaluacion()
    {
    }

    internal BorradorEvaluacion(
        CalificacionParcial calificacion,
        EstadoPracticante? decision,
        IEnumerable<MotivoRechazo> motivos,
        string? observaciones,
        string evaluador,
        DateTime fechaUtc)
    {
        Ortografia = calificacion.Ortografia;
        Presentacion = calificacion.Presentacion;
        Formacion = calificacion.Formacion;
        Experiencia = calificacion.Experiencia;
        Habilidades = calificacion.Habilidades;
        Decision = decision;
        MotivosRechazo = motivos.Distinct().Order().ToList();
        Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones;
        Evaluador = evaluador;
        FechaActualizacionUtc = fechaUtc;
    }

    public int? Ortografia { get; private set; }

    public int? Presentacion { get; private set; }

    public int? Formacion { get; private set; }

    public int? Experiencia { get; private set; }

    public int? Habilidades { get; private set; }

    public EstadoPracticante? Decision { get; private set; }

    public List<MotivoRechazo> MotivosRechazo { get; private set; } = [];

    public string? Observaciones { get; private set; }

    public string Evaluador { get; private set; } = string.Empty;

    public DateTime FechaActualizacionUtc { get; private set; }

    public CalificacionParcial Calificacion => new(Ortografia, Presentacion, Formacion, Experiencia, Habilidades);

    /// <summary>Valida solo el formato (rangos y longitudes); las reglas completas se aplican al decidir.</summary>
    internal static Dictionary<string, string[]> Validar(
        CalificacionParcial calificacion,
        EstadoPracticante? decision,
        IReadOnlyCollection<MotivoRechazo> motivos,
        string? observaciones)
    {
        var errores = new Dictionary<string, string[]>();

        foreach (var (criterio, puntaje) in calificacion.Criterios)
        {
            if (puntaje is < Domain.Calificacion.Minimo or > Domain.Calificacion.Maximo)
            {
                errores[$"Calificacion.{criterio}"] = ["Cada criterio se califica de 1 a 5."];
            }
        }

        if (decision is not (null or EstadoPracticante.Viable or EstadoPracticante.NoViable))
        {
            errores["Decision"] = ["La decisión debe ser 'Viable' o 'NoViable'."];
        }

        if (motivos.Any(m => !Enum.IsDefined(m)))
        {
            errores["MotivosRechazo"] = ["Hay un motivo de rechazo no válido."];
        }

        if ((observaciones?.Length ?? 0) > ReglasEvaluacion.LongitudMaximaObservaciones)
        {
            errores["Observaciones"] = [$"Las observaciones no pueden superar {ReglasEvaluacion.LongitudMaximaObservaciones} caracteres."];
        }

        return errores;
    }
}
