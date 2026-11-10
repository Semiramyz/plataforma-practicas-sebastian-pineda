using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Practicas.Application.Practicantes;
using Practicas.Domain;
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

    private async Task<int> RegistrarAsync(string correo = "ana.gomez@correo.com", string nombre = "Ana María Gómez")
    {
        var respuesta = await _factory.CreateClient().PostAsync("/api/practicantes",
            FormularioPracticante.Crear(nombre: nombre, correo: correo));
        respuesta.EnsureSuccessStatusCode();
        return (await respuesta.Content.ReadFromJsonAsync<RegistroPracticanteResponse>())!.Id;
    }

    [Theory]
    [InlineData("GET", "/api/practicantes")]
    [InlineData("GET", "/api/practicantes/1")]
    [InlineData("PUT", "/api/practicantes/1/evaluacion")]
    [InlineData("PUT", "/api/practicantes/1/evaluacion/borrador")]
    [InlineData("DELETE", "/api/practicantes/1/evaluacion/borrador")]
    [InlineData("GET", "/api/practicantes/1/hoja-de-vida")]
    [InlineData("POST", "/api/auth/logout")]
    public async Task EndpointsDelAnalista_SinToken_Devuelven401(string metodo, string ruta)
    {
        var solicitud = new HttpRequestMessage(new HttpMethod(metodo), ruta)
        {
            Content = metodo == "PUT" ? JsonContent.Create(Evaluaciones.Viable()) : null
        };

        var respuesta = await _factory.CreateClient().SendAsync(solicitud);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task TokenFalsificado_Devuelve401()
    {
        var cliente = _factory.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new("Bearer",
            "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJhbmFsaXN0YSIsInJvbGUiOiJBbmFsaXN0YSJ9.firma-invalida");

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/practicantes")).StatusCode);
    }

    [Fact]
    public async Task Login_ConCredencialesIncorrectas_Devuelve401()
    {
        var respuesta = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { usuario = PracticasApiFactory.Usuario, contrasena = "incorrecta" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Logout_InvalidaElTokenAunqueNoHayaExpirado()
    {
        var analista = await _factory.CrearClienteAnalistaAsync();
        Assert.Equal(HttpStatusCode.OK, (await analista.GetAsync("/api/practicantes")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await analista.PostAsync("/api/auth/logout", null)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await analista.GetAsync("/api/practicantes")).StatusCode);
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
        Assert.All(todos, p => Assert.Null(p.Evaluacion));
        Assert.Equal("Carlos Díaz", Assert.Single(deSistemas!).NombreCompleto);
    }

    [Fact]
    public async Task Evaluar_ConRubrica_GuardaDecisionPuntajeYEvaluador()
    {
        var viableId = await RegistrarAsync("uno@correo.com");
        var noViableId = await RegistrarAsync("dos@correo.com");
        await RegistrarAsync("tres@correo.com");
        var analista = await _factory.CrearClienteAnalistaAsync();

        var viable = await analista.PutAsJsonAsync($"/api/practicantes/{viableId}/evaluacion", Evaluaciones.Viable());
        var noViable = await analista.PutAsJsonAsync($"/api/practicantes/{noViableId}/evaluacion", Evaluaciones.NoViablePorOrtografia());

        Assert.Equal(HttpStatusCode.OK, viable.StatusCode);
        var evaluado = await viable.Content.ReadFromJsonAsync<PracticanteResponse>(Json.Opciones);
        Assert.Equal(EstadoPracticante.Viable, evaluado!.Estado);
        Assert.NotNull(evaluado.FechaEvaluacionUtc);
        Assert.Equal(75, evaluado.Evaluacion!.PuntajeTotal);
        Assert.Equal(4.0m, evaluado.Evaluacion.Promedio);
        Assert.Equal(EstadoPracticante.Viable, evaluado.Evaluacion.Recomendacion);
        Assert.Equal("Analista de selección", evaluado.Evaluacion.Evaluador);

        Assert.Equal(HttpStatusCode.OK, noViable.StatusCode);
        var rechazado = await analista.GetFromJsonAsync<PracticanteResponse>($"/api/practicantes/{noViableId}", Json.Opciones);
        Assert.Equal(EstadoPracticante.NoViable, rechazado!.Estado);
        Assert.Equal([MotivoRechazo.ErroresOrtograficos], rechazado.Evaluacion!.MotivosRechazo);
        Assert.Equal(1, rechazado.Evaluacion.Calificacion.Ortografia);

        var viables = await analista.GetFromJsonAsync<List<PracticanteResponse>>("/api/practicantes?estado=Viable", Json.Opciones);
        var pendientes = await analista.GetFromJsonAsync<List<PracticanteResponse>>("/api/practicantes?estado=Pendiente", Json.Opciones);
        Assert.Equal(viableId, Assert.Single(viables!).Id);
        Assert.Single(pendientes!);
    }

    [Fact]
    public async Task Evaluar_PermiteCorregirYConservaElHistorialDeAuditoria()
    {
        var id = await RegistrarAsync();
        var analista = await _factory.CrearClienteAnalistaAsync();
        await analista.PutAsJsonAsync($"/api/practicantes/{id}/evaluacion", Evaluaciones.NoViablePorOrtografia());

        var respuesta = await analista.PutAsJsonAsync($"/api/practicantes/{id}/evaluacion", Evaluaciones.Viable());

        var practicante = await respuesta.Content.ReadFromJsonAsync<PracticanteResponse>(Json.Opciones);
        Assert.Equal(EstadoPracticante.Viable, practicante!.Estado);
        Assert.Empty(practicante.Evaluacion!.MotivosRechazo);

        // La decisión corregida no borra la anterior: ambas quedan en el historial, la más reciente primero.
        var detalle = await analista.GetFromJsonAsync<PracticanteResponse>($"/api/practicantes/{id}", Json.Opciones);
        Assert.Collection(detalle!.Historial,
            reciente => Assert.Equal((EstadoPracticante.Viable, 75), (reciente.Decision, reciente.PuntajeTotal)),
            anterior => Assert.Equal(EstadoPracticante.NoViable, anterior.Decision));
        Assert.All(detalle.Historial, h => Assert.Equal("Analista de selección", h.Evaluador));
    }

    public static TheoryData<string, object, string> EvaluacionesSinSustento => new()
    {
        {
            "sin decisión",
            new { calificacion = Evaluaciones.Calificacion() },
            "Decision"
        },
        {
            "decisión 'Pendiente'",
            new { decision = "Pendiente", calificacion = Evaluaciones.Calificacion() },
            "Decision"
        },
        {
            "sin rúbrica",
            new { decision = "Viable" },
            "Calificacion"
        },
        {
            "criterio sin calificar",
            new { decision = "Viable", calificacion = new { ortografia = 4, presentacion = 4, formacion = 4, experiencia = 4 } },
            "Calificacion.Habilidades"
        },
        {
            "criterio fuera de rango",
            new { decision = "Viable", calificacion = Evaluaciones.Calificacion(ortografia: 6) },
            "Calificacion.Ortografia"
        },
        {
            "no viable sin motivos",
            new { decision = "NoViable", calificacion = Evaluaciones.Calificacion(ortografia: 1) },
            "MotivosRechazo"
        },
        {
            "viable con motivos",
            new { decision = "Viable", calificacion = Evaluaciones.Calificacion(), motivosRechazo = new[] { "FormatoInadecuado" } },
            "MotivosRechazo"
        },
        {
            "motivo 'Otro' sin explicación",
            new { decision = "NoViable", calificacion = Evaluaciones.Calificacion(ortografia: 2), motivosRechazo = new[] { "Otro" } },
            "Observaciones"
        },
        {
            "viable con errores ortográficos y sin justificar",
            new { decision = "Viable", calificacion = Evaluaciones.Calificacion(ortografia: 2) },
            "Observaciones"
        },
        {
            "no viable con rúbrica excelente y sin justificar",
            new { decision = "NoViable", calificacion = Evaluaciones.Calificacion(5, 5, 5, 5, 5), motivosRechazo = new[] { "PerfilNoAlineado" } },
            "Observaciones"
        }
    };

    [Theory]
    [MemberData(nameof(EvaluacionesSinSustento))]
    public async Task Evaluar_SinSustento_Devuelve400ConElCampo(string caso, object cuerpo, string campoEsperado)
    {
        var id = await RegistrarAsync();
        var analista = await _factory.CrearClienteAnalistaAsync();

        var respuesta = await analista.PutAsJsonAsync($"/api/practicantes/{id}/evaluacion", cuerpo);

        Assert.True(respuesta.StatusCode == HttpStatusCode.BadRequest, $"Caso '{caso}': se esperaba 400 y fue {respuesta.StatusCode}.");
        var problema = await respuesta.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(problema!.Errors.Keys, k => k.Equals(campoEsperado, StringComparison.OrdinalIgnoreCase)
                                                    || k.Equals("$." + campoEsperado, StringComparison.OrdinalIgnoreCase)
                                                    || k.EndsWith(campoEsperado, StringComparison.OrdinalIgnoreCase));

        var practicante = await analista.GetFromJsonAsync<PracticanteResponse>($"/api/practicantes/{id}", Json.Opciones);
        Assert.Equal(EstadoPracticante.Pendiente, practicante!.Estado);
    }

    [Fact]
    public async Task Evaluar_DecisionContrariaJustificada_SeAcepta()
    {
        var id = await RegistrarAsync();
        var analista = await _factory.CrearClienteAnalistaAsync();

        var respuesta = await analista.PutAsJsonAsync($"/api/practicantes/{id}/evaluacion", new
        {
            decision = "Viable",
            calificacion = Evaluaciones.Calificacion(ortografia: 2),
            observaciones = "Dos errores menores; el portafolio de proyectos compensa ampliamente."
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Borrador_SeGuardaEnLaBaseDeDatosYSeRecuperaAlVolver()
    {
        var id = await RegistrarAsync();
        var analista = await _factory.CrearClienteAnalistaAsync();

        // El analista califica dos criterios, elige "No viable", marca un motivo y escribe un comentario.
        var guardado = await analista.PutAsJsonAsync($"/api/practicantes/{id}/evaluacion/borrador", new
        {
            ortografia = 2,
            presentacion = 4,
            decision = "NoViable",
            motivosRechazo = new[] { "ErroresOrtograficos" },
            observaciones = "Cuatro tildes faltantes en el perfil."
        });
        Assert.Equal(HttpStatusCode.OK, guardado.StatusCode);

        // Al volver (otra solicitud, otro contexto de base de datos) el avance sigue ahí.
        var detalle = await analista.GetFromJsonAsync<PracticanteResponse>($"/api/practicantes/{id}", Json.Opciones);
        var borrador = detalle!.Borrador!;
        Assert.Equal(EstadoPracticante.Pendiente, detalle.Estado);
        Assert.Equal(new CalificacionParcialDto(2, 4, null, null, null), borrador.Calificacion);
        Assert.Equal(EstadoPracticante.NoViable, borrador.Decision);
        Assert.Equal([MotivoRechazo.ErroresOrtograficos], borrador.MotivosRechazo);
        Assert.Equal("Cuatro tildes faltantes en el perfil.", borrador.Observaciones);
        Assert.Equal("Analista de selección", borrador.Evaluador);
    }

    [Fact]
    public async Task Borrador_SeReemplazaPorLaEvaluacionDefinitivaConPuntajesYComentario()
    {
        var id = await RegistrarAsync();
        var analista = await _factory.CrearClienteAnalistaAsync();
        await analista.PutAsJsonAsync($"/api/practicantes/{id}/evaluacion/borrador", new { ortografia = 1 });

        await analista.PutAsJsonAsync($"/api/practicantes/{id}/evaluacion", Evaluaciones.NoViablePorOrtografia());

        var detalle = await analista.GetFromJsonAsync<PracticanteResponse>($"/api/practicantes/{id}", Json.Opciones);
        Assert.Null(detalle!.Borrador);
        Assert.Equal(new CalificacionDto(1, 3, 4, 4, 4), detalle.Evaluacion!.Calificacion);
        Assert.Equal("Más de cinco errores ortográficos en el perfil profesional.", detalle.Evaluacion.Observaciones);
    }

    [Fact]
    public async Task Borrador_SePuedeDescartar()
    {
        var id = await RegistrarAsync();
        var analista = await _factory.CrearClienteAnalistaAsync();
        await analista.PutAsJsonAsync($"/api/practicantes/{id}/evaluacion/borrador", new { ortografia = 3 });

        Assert.Equal(HttpStatusCode.NoContent, (await analista.DeleteAsync($"/api/practicantes/{id}/evaluacion/borrador")).StatusCode);

        var detalle = await analista.GetFromJsonAsync<PracticanteResponse>($"/api/practicantes/{id}", Json.Opciones);
        Assert.Null(detalle!.Borrador);
    }

    [Fact]
    public async Task Borrador_ConDatosFueraDeRango_Devuelve400()
    {
        var id = await RegistrarAsync();
        var analista = await _factory.CrearClienteAnalistaAsync();

        var respuesta = await analista.PutAsJsonAsync($"/api/practicantes/{id}/evaluacion/borrador",
            new { ortografia = 9, decision = "Pendiente" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(problema!.Errors.Keys, k => k.EndsWith("Ortografia", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(problema.Errors.Keys, k => k.EndsWith("Decision", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PracticanteInexistente_Devuelve404()
    {
        var analista = await _factory.CrearClienteAnalistaAsync();

        Assert.Equal(HttpStatusCode.NotFound,
            (await analista.PutAsJsonAsync("/api/practicantes/999/evaluacion/borrador", new { ortografia = 3 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await analista.DeleteAsync("/api/practicantes/999/evaluacion/borrador")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await analista.GetAsync("/api/practicantes/999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await analista.PutAsJsonAsync("/api/practicantes/999/evaluacion", Evaluaciones.Viable())).StatusCode);
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
