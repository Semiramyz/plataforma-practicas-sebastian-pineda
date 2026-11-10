namespace Practicas.Domain;

/// <summary>
/// Candidato a práctica profesional que se registró en la plataforma.
/// </summary>
public class Practicante
{
    public int Id { get; set; }

    public required string NombreCompleto { get; set; }

    /// <summary>Correo normalizado (sin espacios y en minúsculas). Es único.</summary>
    public required string Correo { get; set; }

    public required string Carrera { get; set; }

    public int Semestre { get; set; }

    /// <summary>Nombre con el que el practicante subió su hoja de vida.</summary>
    public required string HojaDeVidaNombreOriginal { get; set; }

    /// <summary>Nombre del archivo en la carpeta del servidor (generado, nunca el del usuario).</summary>
    public required string HojaDeVidaArchivo { get; set; }

    public long HojaDeVidaTamanoBytes { get; set; }

    /// <summary>Momento en que el practicante autorizó el tratamiento de sus datos personales.</summary>
    public DateTime FechaAutorizacionDatosUtc { get; set; }

    public EstadoPracticante Estado { get; private set; } = EstadoPracticante.Pendiente;

    public DateTime FechaRegistroUtc { get; set; }

    public DateTime? FechaEvaluacionUtc { get; private set; }

    /// <summary>Última evaluación del analista; <c>null</c> mientras esté pendiente.</summary>
    public Evaluacion? Evaluacion { get; private set; }

    /// <summary>Evaluación en curso guardada automáticamente; <c>null</c> si no hay trabajo sin terminar.</summary>
    public BorradorEvaluacion? Borrador { get; private set; }

    private readonly List<RegistroEvaluacion> _historial = [];

    /// <summary>Historial de auditoría: todas las evaluaciones, incluidas las corregidas. Solo crece.</summary>
    public IReadOnlyList<RegistroEvaluacion> Historial => _historial.AsReadOnly();

    /// <summary>
    /// Registra la decisión del analista con su rúbrica. Lanza <see cref="EvaluacionInvalidaException"/>
    /// si no cumple las <see cref="ReglasEvaluacion"/>; así ninguna capa puede guardar una decisión sin sustento.
    /// </summary>
    public void Evaluar(
        EstadoPracticante decision,
        Calificacion calificacion,
        IReadOnlyCollection<MotivoRechazo> motivos,
        string? observaciones,
        string evaluador,
        DateTime fechaUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evaluador);

        var errores = ReglasEvaluacion.Validar(decision, calificacion, motivos, observaciones);
        if (errores.Count > 0)
        {
            throw new EvaluacionInvalidaException(errores);
        }

        Estado = decision;
        Evaluacion = new Evaluacion(calificacion, motivos, observaciones, evaluador);
        FechaEvaluacionUtc = fechaUtc;
        Borrador = null; // La evaluación definitiva reemplaza al trabajo en curso.
        _historial.Add(new RegistroEvaluacion(decision, calificacion.PuntajeTotal, evaluador, fechaUtc));
    }

    /// <summary>
    /// Guarda el avance de la evaluación (criterios calificados hasta el momento, decisión tentativa,
    /// motivos y comentario) sin cambiar el estado del practicante.
    /// </summary>
    public void GuardarBorrador(
        CalificacionParcial calificacion,
        EstadoPracticante? decision,
        IReadOnlyCollection<MotivoRechazo> motivos,
        string? observaciones,
        string evaluador,
        DateTime fechaUtc)
    {
        ArgumentNullException.ThrowIfNull(calificacion);
        ArgumentException.ThrowIfNullOrWhiteSpace(evaluador);

        var errores = BorradorEvaluacion.Validar(calificacion, decision, motivos, observaciones);
        if (errores.Count > 0)
        {
            throw new EvaluacionInvalidaException(errores);
        }

        Borrador = new BorradorEvaluacion(calificacion, decision, motivos, observaciones, evaluador, fechaUtc);
    }

    /// <summary>Descarta el borrador (por ejemplo, si el analista cancela la edición).</summary>
    public void DescartarBorrador() => Borrador = null;
}
