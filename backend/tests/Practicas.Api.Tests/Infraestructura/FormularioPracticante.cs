using System.Net.Http.Headers;
using System.Text;

namespace Practicas.Api.Tests.Infraestructura;

/// <summary>Construye el multipart/form-data que envía el formulario de registro.</summary>
public static class FormularioPracticante
{
    public static readonly byte[] PdfMinimo = Encoding.ASCII.GetBytes(
        "%PDF-1.4\n1 0 obj << /Type /Catalog >> endobj\ntrailer << /Root 1 0 R >>\n%%EOF");

    public static MultipartFormDataContent Crear(
        string? nombre = "Ana María Gómez",
        string? correo = "ana.gomez@unbosque.edu.co",
        string? carrera = "Ingeniería de Sistemas",
        string? semestre = "8",
        byte[]? archivo = null,
        string nombreArchivo = "hoja-de-vida.pdf",
        bool incluirArchivo = true)
    {
        var formulario = new MultipartFormDataContent();
        Agregar(formulario, "nombreCompleto", nombre);
        Agregar(formulario, "correo", correo);
        Agregar(formulario, "carrera", carrera);
        Agregar(formulario, "semestre", semestre);

        if (incluirArchivo)
        {
            var contenido = new ByteArrayContent(archivo ?? PdfMinimo);
            contenido.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            formulario.Add(contenido, "hojaDeVida", nombreArchivo);
        }

        return formulario;
    }

    private static void Agregar(MultipartFormDataContent formulario, string campo, string? valor)
    {
        if (valor is not null)
        {
            formulario.Add(new StringContent(valor), campo);
        }
    }
}
