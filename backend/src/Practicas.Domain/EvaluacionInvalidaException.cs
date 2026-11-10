namespace Practicas.Domain;

/// <summary>La evaluación no cumple las <see cref="ReglasEvaluacion"/>; trae los errores por campo.</summary>
public sealed class EvaluacionInvalidaException : Exception
{
    public EvaluacionInvalidaException(IReadOnlyDictionary<string, string[]> errores)
        : base("La evaluación no cumple las reglas de negocio.")
    {
        Errores = errores;
    }

    public IReadOnlyDictionary<string, string[]> Errores { get; }
}
