using Microsoft.Extensions.Logging;
using Practicas.Application.Abstracciones;
using Practicas.Application.Errores;
using Practicas.Application.Validacion;
using Practicas.Domain;

namespace Practicas.Application.Practicantes;

/// <summary>
/// Casos de uso de la selección. Solo conoce el dominio y los puertos: no sabe si los datos van a
/// SQLite ni si los archivos van a disco, lo que permite probarlo con dobles en memoria.
/// </summary>
public sealed partial class PracticanteService(
    IPracticanteRepositorio repositorio,
    IAlmacenamientoArchivos almacenamiento,
    TimeProvider reloj,
    ILogger<PracticanteService> logger) : IPracticanteService
{
    public async Task<RegistroPracticanteResponse> RegistrarAsync(
        RegistrarPracticanteComando comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var correo = NormalizarCorreo(comando.Correo);
        if (await repositorio.ExisteCorreoAsync(correo, cancellationToken))
        {
            throw new CorreoDuplicadoException(correo);
        }

        // Primero se guarda el archivo; si la base de datos falla, se elimina para no dejar huérfanos.
        string archivoGuardado;
        await using (var contenido = comando.HojaDeVida.Abrir())
        {
            archivoGuardado = await almacenamiento.GuardarAsync(contenido, ".pdf", cancellationToken);
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var practicante = new Practicante
        {
            NombreCompleto = ValidadorNombre.NormalizarNombre(comando.NombreCompleto),
            Correo = correo,
            Carrera = ValidadorNombre.NormalizarTexto(comando.Carrera),
            Semestre = comando.Semestre,
            HojaDeVidaNombreOriginal = NombreOriginalSeguro(comando.HojaDeVida.Nombre),
            HojaDeVidaArchivo = archivoGuardado,
            HojaDeVidaTamanoBytes = comando.HojaDeVida.TamanoBytes,
            FechaAutorizacionDatosUtc = ahora,
            FechaRegistroUtc = ahora
        };

        try
        {
            await repositorio.AgregarAsync(practicante, cancellationToken);
        }
        catch
        {
            almacenamiento.Eliminar(archivoGuardado);
            throw;
        }

        LogRegistrado(logger, practicante.Id);
        return new RegistroPracticanteResponse(practicante.Id, practicante.NombreCompleto, practicante.FechaRegistroUtc);
    }

    public async Task<IReadOnlyList<PracticanteResponse>> ListarAsync(
        EstadoPracticante? estado, string? busqueda, CancellationToken cancellationToken = default)
    {
        var practicantes = await repositorio.ListarAsync(estado, busqueda, cancellationToken);
        return practicantes.Select(PracticanteResponse.Desde).ToList();
    }

    public async Task<PracticanteResponse?> ObtenerAsync(int id, CancellationToken cancellationToken = default)
    {
        var practicante = await repositorio.ObtenerAsync(id, cancellationToken);
        return practicante is null ? null : PracticanteResponse.Desde(practicante);
    }

    public async Task<PracticanteResponse?> EvaluarAsync(
        int id, EvaluarPracticanteComando comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var practicante = await repositorio.ObtenerAsync(id, cancellationToken);
        if (practicante is null)
        {
            return null;
        }

        practicante.Evaluar(
            comando.Decision,
            comando.Calificacion,
            comando.MotivosRechazo,
            comando.Observaciones,
            comando.Evaluador,
            reloj.GetUtcNow().UtcDateTime);
        await repositorio.GuardarCambiosAsync(cancellationToken);

        // Evento de auditoría (OWASP A09): quién decidió qué y con qué puntaje.
        LogEvaluado(logger, id, practicante.Estado, comando.Evaluador, comando.Calificacion.PuntajeTotal);
        return PracticanteResponse.Desde(practicante);
    }

    public async Task<BorradorResponse?> GuardarBorradorAsync(
        int id, GuardarBorradorComando comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var practicante = await repositorio.ObtenerAsync(id, cancellationToken);
        if (practicante is null)
        {
            return null;
        }

        practicante.GuardarBorrador(
            comando.Calificacion,
            comando.Decision,
            comando.MotivosRechazo,
            comando.Observaciones,
            comando.Evaluador,
            reloj.GetUtcNow().UtcDateTime);
        await repositorio.GuardarCambiosAsync(cancellationToken);

        return BorradorResponse.Desde(practicante.Borrador!);
    }

    public async Task<bool> DescartarBorradorAsync(int id, CancellationToken cancellationToken = default)
    {
        var practicante = await repositorio.ObtenerAsync(id, cancellationToken);
        if (practicante is null)
        {
            return false;
        }

        if (practicante.Borrador is not null)
        {
            practicante.DescartarBorrador();
            await repositorio.GuardarCambiosAsync(cancellationToken);
        }

        return true;
    }

    public async Task<HojaDeVidaArchivo?> ObtenerHojaDeVidaAsync(int id, CancellationToken cancellationToken = default)
    {
        var practicante = await repositorio.ObtenerAsync(id, cancellationToken);
        if (practicante is null)
        {
            return null;
        }

        var contenido = almacenamiento.Abrir(practicante.HojaDeVidaArchivo);
        if (contenido is null)
        {
            LogArchivoFaltante(logger, id);
            return null;
        }

        return new HojaDeVidaArchivo(contenido, practicante.HojaDeVidaNombreOriginal);
    }

    internal static string NormalizarCorreo(string correo) => correo.Trim().ToLowerInvariant();

    private static string NombreOriginalSeguro(string nombre)
    {
        var limpio = Path.GetFileName(nombre).Trim();
        if (string.IsNullOrEmpty(limpio))
        {
            return "hoja-de-vida.pdf";
        }

        return limpio.Length <= 255 ? limpio : string.Concat(limpio.AsSpan(0, 251), ".pdf");
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Practicante {Id} registrado")]
    private static partial void LogRegistrado(ILogger logger, int id);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information,
        Message = "Auditoría: practicante {Id} evaluado como {Estado} por {Evaluador} (puntaje {Puntaje})")]
    private static partial void LogEvaluado(ILogger logger, int id, EstadoPracticante estado, string evaluador, int puntaje);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning, Message = "No se encontró el archivo de la hoja de vida del practicante {Id}")]
    private static partial void LogArchivoFaltante(ILogger logger, int id);
}
