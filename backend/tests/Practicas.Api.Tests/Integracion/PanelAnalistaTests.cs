using System.Net;
using System.Net.Http.Json;
using Practicas.Api.Contracts;
using Practicas.Api.Domain;
using Practicas.Api.Tests.Infraestructura;

namespace Practicas.Api.Tests.Integracion;

/// <summary>
/// Cada prueba usa su propia fábrica (base de datos y carpeta propias) para que
/// los conteos y listados no dependan del orden de ejecución.
/// </summary>
public sealed class PanelAnalistaTests : IDisposable
{
    private readonly PracticasApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("GET", "/api/practicantes")]
    [InlineData("GET", "/api/practicantes/1")]
    [InlineData("PATCH", "/api/practicantes/1/estado")]
    [InlineData("GET", "/api/practicantes/1/hoja-de-vida")]
    public async Task EndpointsDelAnalista_SinToken_Devuelven401(string metodo, string ruta)
    {
        var solicitud = new HttpRequestMessage(new HttpMethod(metodo), ruta)
        {
            Content = metodo == "PATCH" ? JsonContent.Create(new { estado = "Viable" }) : null
        };

        var respuesta = await _factory.CreateClient().SendAsync(solicitud);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Login_ConCredencialesIncorrectas_Devuelve401()
    {
        var respuesta = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { usuario = PracticasApiFactory.Usuario, contrasena = "incorrecta" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Listar_DevuelvePracticantesYPermiteFiltrar()
    {
        var anonimo = _factory.CreateClient();
        await anonimo.PostAsync("/api/practicantes", FormularioPracticante.Crear(nombre: "Laura Ríos", correo: "laura@correo.com", carrera: "Ingeniería Industrial"));
        await anonimo.PostAsync("/api/practicantes", FormularioPracticante.Crear(nombre: "Carlos Díaz", correo: "carlos@correo.com", carrera: "Ingeniería de Sistemas"));
        var analista = await _factory.CrearClienteAnalistaAsync();

        var todos = await analista.GetFromJsonAsync<List<PracticanteResponse>>("/api/practicantes", Json.Opciones);
        var deSistemas = await analista.GetFromJsonAsync<List<PracticanteResponse>>("/api/practicantes?busqueda=sistemas", Json.Opciones);

        Assert.Equal(2, todos!.Count);
        Assert.Equal("Carlos Díaz", todos[0].NombreCompleto); // el más reciente primero
        Assert.Equal("Carlos Díaz", Assert.Single(deSistemas!).NombreCompleto);
    }

    [Fact]
    public async Task ActualizarEstado_MarcaViableYNoViable_YPermiteFiltrarPorEstado()
    {
        var anonimo = _factory.CreateClient();
        var r1 = await (await anonimo.PostAsync("/api/practicantes", FormularioPracticante.Crear(correo: "uno@correo.com")))
            .Content.ReadFromJsonAsync<RegistroPracticanteResponse>();
        var r2 = await (await anonimo.PostAsync("/api/practicantes", FormularioPracticante.Crear(correo: "dos@correo.com")))
            .Content.ReadFromJsonAsync<RegistroPracticanteResponse>();
        await anonimo.PostAsync("/api/practicantes", FormularioPracticante.Crear(correo: "tres@correo.com"));
        var analista = await _factory.CrearClienteAnalistaAsync();

        var viable = await analista.PatchAsJsonAsync($"/api/practicantes/{r1!.Id}/estado", new { estado = "Viable" });
        var noViable = await analista.PatchAsJsonAsync($"/api/practicantes/{r2!.Id}/estado", new { estado = "NoViable" });

        Assert.Equal(HttpStatusCode.OK, viable.StatusCode);
        var actualizado = await viable.Content.ReadFromJsonAsync<PracticanteResponse>(Json.Opciones);
        Assert.Equal(EstadoPracticante.Viable, actualizado!.Estado);
        Assert.NotNull(actualizado.FechaEvaluacionUtc);
        Assert.Equal(HttpStatusCode.OK, noViable.StatusCode);

        var viables = await analista.GetFromJsonAsync<List<PracticanteResponse>>("/api/practicantes?estado=Viable", Json.Opciones);
        var noViables = await analista.GetFromJsonAsync<List<PracticanteResponse>>("/api/practicantes?estado=NoViable", Json.Opciones);
        var pendientes = await analista.GetFromJsonAsync<List<PracticanteResponse>>("/api/practicantes?estado=Pendiente", Json.Opciones);
        Assert.Equal(r1.Id, Assert.Single(viables!).Id);
        Assert.Equal(r2.Id, Assert.Single(noViables!).Id);
        Assert.Single(pendientes!);
    }

    [Theory]
    [InlineData("Pendiente")]
    [InlineData("Aprobado")]
    [InlineData(null)]
    public async Task ActualizarEstado_ConEstadoNoPermitido_Devuelve400(string? estado)
    {
        var registro = await (await _factory.CreateClient().PostAsync("/api/practicantes", FormularioPracticante.Crear()))
            .Content.ReadFromJsonAsync<RegistroPracticanteResponse>();
        var analista = await _factory.CrearClienteAnalistaAsync();

        var respuesta = await analista.PatchAsJsonAsync($"/api/practicantes/{registro!.Id}/estado", new { estado });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task PracticanteInexistente_Devuelve404()
    {
        var analista = await _factory.CrearClienteAnalistaAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await analista.GetAsync("/api/practicantes/999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await analista.PatchAsJsonAsync("/api/practicantes/999/estado", new { estado = "Viable" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await analista.GetAsync("/api/practicantes/999/hoja-de-vida")).StatusCode);
    }

    [Fact]
    public async Task DescargarHojaDeVida_DevuelveElPdfConSuNombreOriginal()
    {
        var registro = await (await _factory.CreateClient().PostAsync("/api/practicantes",
                FormularioPracticante.Crear(nombreArchivo: "CV Ana Gómez.pdf")))
            .Content.ReadFromJsonAsync<RegistroPracticanteResponse>();
        var analista = await _factory.CrearClienteAnalistaAsync();

        var respuesta = await analista.GetAsync($"/api/practicantes/{registro!.Id}/hoja-de-vida");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("application/pdf", respuesta.Content.Headers.ContentType!.MediaType);
        Assert.Equal("CV Ana Gómez.pdf", respuesta.Content.Headers.ContentDisposition!.FileNameStar);
        Assert.Equal(FormularioPracticante.PdfMinimo, await respuesta.Content.ReadAsByteArrayAsync());
    }
}
