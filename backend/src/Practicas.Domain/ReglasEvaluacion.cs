namespace Practicas.Domain;

/// <summary>
/// Reglas que debe cumplir una evaluación para que el analista no pueda decidir "sin razón":
/// <list type="bullet">
/// <item>Todos los criterios de la rúbrica se califican de 1 a 5.</item>
/// <item>"No viable" exige al menos un motivo de rechazo; "Viable" no admite motivos.</item>
/// <item>El motivo "Otro" exige explicarlo en las observaciones.</item>
/// <item>Si la decisión contradice la recomendación de la rúbrica, hay que justificarla.</item>
/// </list>
/// </summary>
public static class ReglasEvaluacion
{
    public const int LongitudMaximaObservaciones = 1000;
    public const int LongitudMinimaJustificacion = 20;

    /// <summary>Devuelve los errores encontrados por campo; vacío si la evaluación es válida.</summary>
    public static Dictionary<string, string[]> Validar(
        EstadoPracticante decision,
        Calificacion calificacion,
        IReadOnlyCollection<MotivoRechazo> motivos,
        string? observaciones)
    {
        var errores = new Dictionary<string, string[]>();
        var textoObservaciones = observaciones?.Trim() ?? string.Empty;

        if (decision is not (EstadoPracticante.Viable or EstadoPracticante.NoViable))
        {
            errores["Decision"] = ["La decisión debe ser 'Viable' o 'NoViable'."];
        }

        foreach (var (criterio, puntaje) in calificacion.Criterios)
        {
            if (puntaje is < Calificacion.Minimo or > Calificacion.Maximo)
            {
                errores[$"Calificacion.{criterio}"] = ["Cada criterio se califica de 1 a 5."];
            }
        }

        if (motivos.Any(m => !Enum.IsDefined(m)))
        {
            errores["MotivosRechazo"] = ["Hay un motivo de rechazo no válido."];
        }
        else if (decision == EstadoPracticante.NoViable && motivos.Count == 0)
        {
            errores["MotivosRechazo"] = ["Para marcar 'No viable' selecciona al menos un motivo de rechazo."];
        }
        else if (decision == EstadoPracticante.Viable && motivos.Count > 0)
        {
            errores["MotivosRechazo"] = ["Un practicante viable no puede tener motivos de rechazo."];
        }

        if (textoObservaciones.Length > LongitudMaximaObservaciones)
        {
            errores["Observaciones"] = [$"Las observaciones no pueden superar {LongitudMaximaObservaciones} caracteres."];
        }
        else if (motivos.Contains(MotivoRechazo.Otro) && textoObservaciones.Length == 0)
        {
            errores["Observaciones"] = ["Explica en las observaciones el motivo 'Otro'."];
        }
        else if (calificacion.EsValida && errores.Count == 0
                 && decision != calificacion.Recomendacion
                 && textoObservaciones.Length < LongitudMinimaJustificacion)
        {
            errores["Observaciones"] =
            [
                $"La decisión no coincide con la recomendación de la rúbrica; justifícala en las observaciones (mínimo {LongitudMinimaJustificacion} caracteres)."
            ];
        }

        return errores;
    }
}
