namespace Practicas.Api.Tests.Infraestructura;

/// <summary>Cuerpos JSON de evaluación listos para usar en las pruebas.</summary>
public static class Evaluaciones
{
    public static object Calificacion(int ortografia = 4, int presentacion = 4, int formacion = 4, int experiencia = 4, int habilidades = 4) =>
        new { ortografia, presentacion, formacion, experiencia, habilidades };

    /// <summary>Rúbrica alta y decisión coherente: no necesita observaciones.</summary>
    public static object Viable() => new { decision = "Viable", calificacion = Calificacion() };

    /// <summary>Rechazo por ortografía: la rúbrica lo recomienda y lleva su motivo.</summary>
    public static object NoViablePorOrtografia() => new
    {
        decision = "NoViable",
        calificacion = Calificacion(ortografia: 1, presentacion: 3),
        motivosRechazo = new[] { "ErroresOrtograficos" },
        observaciones = "Más de cinco errores ortográficos en el perfil profesional."
    };
}
