using Microsoft.EntityFrameworkCore;
using Practicas.Api.Contracts;
using Practicas.Api.Data;
using Practicas.Api.Domain;
using Practicas.Api.Errors;
using Practicas.Api.Storage;

namespace Practicas.Api.Services;

public class PracticanteService(
    PracticasDbContext db,
    IAlmacenamientoArchivos almacenamiento,
    TimeProvider reloj,
    ILogger<PracticanteService> logger) : IPracticanteService
{
    public async Task<RegistroPracticanteResponse> RegistrarAsync(
        RegistrarPracticanteRequest solicitud, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud.HojaDeVida);

        var correo = NormalizarCorreo(solicitud.Correo);
        if (await db.Practicantes.AnyAsync(p => p.Correo == correo, cancellationToken))
        {
            throw new CorreoDuplicadoException(correo);
        }

        // Primero se guarda el archivo; si la base de datos falla, se elimina para no dejar huérfanos.
        string archivoGuardado;
        await using (var contenido = solicitud.HojaDeVida.OpenReadStream())
        {
            archivoGuardado = await almacenamiento.GuardarAsync(contenido, ".pdf", cancellationToken);
        }

        var practicante = new Practicante
        {
            NombreCompleto = NormalizarTexto(solicitud.NombreCompleto),
            Correo = correo,
            Carrera = NormalizarTexto(solicitud.Carrera),
            Semestre = solicitud.Semestre!.Value,
            HojaDeVidaNombreOriginal = NombreOriginalSeguro(solicitud.HojaDeVida.FileName),
            HojaDeVidaArchivo = archivoGuardado,
            HojaDeVidaTamanoBytes = solicitud.HojaDeVida.Length,
            FechaRegistroUtc = reloj.GetUtcNow().UtcDateTime
        };

        try
        {
            db.Practicantes.Add(practicante);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            almacenamiento.Eliminar(archivoGuardado);

            // Dos registros simultáneos con el mismo correo: lo resuelve el índice único.
            if (await db.Practicantes.AsNoTracking().AnyAsync(p => p.Correo == correo, cancellationToken))
            {
                throw new CorreoDuplicadoException(correo);
            }

            throw;
        }

        logger.LogInformation("Practicante {Id} registrado", practicante.Id);
        return new RegistroPracticanteResponse(practicante.Id, practicante.NombreCompleto, practicante.FechaRegistroUtc);
    }

    public async Task<IReadOnlyList<PracticanteResponse>> ListarAsync(
        EstadoPracticante? estado, string? busqueda, CancellationToken cancellationToken = default)
    {
        var consulta = db.Practicantes.AsNoTracking();

        if (estado is not null)
        {
            consulta = consulta.Where(p => p.Estado == estado);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var patron = $"%{busqueda.Trim()}%";
            consulta = consulta.Where(p =>
                EF.Functions.Like(p.NombreCompleto, patron) ||
                EF.Functions.Like(p.Correo, patron) ||
                EF.Functions.Like(p.Carrera, patron));
        }

        var practicantes = await consulta
            .OrderByDescending(p => p.FechaRegistroUtc)
            .ThenByDescending(p => p.Id)
            .ToListAsync(cancellationToken);

        return practicantes.Select(PracticanteResponse.Desde).ToList();
    }

    public async Task<PracticanteResponse?> ObtenerAsync(int id, CancellationToken cancellationToken = default)
    {
        var practicante = await db.Practicantes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return practicante is null ? null : PracticanteResponse.Desde(practicante);
    }

    public async Task<PracticanteResponse?> ActualizarEstadoAsync(
        int id, EstadoPracticante estado, CancellationToken cancellationToken = default)
    {
        var practicante = await db.Practicantes.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (practicante is null)
        {
            return null;
        }

        practicante.Evaluar(estado, reloj.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Practicante {Id} marcado como {Estado}", id, estado);
        return PracticanteResponse.Desde(practicante);
    }

    public async Task<HojaDeVidaArchivo?> ObtenerHojaDeVidaAsync(int id, CancellationToken cancellationToken = default)
    {
        var datos = await db.Practicantes.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new { p.HojaDeVidaArchivo, p.HojaDeVidaNombreOriginal })
            .FirstOrDefaultAsync(cancellationToken);

        if (datos is null)
        {
            return null;
        }

        var contenido = almacenamiento.Abrir(datos.HojaDeVidaArchivo);
        if (contenido is null)
        {
            logger.LogWarning("No se encontró el archivo de la hoja de vida del practicante {Id}", id);
            return null;
        }

        return new HojaDeVidaArchivo(contenido, datos.HojaDeVidaNombreOriginal);
    }

    internal static string NormalizarCorreo(string correo) => correo.Trim().ToLowerInvariant();

    /// <summary>Quita espacios sobrantes, incluidos los repetidos entre palabras.</summary>
    internal static string NormalizarTexto(string texto) =>
        string.Join(' ', texto.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string NombreOriginalSeguro(string nombre)
    {
        var limpio = Path.GetFileName(nombre).Trim();
        if (string.IsNullOrEmpty(limpio))
        {
            return "hoja-de-vida.pdf";
        }

        return limpio.Length <= 255 ? limpio : string.Concat(limpio.AsSpan(0, 251), ".pdf");
    }
}
