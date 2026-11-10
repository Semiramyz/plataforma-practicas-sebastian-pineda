namespace Practicas.Domain;

/// <summary>
/// Entrada inmutable del historial de auditoría: cada vez que un analista evalúa (o corrige
/// una evaluación) queda constancia de quién decidió, cuándo, qué decidió y con qué puntaje.
/// </summary>
public sealed class RegistroEvaluacion
{
    // Constructor para EF Core.
    private RegistroEvaluacion()
    {
    }

    internal RegistroEvaluacion(EstadoPracticante decision, int puntajeTotal, string evaluador, DateTime fechaUtc)
    {
        Decision = decision;
        PuntajeTotal = puntajeTotal;
        Evaluador = evaluador;
        FechaUtc = fechaUtc;
    }

    public EstadoPracticante Decision { get; private set; }

    public int PuntajeTotal { get; private set; }

    public string Evaluador { get; private set; } = string.Empty;

    public DateTime FechaUtc { get; private set; }
}
