namespace Practicas.Api.Domain;

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

    public EstadoPracticante Estado { get; private set; } = EstadoPracticante.Pendiente;

    public DateTime FechaRegistroUtc { get; set; }

    public DateTime? FechaEvaluacionUtc { get; private set; }

    /// <summary>
    /// Registra la decisión del analista. Solo se admite "Viable" o "No viable".
    /// </summary>
    public void Evaluar(EstadoPracticante decision, DateTime fechaUtc)
    {
        if (decision == EstadoPracticante.Pendiente)
        {
            throw new ArgumentException(
                "La evaluación debe ser Viable o NoViable.", nameof(decision));
        }

        Estado = decision;
        FechaEvaluacionUtc = fechaUtc;
    }
}
