using Practicas.Domain;

namespace Practicas.Application.Abstracciones;

/// <summary>Puerto de persistencia de practicantes (implementado con EF Core en Infraestructura).</summary>
public interface IPracticanteRepositorio
{
    Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default);

    /// <summary>Obtiene el practicante para modificarlo; <c>null</c> si no existe.</summary>
    Task<Practicante?> ObtenerAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Lista de solo lectura, de la más reciente a la más antigua.</summary>
    Task<IReadOnlyList<Practicante>> ListarAsync(
        EstadoPracticante? estado, string? busqueda, CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda un practicante nuevo. Lanza <see cref="Errores.CorreoDuplicadoException"/> si el
    /// índice único de correo detecta un duplicado (por ejemplo, dos registros simultáneos).
    /// </summary>
    Task AgregarAsync(Practicante practicante, CancellationToken cancellationToken = default);

    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
