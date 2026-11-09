using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Practicas.Api.Auth;
using Practicas.Api.Data;
using Practicas.Api.Errors;
using Practicas.Api.Options;
using Practicas.Api.Services;
using Practicas.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuración tipada ----------
builder.Services.AddOptions<AlmacenamientoOptions>()
    .Bind(builder.Configuration.GetSection(AlmacenamientoOptions.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<AutenticacionOptions>()
    .Bind(builder.Configuration.GetSection(AutenticacionOptions.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ---------- Persistencia (SQLite) ----------
builder.Services.AddDbContext<PracticasDbContext>((sp, opciones) =>
{
    var configuracion = sp.GetRequiredService<IConfiguration>();
    var entorno = sp.GetRequiredService<IWebHostEnvironment>();
    opciones.UseSqlite(ResolverCadenaSqlite(configuracion.GetConnectionString("Practicas"), entorno.ContentRootPath));
});

// ---------- Servicios de la aplicación ----------
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAlmacenamientoArchivos, AlmacenamientoLocal>();
builder.Services.AddScoped<IPracticanteService, PracticanteService>();
builder.Services.AddSingleton<IAutenticacionService, AutenticacionService>();

// ---------- Autenticación y autorización (JWT) ----------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<AutenticacionOptions>>((jwt, auth) =>
    {
        jwt.MapInboundClaims = false;
        jwt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = auth.Value.Emisor,
            ValidAudience = auth.Value.Audiencia,
            IssuerSigningKey = AutenticacionService.CrearClave(auth.Value.ClaveFirma),
            NameClaimType = ReclamosJwt.Nombre,
            RoleClaimType = ReclamosJwt.Rol,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// ---------- API ----------
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();
builder.Services.AddOpenApi();

var origenesPermitidos = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(politica => politica
    .WithOrigins(origenesPermitidos)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("Content-Disposition")));

var app = builder.Build();

// Aplica las migraciones pendientes al iniciar (crea la base de datos si no existe).
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<PracticasDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/api/salud", () => Results.Ok(new { estado = "ok" })).AllowAnonymous();

await app.RunAsync();

// Permite usar rutas relativas en la cadena de conexión, resueltas contra la raíz del proyecto.
static string ResolverCadenaSqlite(string? cadena, string raizContenido)
{
    var constructor = new SqliteConnectionStringBuilder(cadena ?? "Data Source=App_Data/practicas.db");
    if (!string.IsNullOrWhiteSpace(constructor.DataSource)
        && constructor.DataSource != ":memory:"
        && !Path.IsPathRooted(constructor.DataSource))
    {
        constructor.DataSource = Path.GetFullPath(Path.Combine(raizContenido, constructor.DataSource));
    }

    if (Path.GetDirectoryName(constructor.DataSource) is { Length: > 0 } carpeta)
    {
        Directory.CreateDirectory(carpeta);
    }

    return constructor.ToString();
}

/// <summary>Expuesto para las pruebas de integración con WebApplicationFactory.</summary>
public partial class Program;
