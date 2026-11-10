using Practicas.Domain;

namespace Practicas.Application.Practicantes;

/// <summary>Casos de uso de la selección de practicantes.</summary>
public interface IPracticanteService
{
    Task<RegistroPracticanteResponse> RegistrarAsync(RegistrarPracticanteComando comando, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PracticanteResponse>> ListarAsync(EstadoPracticante? estado, string? busqueda, CancellationToken cancellationToken = default);

    Task<PracticanteResponse?> ObtenerAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Guarda la evaluación con su rúbrica; <c>null</c> si el practicante no existe.</summary>
    Task<PracticanteResponse?> EvaluarAsync(int id, EvaluarPracticanteComando comando, CancellationToken cancellationToken = default);

    /// <summary>Guarda el avance de la evaluación sin decidir; <c>null</c> si el practicante no existe.</summary>
    Task<BorradorResponse?> GuardarBorradorAsync(int id, GuardarBorradorComando comando, CancellationToken cancellationToken = default);

    /// <summary>Descarta el avance guardado; <c>false</c> si el practicante no existe.</summary>
    Task<bool> DescartarBorradorAsync(int id, CancellationToken cancellationToken = default);

    Task<HojaDeVidaArchivo?> ObtenerHojaDeVidaAsync(int id, CancellationToken cancellationToken = default);
}
