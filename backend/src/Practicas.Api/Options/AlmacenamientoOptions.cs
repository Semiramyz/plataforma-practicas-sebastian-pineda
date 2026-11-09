using System.ComponentModel.DataAnnotations;

namespace Practicas.Api.Options;

public class AlmacenamientoOptions
{
    public const string Seccion = "Almacenamiento";

    /// <summary>
    /// Carpeta del servidor donde se guardan las hojas de vida.
    /// Si es relativa, se resuelve contra la raíz de contenido de la API.
    /// </summary>
    [Required]
    public string RutaHojasDeVida { get; set; } = "App_Data/hojas-de-vida";

    /// <summary>Tamaño máximo permitido para la hoja de vida (por defecto 5 MB).</summary>
    [Range(1, 50 * 1024 * 1024)]
    public long TamanoMaximoBytes { get; set; } = 5 * 1024 * 1024;
}
