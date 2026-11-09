using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Practicas.Api.Contracts;

namespace Practicas.Api.Tests.Infraestructura;

/// <summary>
/// Levanta la API completa en memoria con una base de datos SQLite y una carpeta
/// de archivos temporales, aisladas para cada instancia de la fábrica.
/// </summary>
public sealed class PracticasApiFactory : WebApplicationFactory<Program>
{
    public const string Usuario = "analista.pruebas";
    public const string Contrasena = "Contrasena-De-Pruebas-123";

    private readonly string _carpetaTemporal =
        Path.Combine(Path.GetTempPath(), "practicas-tests", Guid.NewGuid().ToString("N"));

    public string CarpetaHojasDeVida => Path.Combine(_carpetaTemporal, "hojas-de-vida");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Practicas", $"Data Source={Path.Combine(_carpetaTemporal, "practicas.db")};Pooling=False");
        builder.UseSetting("Almacenamiento:RutaHojasDeVida", CarpetaHojasDeVida);
        builder.UseSetting("Almacenamiento:TamanoMaximoBytes", (1024 * 1024).ToString());
        builder.UseSetting("Autenticacion:ClaveFirma", "clave-para-pruebas-de-integracion-0123456789");
        builder.UseSetting("Autenticacion:Analista:Usuario", Usuario);
        builder.UseSetting("Autenticacion:Analista:Contrasena", Contrasena);
    }

    /// <summary>Cliente con un JWT de analista válido.</summary>
    public async Task<HttpClient> CrearClienteAnalistaAsync()
    {
        var cliente = CreateClient();
        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login", new { usuario = Usuario, contrasena = Contrasena });
        respuesta.EnsureSuccessStatusCode();

        var sesion = await respuesta.Content.ReadFromJsonAsync<LoginResponse>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion!.Token);
        return cliente;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_carpetaTemporal))
        {
            try
            {
                Directory.Delete(_carpetaTemporal, recursive: true);
            }
            catch (IOException)
            {
                // Limpieza best-effort de archivos temporales.
            }
        }
    }
}
