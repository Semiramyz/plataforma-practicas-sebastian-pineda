using Practicas.Domain;

namespace Practicas.Domain.Tests;

public class PracticanteTests
{
    private static readonly DateTime Fecha = new(2026, 11, 8, 15, 0, 0, DateTimeKind.Utc);
    private static readonly Calificacion Buena = new(4, 4, 4, 4, 4);

    private static Practicante Nuevo() => new()
    {
        NombreCompleto = "Ana Gómez",
        Correo = "ana@correo.com",
        Carrera = "Ingeniería de Sistemas",
        Semestre = 7,
        HojaDeVidaNombreOriginal = "cv.pdf",
        HojaDeVidaArchivo = "abc.pdf"
    };

    [Fact]
    public void NuevoPracticante_IniciaPendienteSinEvaluacion()
    {
        var practicante = Nuevo();

        Assert.Equal(EstadoPracticante.Pendiente, practicante.Estado);
        Assert.Null(practicante.FechaEvaluacionUtc);
        Assert.Null(practicante.Evaluacion);
    }

    [Fact]
    public void Evaluar_Viable_GuardaRubricaEvaluadorYFecha()
    {
        var practicante = Nuevo();

        practicante.Evaluar(EstadoPracticante.Viable, Buena, [], "  ", "Analista", Fecha);

        Assert.Equal(EstadoPracticante.Viable, practicante.Estado);
        Assert.Equal(Fecha, practicante.FechaEvaluacionUtc);
        Assert.Equal(Buena, practicante.Evaluacion!.Calificacion);
        Assert.Equal("Analista", practicante.Evaluacion.Evaluador);
        Assert.Null(practicante.Evaluacion.Observaciones);
    }

    [Fact]
    public void Evaluar_NoViable_GuardaMotivosSinDuplicados()
    {
        var practicante = Nuevo();

        practicante.Evaluar(EstadoPracticante.NoViable, new Calificacion(1, 3, 3, 3, 3),
            [MotivoRechazo.FormatoInadecuado, MotivoRechazo.ErroresOrtograficos, MotivoRechazo.FormatoInadecuado],
            null, "Analista", Fecha);

        Assert.Equal([MotivoRechazo.ErroresOrtograficos, MotivoRechazo.FormatoInadecuado], practicante.Evaluacion!.MotivosRechazo);
    }

    [Fact]
    public void Evaluar_SinSustento_LanzaExcepcionYNoCambiaElEstado()
    {
        var practicante = Nuevo();

        var error = Assert.Throws<EvaluacionInvalidaException>(() =>
            practicante.Evaluar(EstadoPracticante.NoViable, Buena, [], null, "Analista", Fecha));

        Assert.Contains("MotivosRechazo", error.Errores.Keys);
        Assert.Equal(EstadoPracticante.Pendiente, practicante.Estado);
        Assert.Null(practicante.Evaluacion);
    }

    [Fact]
    public void Evaluar_ComoPendiente_LanzaExcepcion()
    {
        Assert.Throws<EvaluacionInvalidaException>(() =>
            Nuevo().Evaluar(EstadoPracticante.Pendiente, Buena, [], null, "Analista", Fecha));
    }

    [Fact]
    public void Historial_GuardaCadaEvaluacionYNoSeReescribe()
    {
        var practicante = Nuevo();
        practicante.Evaluar(EstadoPracticante.NoViable, new Calificacion(1, 3, 3, 3, 3),
            [MotivoRechazo.ErroresOrtograficos], null, "Analista A", Fecha);

        practicante.Evaluar(EstadoPracticante.Viable, Buena, [], null, "Analista B", Fecha.AddHours(1));

        Assert.Collection(practicante.Historial,
            primera => Assert.Equal((EstadoPracticante.NoViable, 40, "Analista A"), (primera.Decision, primera.PuntajeTotal, primera.Evaluador)),
            segunda => Assert.Equal((EstadoPracticante.Viable, 75, "Analista B"), (segunda.Decision, segunda.PuntajeTotal, segunda.Evaluador)));
    }

    [Fact]
    public void Historial_UnaEvaluacionRechazadaNoDejaRastro()
    {
        var practicante = Nuevo();

        Assert.Throws<EvaluacionInvalidaException>(() =>
            practicante.Evaluar(EstadoPracticante.NoViable, Buena, [], null, "Analista", Fecha));

        Assert.Empty(practicante.Historial);
    }

    [Fact]
    public void Borrador_GuardaElAvanceSinCambiarElEstado()
    {
        var practicante = Nuevo();

        practicante.GuardarBorrador(new CalificacionParcial(2, 4, null, null, null), EstadoPracticante.NoViable,
            [MotivoRechazo.ErroresOrtograficos], "Tiene varias tildes faltantes", "Analista", Fecha);

        Assert.Equal(EstadoPracticante.Pendiente, practicante.Estado);
        Assert.Null(practicante.Evaluacion);
        Assert.Empty(practicante.Historial);
        var borrador = practicante.Borrador!;
        Assert.Equal(new CalificacionParcial(2, 4, null, null, null), borrador.Calificacion);
        Assert.Equal(EstadoPracticante.NoViable, borrador.Decision);
        Assert.Equal("Tiene varias tildes faltantes", borrador.Observaciones);
        Assert.Equal(Fecha, borrador.FechaActualizacionUtc);
    }

    [Fact]
    public void Borrador_SeDescartaAlGuardarLaEvaluacionDefinitiva()
    {
        var practicante = Nuevo();
        practicante.GuardarBorrador(new CalificacionParcial(4, 4, 4, 4, null), null, [], null, "Analista", Fecha);

        practicante.Evaluar(EstadoPracticante.Viable, Buena, [], null, "Analista", Fecha.AddMinutes(5));

        Assert.Null(practicante.Borrador);
        Assert.NotNull(practicante.Evaluacion);
    }

    [Fact]
    public void Borrador_ConPuntajeFueraDeRango_EsInvalido()
    {
        var practicante = Nuevo();

        var error = Assert.Throws<EvaluacionInvalidaException>(() => practicante.GuardarBorrador(
            new CalificacionParcial(7, null, null, null, null), null, [], null, "Analista", Fecha));

        Assert.Contains("Calificacion.Ortografia", error.Errores.Keys);
        Assert.Null(practicante.Borrador);
    }
}
