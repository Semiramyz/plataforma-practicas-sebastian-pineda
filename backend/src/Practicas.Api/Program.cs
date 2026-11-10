using System.Text.Json.Serialization;
using Practicas.Api.Errores;
using Practicas.Api.Hosting;
using Practicas.Api.Seguridad;
using Practicas.Application;
using Practicas.Infrastructure;
using Practicas.Infrastructure.Autenticacion;

// Utilidad: `dotnet run -- --generar-hash "contraseña"` imprime el hash para Autenticacion:Analista:ContrasenaHash.
if (args is ["--generar-hash", var contrasenaPlana])
{
    Console.WriteLine(HashContrasena.Crear(contrasenaPlana));
    return;
}

var builder = WebApplication.CreateBuilder(args);

// ---------- Capas (Clean Architecture) ----------
builder.Services.AddAplicacion();
builder.Services.AddInfraestructura(builder.Configuration);

// ---------- Seguridad ----------
builder.Services.AddAutenticacionJwt();
builder.Services.AddLimitesSolicitudes(builder.Configuration);
builder.Services.AddHsts(o => o.MaxAge = TimeSpan.FromDays(365));

// ---------- API HTTP ----------
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();
builder.Services.AddOpenApi();

var origenesPermitidos = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(politica => politica
    .WithOrigins(origenesPermitidos)
    .WithHeaders("Authorization", "Content-Type")
    .WithMethods("GET", "POST", "PUT", "DELETE")
    .WithExposedHeaders("Content-Disposition")));

var app = builder.Build();

await app.Services.AplicarMigracionesAsync();

app.UseCabecerasSeguridad();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}
else
{
    // Fuera de desarrollo se exige HTTPS (OWASP A02): HSTS y redirección de HTTP a HTTPS.
    app.UseHsts();
    app.UseHttpsRedirection();
}

var archivosFrontend = app.UseFrontendIntegrado();

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/api/salud", () => Results.Ok(new { estado = "ok" })).AllowAnonymous();
app.MapFrontendIntegrado(archivosFrontend);

await app.RunAsync();

/// <summary>Expuesto para las pruebas de integración con WebApplicationFactory.</summary>
public partial class Program;
