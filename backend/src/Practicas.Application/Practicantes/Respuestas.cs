using Practicas.Domain;

namespace Practicas.Application.Practicantes;

public sealed record RegistroPracticanteResponse(int Id, string NombreCompleto, DateTime FechaRegistroUtc);

public sealed record CalificacionDto(int Ortografia, int Presentacion, int Formacion, int Experiencia, int Habilidades);

public sealed record EvaluacionResponse(
    CalificacionDto Calificacion,
    decimal Promedio,
    int PuntajeTotal,
    EstadoPracticante Recomendacion,
    IReadOnlyList<MotivoRechazo> MotivosRechazo,
    string? Observaciones,
    string Evaluador)
{
    public static EvaluacionResponse Desde(Evaluacion evaluacion)
    {
        ArgumentNullException.ThrowIfNull(evaluacion);
        var calificacion = evaluacion.Calificacion;
        return new EvaluacionResponse(
            new CalificacionDto(calificacion.Ortografia, calificacion.Presentacion, calificacion.Formacion,
                calificacion.Experiencia, calificacion.Habilidades),
            calificacion.Promedio,
            calificacion.PuntajeTotal,
            calificacion.Recomendacion,
            evaluacion.MotivosRechazo,
            evaluacion.Observaciones,
            evaluacion.Evaluador);
    }
}

public sealed record CalificacionParcialDto(int? Ortografia, int? Presentacion, int? Formacion, int? Experiencia, int? Habilidades);

/// <summary>Evaluación en curso guardada automáticamente.</summary>
public sealed record BorradorResponse(
    CalificacionParcialDto Calificacion,
    EstadoPracticante? Decision,
    IReadOnlyList<MotivoRechazo> MotivosRechazo,
    string? Observaciones,
    string Evaluador,
    DateTime FechaActualizacionUtc)
{
    public static BorradorResponse Desde(BorradorEvaluacion borrador)
    {
        ArgumentNullException.ThrowIfNull(borrador);
        return new BorradorResponse(
            new CalificacionParcialDto(borrador.Ortografia, borrador.Presentacion, borrador.Formacion,
                borrador.Experiencia, borrador.Habilidades),
            borrador.Decision,
            borrador.MotivosRechazo,
            borrador.Observaciones,
            borrador.Evaluador,
            borrador.FechaActualizacionUtc);
    }
}

/// <summary>Entrada del historial de auditoría de evaluaciones.</summary>
public sealed record RegistroEvaluacionResponse(EstadoPracticante Decision, int PuntajeTotal, string Evaluador, DateTime FechaUtc);

public sealed record PracticanteResponse(
    int Id,
    string NombreCompleto,
    string Correo,
    string Carrera,
    int Semestre,
    EstadoPracticante Estado,
    DateTime FechaRegistroUtc,
    DateTime? FechaEvaluacionUtc,
    string HojaDeVidaNombre,
    long HojaDeVidaTamanoBytes,
    EvaluacionResponse? Evaluacion,
    BorradorResponse? Borrador,
    IReadOnlyList<RegistroEvaluacionResponse> Historial)
{
    public static PracticanteResponse Desde(Practicante practicante)
    {
        ArgumentNullException.ThrowIfNull(practicante);
        return new PracticanteResponse(
            practicante.Id,
            practicante.NombreCompleto,
            practicante.Correo,
            practicante.Carrera,
            practicante.Semestre,
            practicante.Estado,
            practicante.FechaRegistroUtc,
            practicante.FechaEvaluacionUtc,
            practicante.HojaDeVidaNombreOriginal,
            practicante.HojaDeVidaTamanoBytes,
            practicante.Evaluacion is null ? null : EvaluacionResponse.Desde(practicante.Evaluacion),
            practicante.Borrador is null ? null : BorradorResponse.Desde(practicante.Borrador),
            practicante.Historial
                .OrderByDescending(h => h.FechaUtc)
                .Select(h => new RegistroEvaluacionResponse(h.Decision, h.PuntajeTotal, h.Evaluador, h.FechaUtc))
                .ToList());
    }
}

/// <summary>Hoja de vida lista para enviarse al cliente.</summary>
public sealed record HojaDeVidaArchivo(Stream Contenido, string NombreDescarga);
