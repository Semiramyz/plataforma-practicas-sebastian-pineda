using Practicas.Api.Contracts;
using Practicas.Api.Domain;

namespace Practicas.Api.Services;

public interface IPracticanteService
{
    Task<RegistroPracticanteResponse> RegistrarAsync(RegistrarPracticanteRequest solicitud, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PracticanteResponse>> ListarAsync(EstadoPracticante? estado, string? busqueda, CancellationToken cancellationToken = default);

    Task<PracticanteResponse?> ObtenerAsync(int id, CancellationToken cancellationToken = default);

    Task<PracticanteResponse?> ActualizarEstadoAsync(int id, EstadoPracticante estado, CancellationToken cancellationToken = default);

    Task<HojaDeVidaArchivo?> ObtenerHojaDeVidaAsync(int id, CancellationToken cancellationToken = default);
}

public sealed record HojaDeVidaArchivo(Stream Contenido, string NombreDescarga);
