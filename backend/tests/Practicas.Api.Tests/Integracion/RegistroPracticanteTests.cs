using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Practicas.Application.Practicantes;
using Practicas.Domain;
using Practicas.Api.Tests.Infraestructura;

namespace Practicas.Api.Tests.Integracion;

public class RegistroPracticanteTests : IClassFixture<PracticasApiFactory>
{
    private readonly PracticasApiFactory _factory;
    private readonly HttpClient _cliente;

    public RegistroPracticanteTests(PracticasApiFactory factory)
    {
        _factory = factory;
        _cliente = factory.CreateClient();
    }

    [Fact]
    public async Task Registrar_ConDatosValidos_GuardaPracticanteYArchivo()
    {
        var archivosAntes = ArchivosGuardados();

        var respuesta = await _cliente.PostAsync("/api/practicantes",
            FormularioPracticante.Crear(nombre: "  Juan   Pérez ", correo: "Juan.Perez@Correo.com"));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var registro = await respuesta.Content.ReadFromJsonAsync<RegistroPracticanteResponse>();
        Assert.NotNull(registro);
        Assert.Equal("Juan Pérez", registro.NombreCompleto);

        var analista = await _factory.CrearClienteAnalistaAsync();
        var detalle = await analista.GetFromJsonAsync<PracticanteResponse>($"/api/practicantes/{registro.Id}", Json.Opciones);

        Assert.NotNull(detalle);
        Assert.Equal("juan.perez@correo.com", detalle.Correo);
        Assert.Equal(EstadoPracticante.Pendiente, detalle.Estado);
        Assert.Equal(8, detalle.Semestre);
        Assert.Equal("hoja-de-vida.pdf", detalle.HojaDeVidaNombre);
        Assert.Equal(DateTimeKind.Utc, detalle.FechaRegistroUtc.Kind);
        var archivoNuevo = Assert.Single(ArchivosGuardados().Except(archivosAntes));
        Assert.Equal(FormularioPracticante.PdfMinimo, await File.ReadAllBytesAsync(archivoNuevo));
    }

    [Theory]
    [InlineData("ANA MARÍA DE LA ROSA", "Ana María de la Rosa")]
    [InlineData("juan pérez", "Juan Pérez")]
    [InlineData("María José McAllister", "María José McAllister")]
    public async Task Registrar_NormalizaNombresEnMayusculasOMinusculas(string escrito, string esperado)
    {
        var respuesta = await _cliente.PostAsync("/api/practicantes",
            FormularioPracticante.Crear(nombre: escrito, correo: $"{Guid.NewGuid():N}@correo.com"));

        var registro = await respuesta.Content.ReadFromJsonAsync<RegistroPracticanteResponse>();
        Assert.Equal(esperado, registro!.NombreCompleto);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public async Task Registrar_SinAutorizarTratamientoDeDatos_Devuelve400(string? autoriza)
    {
        var respuesta = await _cliente.PostAsync("/api/practicantes",
            FormularioPracticante.Crear(correo: "sin.autorizacion@correo.com", autorizaTratamientoDatos: autoriza));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(problema!.Errors.Keys, k => k.Equals("AutorizaTratamientoDatos", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Registrar_PdfConJavaScript_Devuelve400YNoGuardaElArchivo()
    {
        var archivosAntes = ArchivosGuardados();
        var pdfMalicioso = System.Text.Encoding.ASCII.GetBytes(
            "%PDF-1.4\n1 0 obj << /Type /Catalog /OpenAction << /S /JavaScript /JS (app.alert(1)) >> >> endobj\n%%EOF");

        var respuesta = await _cliente.PostAsync("/api/practicantes",
            FormularioPracticante.Crear(correo: "pdf.malicioso@correo.com", archivo: pdfMalicioso));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("scripts", string.Join(' ', problema!.Errors["HojaDeVida"]));
        Assert.Empty(ArchivosGuardados().Except(archivosAntes));
    }

    private string[] ArchivosGuardados() => Directory.Exists(_factory.CarpetaHojasDeVida)
        ? Directory.GetFiles(_factory.CarpetaHojasDeVida)
        : [];

    [Theory]
    [InlineData("", "a@b.co", "Sistemas", "5", "NombreCompleto")]
    [InlineData("   ", "a@b.co", "Sistemas", "5", "NombreCompleto")]
    [InlineData("Ana Gómez", "no-es-correo", "Sistemas", "5", "Correo")]
    [InlineData("Ana Gómez", "a@b.co", "", "5", "Carrera")]
    [InlineData("Ana Gómez", "a@b.co", "Sistemas", "0", "Semestre")]
    [InlineData("Ana Gómez", "a@b.co", "Sistemas", "13", "Semestre")]
    [InlineData("Ana Gómez 123", "a@b.co", "Sistemas", "5", "NombreCompleto")]
    [InlineData("<script>alert(1)</script>", "a@b.co", "Sistemas", "5", "NombreCompleto")]
    [InlineData("Ana", "a@b.co", "Sistemas", "5", "NombreCompleto")]
    [InlineData("Annnnna Gómez", "a@b.co", "Sistemas", "5", "NombreCompleto")]
    [InlineData("Ana Gómez", "ana@correo", "Sistemas", "5", "Correo")]
    [InlineData("Ana Gómez", "a@b.co", "<b>Sistemas</b>", "5", "Carrera")]
    public async Task Registrar_ConCampoInvalido_Devuelve400ConElCampo(
        string nombre, string correo, string carrera, string semestre, string campoEsperado)
    {
        var respuesta = await _cliente.PostAsync("/api/practicantes",
            FormularioPracticante.Crear(nombre, correo, carrera, semestre));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(problema!.Errors.Keys, k => k.Equals(campoEsperado, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Registrar_SinHojaDeVida_Devuelve400()
    {
        var respuesta = await _cliente.PostAsync("/api/practicantes",
            FormularioPracticante.Crear(correo: "sin.archivo@correo.com", incluirArchivo: false));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(problema!.Errors.Keys, k => k.Equals("HojaDeVida", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Registrar_ArchivoQueNoEsPdf_Devuelve400()
    {
        var respuesta = await _cliente.PostAsync("/api/practicantes",
            FormularioPracticante.Crear(correo: "falso.pdf@correo.com", archivo: "MZ ejecutable"u8.ToArray()));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("PDF", string.Join(' ', problema!.Errors["HojaDeVida"]));
    }

    [Fact]
    public async Task Registrar_ArchivoMuyGrande_Devuelve400()
    {
        var grande = new byte[1024 * 1024 + 1];
        FormularioPracticante.PdfMinimo.CopyTo(grande, 0);

        var respuesta = await _cliente.PostAsync("/api/practicantes",
            FormularioPracticante.Crear(correo: "grande@correo.com", archivo: grande));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Registrar_CorreoDuplicado_Devuelve409()
    {
        var primero = await _cliente.PostAsync("/api/practicantes",
            FormularioPracticante.Crear(correo: "duplicado@correo.com"));
        var segundo = await _cliente.PostAsync("/api/practicantes",
            FormularioPracticante.Crear(correo: " DUPLICADO@correo.com "));

        Assert.Equal(HttpStatusCode.Created, primero.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);
        var problema = await segundo.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Correo ya registrado", problema!.Title);
    }
}
