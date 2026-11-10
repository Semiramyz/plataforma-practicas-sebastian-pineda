namespace Practicas.Application.Validacion;

/// <summary>
/// Reglas de aceptación de la hoja de vida: extensión .pdf, firma real "%PDF-", tamaño máximo
/// y ausencia de contenido activo (JavaScript, acciones que lanzan programas o archivos incrustados).
/// </summary>
public static class ValidadorPdf
{
    private static readonly byte[] FirmaPdf = "%PDF-"u8.ToArray();

    private static readonly byte[][] MarcadoresActivos =
    [
        "/JavaScript"u8.ToArray(),
        "/JS"u8.ToArray(),
        "/Launch"u8.ToArray(),
        "/EmbeddedFile"u8.ToArray(),
        "/RichMedia"u8.ToArray(),
        "/XFA"u8.ToArray()
    ];

    /// <summary>Devuelve el mensaje de error o <c>null</c> si el archivo es aceptable.</summary>
    public static string? Validar(string nombreArchivo, long tamanoBytes, Func<Stream> abrir, long tamanoMaximoBytes)
    {
        ArgumentNullException.ThrowIfNull(abrir);

        if (tamanoBytes == 0)
        {
            return "La hoja de vida está vacía.";
        }

        if (tamanoBytes > tamanoMaximoBytes)
        {
            return $"La hoja de vida no puede superar {tamanoMaximoBytes / (1024 * 1024)} MB.";
        }

        if (!string.Equals(Path.GetExtension(nombreArchivo), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "La hoja de vida debe ser un archivo PDF.";
        }

        // El tamaño ya está acotado por el máximo configurado: se puede revisar el archivo completo.
        var contenido = new byte[tamanoBytes];
        using (var stream = abrir())
        {
            stream.ReadExactly(contenido);
        }

        return ValidarContenido(contenido);
    }

    /// <summary>Valida la firma y el contenido activo de un PDF ya leído.</summary>
    public static string? ValidarContenido(ReadOnlySpan<byte> contenido)
    {
        if (!contenido.StartsWith(FirmaPdf))
        {
            return "El archivo no tiene un formato PDF válido.";
        }

        return TieneContenidoActivo(contenido)
            ? "El PDF contiene scripts, acciones automáticas o archivos incrustados y no se puede aceptar. Expórtalo de nuevo como PDF simple."
            : null;
    }

    /// <summary>
    /// Busca marcadores de contenido activo. Una hoja de vida legítima no los necesita y son el
    /// vector típico de los PDF maliciosos. El nombre debe terminar ahí: "/JS" sí, "/JSON" no.
    /// </summary>
    internal static bool TieneContenidoActivo(ReadOnlySpan<byte> contenido)
    {
        foreach (var marcador in MarcadoresActivos)
        {
            var desde = 0;
            while (desde < contenido.Length)
            {
                var indice = contenido[desde..].IndexOf(marcador);
                if (indice < 0)
                {
                    break;
                }

                var fin = desde + indice + marcador.Length;
                if (fin >= contenido.Length || !char.IsAsciiLetterOrDigit((char)contenido[fin]))
                {
                    return true;
                }

                desde = fin;
            }
        }

        return false;
    }
}
