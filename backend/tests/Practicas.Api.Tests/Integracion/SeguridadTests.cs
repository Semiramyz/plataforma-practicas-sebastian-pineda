using System.Net;
using System.Net.Http.Json;
using Practicas.Api.Tests.Infraestructura;

namespace Practicas.Api.Tests.Integracion;

public sealed class SeguridadTests
{
    [Fact]
    public async Task Login_BloqueaIntentosRepetidosConUn429()
    {
        using var factory = new PracticasApiFactory { LimiteIntentosLogin = 3 };
        var cliente = factory.CreateClient();
        var credencialesMalas = new { usuario = PracticasApiFactory.Usuario, contrasena = "adivinando" };

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await cliente.PostAsJsonAsync("/api/auth/login", credencialesMalas)).StatusCode);
        }

        // Ni siquiera la contraseña correcta entra mientras dura el bloqueo.
        var bloqueado = await cliente.PostAsJsonAsync("/api/auth/login",
            new { usuario = PracticasApiFactory.Usuario, contrasena = PracticasApiFactory.Contrasena });

        Assert.Equal(HttpStatusCode.TooManyRequests, bloqueado.StatusCode);
        Assert.True(bloqueado.Headers.Contains("Retry-After"));
        Assert.Contains("Demasiadas solicitudes", await bloqueado.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Registro_LimitaLaCantidadDeEnviosPorIp()
    {
        using var factory = new PracticasApiFactory { LimiteRegistros = 2 };
        var cliente = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsync("/api/practicantes", FormularioPracticante.Crear(correo: "a1@correo.com"))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsync("/api/practicantes", FormularioPracticante.Crear(correo: "a2@correo.com"))).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await cliente.PostAsync("/api/practicantes", FormularioPracticante.Crear(correo: "a3@correo.com"))).StatusCode);
    }

    [Theory]
    [InlineData("/api/salud")]
    [InlineData("/api/practicantes")]
    public async Task Respuestas_IncluyenCabecerasDeSeguridad(string ruta)
    {
        using var factory = new PracticasApiFactory();

        var respuesta = await factory.CreateClient().GetAsync(ruta);

        var csp = string.Join(';', respuesta.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("script-src 'self'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.Equal("nosniff", respuesta.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", respuesta.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", respuesta.Headers.GetValues("Referrer-Policy").Single());
        Assert.True(respuesta.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task LoginConContrasenaEnorme_SeRechazaSinCalcularElHash()
    {
        using var factory = new PracticasApiFactory();

        var respuesta = await factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { usuario = PracticasApiFactory.Usuario, contrasena = new string('x', 10_000) });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
