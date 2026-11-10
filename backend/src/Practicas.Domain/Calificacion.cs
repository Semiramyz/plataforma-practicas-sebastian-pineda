namespace Practicas.Domain;

/// <summary>
/// Calificación de la hoja de vida según los estándares de calidad de la rúbrica.
/// Cada criterio va de 1 (deficiente) a 5 (excelente).
/// </summary>
public sealed record Calificacion(int Ortografia, int Presentacion, int Formacion, int Experiencia, int Habilidades)
{
    public const int Minimo = 1;
    public const int Maximo = 5;

    /// <summary>Nombre de cada criterio junto con su puntaje, en el orden de la rúbrica.</summary>
    public IEnumerable<(string Criterio, int Puntaje)> Criterios =>
    [
        (nameof(Ortografia), Ortografia),
        (nameof(Presentacion), Presentacion),
        (nameof(Formacion), Formacion),
        (nameof(Experiencia), Experiencia),
        (nameof(Habilidades), Habilidades)
    ];

    public bool EsValida => Criterios.All(c => c.Puntaje is >= Minimo and <= Maximo);

    /// <summary>Promedio de los criterios (1,0 a 5,0) con un decimal.</summary>
    public decimal Promedio => Math.Round(Criterios.Sum(c => c.Puntaje) / 5m, 1);

    /// <summary>Puntaje global de 0 a 100: 1 en todos los criterios = 0, 5 en todos = 100.</summary>
    public int PuntajeTotal => (int)Math.Round((Criterios.Sum(c => c.Puntaje) - 5) / 20m * 100);

    /// <summary>
    /// Recomendación según los estándares de calidad:
    /// es "No viable" si la ortografía está en 2 o menos, si algún criterio tiene 1
    /// o si el promedio es menor que 3. En los demás casos es "Viable".
    /// </summary>
    public EstadoPracticante Recomendacion =>
        Ortografia <= 2 || Criterios.Any(c => c.Puntaje == Minimo) || Promedio < 3m
            ? EstadoPracticante.NoViable
            : EstadoPracticante.Viable;
}
