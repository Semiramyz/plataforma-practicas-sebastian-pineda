using Microsoft.Extensions.Logging.Abstractions;
using Practicas.Application.Abstracciones;
using Practicas.Application.Errores;
using Practicas.Application.Practicantes;
using Practicas.Domain;

namespace Practicas.Application.Tests;

/// <summary>
/// Pruebas del caso de uso con dobles en memoria: no hace falta base de datos ni disco,
/// que es justamente lo que permite la separación por puertos de la arquitectura.
/// </summary>
public class PracticanteServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 9, 15, 0, 0, TimeSpan.Zero);

    private readonly RepositorioEnMemoria _repositorio = new();
    private readonly AlmacenamientoEnMemoria _almacenamiento = new();
    private readonly PracticanteService _servicio;

    public PracticanteServiceTests()
    {
        _servicio = new PracticanteService(_repositorio, _almacenamiento, new RelojFijo(Ahora),
            NullLogger<PracticanteService>.Instance);
    }

    private static RegistrarPracticanteComando Comando(string correo = "Ana.Gomez@Correo.com ", string nombre = "ANA GÓMEZ") =>
        new(nombre, correo, "  Ingeniería   de Sistemas ", 8,
            new ArchivoSubido("CV Ana.pdf", 4, () => new MemoryStream("%PDF"u8.ToArray())));

    [Fact]
    public async Task Registrar_NormalizaDatosGuardaArchivoYFechas()
    {
        var registro = await _servicio.RegistrarAsync(Comando());

        var guardado = Assert.Single(_repositorio.Practicantes);
        Assert.Equal("Ana Gómez", registro.NombreCompleto);
        Assert.Equal("ana.gomez@correo.com", guardado.Correo);
        Assert.Equal("Ingeniería de Sistemas", guardado.Carrera);
        Assert.Equal("CV Ana.pdf", guardado.HojaDeVidaNombreOriginal);
        Assert.Equal(Ahora.UtcDateTime, guardado.FechaRegistroUtc);
        Assert.Equal(Ahora.UtcDateTime, guardado.FechaAutorizacionDatosUtc);
        Assert.True(_almacenamiento.Archivos.ContainsKey(guardado.HojaDeVidaArchivo));
    }

    [Fact]
    public async Task Registrar_CorreoExistente_NoGuardaNadaNiElArchivo()
    {
        await _servicio.RegistrarAsync(Comando());

        await Assert.ThrowsAsync<CorreoDuplicadoException>(() => _servicio.RegistrarAsync(Comando(" ANA.GOMEZ@correo.com")));

        Assert.Single(_repositorio.Practicantes);
        Assert.Single(_almacenamiento.Archivos);
    }

    [Fact]
    public async Task Registrar_SiFallaLaBaseDeDatos_EliminaElArchivoParaNoDejarHuerfanos()
    {
        _repositorio.FallarAlAgregar = true;

        await Assert.ThrowsAsync<CorreoDuplicadoException>(() => _servicio.RegistrarAsync(Comando()));

        Assert.Empty(_almacenamiento.Archivos);
    }

    [Fact]
    public async Task Evaluar_AplicaLaRubricaYRegistraElHistorial()
    {
        var registro = await _servicio.RegistrarAsync(Comando());

        var resultado = await _servicio.EvaluarAsync(registro.Id, new EvaluarPracticanteComando(
            EstadoPracticante.NoViable, new Calificacion(1, 3, 3, 3, 3), [MotivoRechazo.ErroresOrtograficos], null, "Analista"));

        Assert.Equal(EstadoPracticante.NoViable, resultado!.Estado);
        Assert.Equal(40, resultado.Evaluacion!.PuntajeTotal);
        var entrada = Assert.Single(resultado.Historial);
        Assert.Equal(("Analista", Ahora.UtcDateTime), (entrada.Evaluador, entrada.FechaUtc));
        Assert.Equal(1, _repositorio.CambiosGuardados);
    }

    [Fact]
    public async Task Evaluar_SinSustento_NoGuardaCambios()
    {
        var registro = await _servicio.RegistrarAsync(Comando());

        await Assert.ThrowsAsync<EvaluacionInvalidaException>(() => _servicio.EvaluarAsync(registro.Id,
            new EvaluarPracticanteComando(EstadoPracticante.NoViable, new Calificacion(4, 4, 4, 4, 4), [], null, "Analista")));

        Assert.Equal(0, _repositorio.CambiosGuardados);
    }

    [Fact]
    public async Task OperacionesSobrePracticanteInexistente_DevuelvenNull()
    {
        Assert.Null(await _servicio.ObtenerAsync(99));
        Assert.Null(await _servicio.ObtenerHojaDeVidaAsync(99));
        Assert.Null(await _servicio.EvaluarAsync(99, new EvaluarPracticanteComando(
            EstadoPracticante.Viable, new Calificacion(4, 4, 4, 4, 4), [], null, "Analista")));
        Assert.Null(await _servicio.GuardarBorradorAsync(99, new GuardarBorradorComando(
            new CalificacionParcial(3, null, null, null, null), null, [], null, "Analista")));
        Assert.False(await _servicio.DescartarBorradorAsync(99));
    }

    [Fact]
    public async Task GuardarBorrador_PersisteElAvanceConFecha()
    {
        var registro = await _servicio.RegistrarAsync(Comando());

        var borrador = await _servicio.GuardarBorradorAsync(registro.Id, new GuardarBorradorComando(
            new CalificacionParcial(3, 4, null, null, null), null, [], "Va bien", "Analista"));

        Assert.Equal(new CalificacionParcialDto(3, 4, null, null, null), borrador!.Calificacion);
        Assert.Equal(Ahora.UtcDateTime, borrador.FechaActualizacionUtc);
        Assert.Equal(1, _repositorio.CambiosGuardados);
        Assert.Equal(EstadoPracticante.Pendiente, (await _servicio.ObtenerAsync(registro.Id))!.Estado);
    }

    // ---------- Dobles de prueba ----------

    private sealed class RepositorioEnMemoria : IPracticanteRepositorio
    {
        public List<Practicante> Practicantes { get; } = [];

        public bool FallarAlAgregar { get; set; }

        public int CambiosGuardados { get; private set; }

        public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default) =>
            Task.FromResult(Practicantes.Exists(p => p.Correo == correo));

        public Task<Practicante?> ObtenerAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Practicantes.Find(p => p.Id == id));

        public Task<IReadOnlyList<Practicante>> ListarAsync(EstadoPracticante? estado, string? busqueda, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Practicante>>(Practicantes);

        public Task AgregarAsync(Practicante practicante, CancellationToken cancellationToken = default)
        {
            if (FallarAlAgregar)
            {
                throw new CorreoDuplicadoException(practicante.Correo);
            }

            practicante.Id = Practicantes.Count + 1;
            Practicantes.Add(practicante);
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync(CancellationToken cancellationToken = default)
        {
            CambiosGuardados++;
            return Task.CompletedTask;
        }
    }

    private sealed class AlmacenamientoEnMemoria : IAlmacenamientoArchivos
    {
        public Dictionary<string, byte[]> Archivos { get; } = [];

        public async Task<string> GuardarAsync(Stream contenido, string extension, CancellationToken cancellationToken = default)
        {
            using var copia = new MemoryStream();
            await contenido.CopyToAsync(copia, cancellationToken);
            var nombre = $"{Guid.NewGuid():N}{extension}";
            Archivos[nombre] = copia.ToArray();
            return nombre;
        }

        public Stream? Abrir(string nombreArchivo) =>
            Archivos.TryGetValue(nombreArchivo, out var datos) ? new MemoryStream(datos) : null;

        public void Eliminar(string nombreArchivo) => Archivos.Remove(nombreArchivo);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
