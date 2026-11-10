using System.Globalization;
using System.Text.RegularExpressions;

namespace Practicas.Application.Validacion;

/// <summary>Reglas y normalización de nombres de persona.</summary>
public static partial class ValidadorNombre
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");

    private static readonly HashSet<string> Particulas =
        new(["de", "del", "la", "las", "los", "y", "e", "da", "van", "von"], StringComparer.Ordinal);

    /// <summary>
    /// Solo letras (con tildes), espacios, apóstrofes, puntos y guiones; al menos nombre y apellido;
    /// sin letras repetidas de forma anómala ("aaaa"). Devuelve el error o <c>null</c>.
    /// </summary>
    public static string? Validar(string nombre)
    {
        ArgumentNullException.ThrowIfNull(nombre);
        var limpio = nombre.Trim();

        if (!CaracteresPermitidos().IsMatch(limpio))
        {
            return "El nombre solo puede contener letras, espacios, apóstrofes y guiones.";
        }

        if (limpio.Split(' ', StringSplitOptions.RemoveEmptyEntries).Count(p => p.Length > 1) < 2)
        {
            return "Escribe tu nombre y al menos un apellido.";
        }

        return LetraRepetida().IsMatch(limpio)
            ? "El nombre tiene letras repetidas de forma inusual; revísalo."
            : null;
    }

    /// <summary>Quita espacios sobrantes, incluidos los repetidos entre palabras.</summary>
    public static string NormalizarTexto(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        return string.Join(' ', texto.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    /// <summary>
    /// Si el nombre llegó todo en mayúsculas o todo en minúsculas, lo pasa a nombre propio
    /// ("ANA DE LA ROSA" → "Ana de la Rosa"). Si trae mayúsculas y minúsculas, se respeta.
    /// </summary>
    public static string NormalizarNombre(string nombre)
    {
        var limpio = NormalizarTexto(nombre);
        var letras = limpio.Where(char.IsLetter).ToList();
        var todoMayusculas = letras.TrueForAll(char.IsUpper);
        var todoMinusculas = letras.TrueForAll(char.IsLower);
        if (!todoMayusculas && !todoMinusculas)
        {
            return limpio;
        }

        var palabras = limpio.ToLower(Cultura).Split(' ');
        for (var i = 0; i < palabras.Length; i++)
        {
            if (i == 0 || !Particulas.Contains(palabras[i]))
            {
                palabras[i] = Cultura.TextInfo.ToTitleCase(palabras[i]);
            }
        }

        return string.Join(' ', palabras);
    }

    [GeneratedRegex(@"^\p{L}[\p{L}\p{M}' .-]*\p{L}\.?$")]
    private static partial Regex CaracteresPermitidos();

    [GeneratedRegex(@"(\p{L})\1{3,}", RegexOptions.IgnoreCase)]
    private static partial Regex LetraRepetida();
}
