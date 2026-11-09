namespace Practicas.Api.Domain;

/// <summary>
/// Estado del proceso de selección de un practicante.
/// </summary>
public enum EstadoPracticante
{
    /// <summary>Registrado, aún sin revisión del analista.</summary>
    Pendiente = 0,

    /// <summary>El analista lo consideró apto para continuar el proceso.</summary>
    Viable = 1,

    /// <summary>El analista lo consideró no apto para continuar el proceso.</summary>
    NoViable = 2
}
