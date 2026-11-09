using Practicas.Api.Domain;

namespace Practicas.Api.Tests.Unitarias;

public class PracticanteTests
{
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
    public void NuevoPracticante_IniciaPendienteSinFechaDeEvaluacion()
    {
        var practicante = Nuevo();

        Assert.Equal(EstadoPracticante.Pendiente, practicante.Estado);
        Assert.Null(practicante.FechaEvaluacionUtc);
    }

    [Theory]
    [InlineData(EstadoPracticante.Viable)]
    [InlineData(EstadoPracticante.NoViable)]
    public void Evaluar_RegistraDecisionYFecha(EstadoPracticante decision)
    {
        var practicante = Nuevo();
        var fecha = new DateTime(2026, 11, 8, 15, 0, 0, DateTimeKind.Utc);

        practicante.Evaluar(decision, fecha);

        Assert.Equal(decision, practicante.Estado);
        Assert.Equal(fecha, practicante.FechaEvaluacionUtc);
    }

    [Fact]
    public void Evaluar_PermiteCambiarDeDecision()
    {
        var practicante = Nuevo();
        practicante.Evaluar(EstadoPracticante.NoViable, DateTime.UtcNow);

        practicante.Evaluar(EstadoPracticante.Viable, DateTime.UtcNow);

        Assert.Equal(EstadoPracticante.Viable, practicante.Estado);
    }

    [Fact]
    public void Evaluar_ComoPendiente_LanzaExcepcion()
    {
        var practicante = Nuevo();

        Assert.Throws<ArgumentException>(() => practicante.Evaluar(EstadoPracticante.Pendiente, DateTime.UtcNow));
    }
}
