using System.Net;
using Microsoft.AspNetCore.Hosting;
using Practicas.Api.Tests.Infraestructura;

namespace Practicas.Api.Tests.Integracion;

/// <summary>
/// Modo de un solo comando: la API sirve el frontend compilado cuando se configura "Frontend:Ruta".
/// </summary>
public sealed class FrontendIntegradoTests : IDisposable
{
    private const string IndexHtml = "<!doctype html><html><body><app-root></app-root></body></html>";

    private readonly string _carpetaFrontend =
        Path.Combine(Path.GetTempPath(), "practicas-tests", "web-" + Guid.NewGuid().ToString("N"));

    private readonly PracticasApiFactory _fabrica = new();

    public FrontendIntegradoTests()
    {
        Directory.CreateDirectory(_carpetaFrontend);
        File.WriteAllText(Path.Combine(_carpetaFrontend, "index.html"), IndexHtml);
        File.WriteAllText(Path.Combine(_carpetaFrontend, "main.js"), "console.log('ok');");
    }

    private HttpClient CrearCliente() =>
        _fabrica.WithWebHostBuilder(b => b.UseSetting("Frontend:Ruta", _carpetaFrontend)).CreateClient();

    [Theory]
    [InlineData("/")]
    [InlineData("/registro")]
    [InlineData("/analista")]
    public async Task Rutas_de_la_aplicacion_devuelven_el_index(string ruta)
    {
        var respuesta = await CrearCliente().GetAsync(ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("text/html", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<app-root>", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Sirve_los_archivos_estaticos_del_frontend()
    {
        var respuesta = await CrearCliente().GetAsync("/main.js");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("console.log('ok');", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task La_API_sigue_funcionando_y_sus_rutas_inexistentes_dan_404()
    {
        var cliente = CrearCliente();

        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/salud")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/practicantes")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/no-existe")).StatusCode);
    }

    [Fact]
    public void Una_ruta_sin_index_html_impide_arrancar()
    {
        var vacia = Path.Combine(_carpetaFrontend, "vacia");
        Directory.CreateDirectory(vacia);

        using var fabrica = _fabrica.WithWebHostBuilder(b => b.UseSetting("Frontend:Ruta", vacia));

        var error = Assert.Throws<InvalidOperationException>(() => fabrica.CreateClient());
        Assert.Contains("index.html", error.Message);
    }

    public void Dispose()
    {
        _fabrica.Dispose();
        try
        {
            Directory.Delete(_carpetaFrontend, recursive: true);
        }
        catch (IOException)
        {
            // Limpieza best-effort.
        }
    }
}
