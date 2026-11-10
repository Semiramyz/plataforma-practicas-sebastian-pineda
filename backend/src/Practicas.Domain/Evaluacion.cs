namespace Practicas.Domain;

/// <summary>
/// Evaluación de la hoja de vida hecha por un analista. Se guarda junto al practicante
/// para dejar trazabilidad de quién decidió, cuándo y con qué argumentos.
/// </summary>
public class Evaluacion
{
    // Constructor para EF Core.
    private Evaluacion()
    {
    }

    internal Evaluacion(Calificacion calificacion, IEnumerable<MotivoRechazo> motivos, string? observaciones, string evaluador)
    {
        Ortografia = calificacion.Ortografia;
        Presentacion = calificacion.Presentacion;
        Formacion = calificacion.Formacion;
        Experiencia = calificacion.Experiencia;
        Habilidades = calificacion.Habilidades;
        MotivosRechazo = motivos.Distinct().Order().ToList();
        Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
        Evaluador = evaluador;
    }

    public int Ortografia { get; private set; }

    public int Presentacion { get; private set; }

    public int Formacion { get; private set; }

    public int Experiencia { get; private set; }

    public int Habilidades { get; private set; }

    public List<MotivoRechazo> MotivosRechazo { get; private set; } = [];

    public string? Observaciones { get; private set; }

    public string Evaluador { get; private set; } = string.Empty;

    public Calificacion Calificacion => new(Ortografia, Presentacion, Formacion, Experiencia, Habilidades);
}
