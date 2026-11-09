using System.Text;
using Microsoft.AspNetCore.Http;
using Practicas.Api.Tests.Infraestructura;
using Practicas.Api.Validation;

namespace Practicas.Api.Tests.Unitarias;

public class ArchivoPdfAttributeTests
{
    private const long UnMegabyte = 1024 * 1024;

    private static IFormFile Archivo(byte[] contenido, string nombre = "cv.pdf") =>
        new FormFile(new MemoryStream(contenido), 0, contenido.Length, "hojaDeVida", nombre);

    [Fact]
    public void PdfValido_NoTieneErrores()
    {
        Assert.Null(ArchivoPdfAttribute.Validar(Archivo(FormularioPracticante.PdfMinimo), UnMegabyte));
    }

    [Fact]
    public void ExtensionEnMayusculas_EsValida()
    {
        Assert.Null(ArchivoPdfAttribute.Validar(Archivo(FormularioPracticante.PdfMinimo, "CV.PDF"), UnMegabyte));
    }

    [Fact]
    public void ArchivoVacio_EsInvalido()
    {
        Assert.Equal("La hoja de vida está vacía.", ArchivoPdfAttribute.Validar(Archivo([]), UnMegabyte));
    }

    [Fact]
    public void ArchivoQueSuperaElLimite_EsInvalido()
    {
        var error = ArchivoPdfAttribute.Validar(Archivo(new byte[UnMegabyte + 1]), UnMegabyte);

        Assert.Equal("La hoja de vida no puede superar 1 MB.", error);
    }

    [Theory]
    [InlineData("cv.docx")]
    [InlineData("cv.pdf.exe")]
    [InlineData("cv")]
    public void ExtensionDistintaDePdf_EsInvalida(string nombre)
    {
        Assert.Equal("La hoja de vida debe ser un archivo PDF.",
            ArchivoPdfAttribute.Validar(Archivo(FormularioPracticante.PdfMinimo, nombre), UnMegabyte));
    }

    [Fact]
    public void ArchivoRenombradoComoPdf_EsInvalido()
    {
        var contenido = Encoding.UTF8.GetBytes("Esto es un texto plano renombrado a .pdf");

        Assert.Equal("El archivo no tiene un formato PDF válido.",
            ArchivoPdfAttribute.Validar(Archivo(contenido), UnMegabyte));
    }
}
