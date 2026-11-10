using System.Text;
using Practicas.Application.Validacion;

namespace Practicas.Application.Tests;

public class ValidadorPdfTests
{
    private const long UnMegabyte = 1024 * 1024;

    private static readonly byte[] PdfMinimo = Encoding.ASCII.GetBytes(
        "%PDF-1.4\n1 0 obj << /Type /Catalog >> endobj\ntrailer << /Root 1 0 R >>\n%%EOF");

    private static string? Validar(byte[] contenido, string nombre = "cv.pdf", long maximo = UnMegabyte) =>
        ValidadorPdf.Validar(nombre, contenido.Length, () => new MemoryStream(contenido), maximo);

    [Fact]
    public void PdfValido_NoTieneErrores()
    {
        Assert.Null(Validar(PdfMinimo));
    }

    [Fact]
    public void ExtensionEnMayusculas_EsValida()
    {
        Assert.Null(Validar(PdfMinimo, "CV.PDF"));
    }

    [Fact]
    public void ArchivoVacio_EsInvalido()
    {
        Assert.Equal("La hoja de vida está vacía.", Validar([]));
    }

    [Fact]
    public void ArchivoQueSuperaElLimite_EsInvalido()
    {
        Assert.Equal("La hoja de vida no puede superar 1 MB.", Validar(new byte[UnMegabyte + 1]));
    }

    [Theory]
    [InlineData("cv.docx")]
    [InlineData("cv.pdf.exe")]
    [InlineData("cv")]
    public void ExtensionDistintaDePdf_EsInvalida(string nombre)
    {
        Assert.Equal("La hoja de vida debe ser un archivo PDF.", Validar(PdfMinimo, nombre));
    }

    [Fact]
    public void ArchivoRenombradoComoPdf_EsInvalido()
    {
        var contenido = Encoding.UTF8.GetBytes("Esto es un texto plano renombrado a .pdf");

        Assert.Equal("El archivo no tiene un formato PDF válido.", Validar(contenido));
    }

    [Theory]
    [InlineData("/S /JavaScript /JS (app.alert(1))")]
    [InlineData("/JS<6170702e616c657274>")]
    [InlineData("/S /Launch /F (cmd.exe)")]
    [InlineData("/Type /EmbeddedFile")]
    [InlineData("/RichMedia")]
    [InlineData("/XFA 5 0 R")]
    public void PdfConContenidoActivo_EsInvalido(string fragmento)
    {
        var contenido = Encoding.ASCII.GetBytes($"%PDF-1.7\n1 0 obj << {fragmento} >> endobj\n%%EOF");

        Assert.StartsWith("El PDF contiene scripts", Validar(contenido), StringComparison.Ordinal);
    }

    [Fact]
    public void NombresParecidosAMarcadores_NoSonFalsosPositivos()
    {
        // "/JSON" o "/Launcher" no son acciones activas: no deben rechazar una hoja de vida legítima.
        var contenido = Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj << /JSON 1 /Launcher 2 >> endobj\n%%EOF");

        Assert.Null(Validar(contenido));
    }
}
