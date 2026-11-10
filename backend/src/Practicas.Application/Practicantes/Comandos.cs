using Practicas.Domain;

namespace Practicas.Application.Practicantes;

/// <summary>Archivo recibido, independiente de la tecnología (HTTP) que lo trajo.</summary>
public sealed record ArchivoSubido(string Nombre, long TamanoBytes, Func<Stream> Abrir);

/// <summary>Datos validados del formulario de registro.</summary>
public sealed record RegistrarPracticanteComando(
    string NombreCompleto,
    string Correo,
    string Carrera,
    int Semestre,
    ArchivoSubido HojaDeVida);

/// <summary>Avance de una evaluación en curso (guardado automático mientras se califica).</summary>
public sealed record GuardarBorradorComando(
    CalificacionParcial Calificacion,
    EstadoPracticante? Decision,
    IReadOnlyCollection<MotivoRechazo> MotivosRechazo,
    string? Observaciones,
    string Evaluador);

/// <summary>Evaluación de la hoja de vida hecha por un analista.</summary>
public sealed record EvaluarPracticanteComando(
    EstadoPracticante Decision,
    Calificacion Calificacion,
    IReadOnlyCollection<MotivoRechazo> MotivosRechazo,
    string? Observaciones,
    string Evaluador);
