using Practicas.Domain;

namespace Practicas.Domain.Tests;

public class ReglasEvaluacionTests
{
    [Theory]
    [InlineData(1, 1, 1, 1, 1, 0, 1.0)]
    [InlineData(3, 3, 3, 3, 3, 50, 3.0)]
    [InlineData(4, 4, 4, 4, 4, 75, 4.0)]
    [InlineData(5, 5, 5, 5, 5, 100, 5.0)]
    [InlineData(5, 4, 3, 4, 5, 80, 4.2)]
    public void Calificacion_CalculaPuntajeYPromedio(int o, int p, int f, int e, int h, int puntaje, double promedio)
    {
        var calificacion = new Calificacion(o, p, f, e, h);

        Assert.Equal(puntaje, calificacion.PuntajeTotal);
        Assert.Equal((decimal)promedio, calificacion.Promedio);
    }

    [Theory]
    [InlineData(4, 4, 4, 4, 4, EstadoPracticante.Viable)]
    [InlineData(3, 3, 3, 3, 3, EstadoPracticante.Viable)]
    [InlineData(2, 5, 5, 5, 5, EstadoPracticante.NoViable)]   // ortografía deficiente
    [InlineData(5, 5, 1, 5, 5, EstadoPracticante.NoViable)]   // un criterio en 1
    [InlineData(3, 3, 2, 3, 2, EstadoPracticante.NoViable)]   // promedio 2,6
    public void Calificacion_RecomiendaSegunEstandaresDeCalidad(int o, int p, int f, int e, int h, EstadoPracticante esperada)
    {
        Assert.Equal(esperada, new Calificacion(o, p, f, e, h).Recomendacion);
    }

    [Fact]
    public void EvaluacionCoherente_NoTieneErrores()
    {
        Assert.Empty(ReglasEvaluacion.Validar(EstadoPracticante.Viable, new(4, 4, 4, 4, 4), [], null));
        Assert.Empty(ReglasEvaluacion.Validar(EstadoPracticante.NoViable, new(1, 3, 3, 3, 3),
            [MotivoRechazo.ErroresOrtograficos], null));
    }

    [Fact]
    public void DecisionContrariaALaRubrica_ExigeJustificacionSuficiente()
    {
        var errores = ReglasEvaluacion.Validar(EstadoPracticante.Viable, new(2, 4, 4, 4, 4), [], "Me gustó");

        Assert.Contains("Observaciones", errores.Keys);
        Assert.Empty(ReglasEvaluacion.Validar(EstadoPracticante.Viable, new(2, 4, 4, 4, 4), [],
            "Errores menores; el portafolio de proyectos es sobresaliente."));
    }

    [Fact]
    public void MotivoOtro_ExigeObservaciones()
    {
        var errores = ReglasEvaluacion.Validar(EstadoPracticante.NoViable, new(1, 3, 3, 3, 3), [MotivoRechazo.Otro], " ");

        Assert.Equal(["Explica en las observaciones el motivo 'Otro'."], errores["Observaciones"]);
    }

    [Fact]
    public void MotivoInexistente_EsInvalido()
    {
        var errores = ReglasEvaluacion.Validar(EstadoPracticante.NoViable, new(1, 3, 3, 3, 3), [(MotivoRechazo)99], null);

        Assert.Contains("MotivosRechazo", errores.Keys);
    }
}
