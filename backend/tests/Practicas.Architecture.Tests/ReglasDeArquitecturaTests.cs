using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Practicas.Domain;

namespace Practicas.Architecture.Tests;

/// <summary>
/// Hace cumplir la regla de dependencias de Clean Architecture: las capas internas no conocen
/// a las externas ni a los frameworks. Si alguien rompe la regla, la compilación de CI falla.
/// </summary>
public class ReglasDeArquitecturaTests
{
    private static readonly Assembly Dominio = typeof(Practicante).Assembly;
    private static readonly Assembly Aplicacion = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly Infraestructura = typeof(Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    private static IEnumerable<string> Referencias(Assembly ensamblado) =>
        ensamblado.GetReferencedAssemblies().Select(r => r.Name ?? string.Empty);

    [Fact]
    public void Dominio_NoDependeDeNingunaOtraCapaNiFramework()
    {
        var prohibidas = Referencias(Dominio).Where(r =>
            r.StartsWith("Practicas.", StringComparison.Ordinal) ||
            r.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
            r.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) ||
            r.StartsWith("Microsoft.Extensions", StringComparison.Ordinal));

        Assert.Empty(prohibidas);
    }

    [Fact]
    public void Aplicacion_SoloDependeDelDominio()
    {
        var prohibidas = Referencias(Aplicacion).Where(r =>
            r is "Practicas.Infrastructure" or "Practicas.Api" ||
            r.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
            r.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) ||
            r.StartsWith("Microsoft.Data.Sqlite", StringComparison.Ordinal));

        Assert.Empty(prohibidas);
        Assert.Contains("Practicas.Domain", Referencias(Aplicacion));
    }

    [Fact]
    public void Infraestructura_NoDependeDeLaApi()
    {
        Assert.DoesNotContain("Practicas.Api", Referencias(Infraestructura));
        Assert.DoesNotContain(Referencias(Infraestructura), r => r.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Dominio_ProtegeSusEstados_SinSettersPublicosEnLaEvaluacion()
    {
        var propiedadesConSetterPublico = new[] { typeof(Evaluacion), typeof(RegistroEvaluacion) }
            .SelectMany(t => t.GetProperties())
            .Where(p => p.SetMethod?.IsPublic == true)
            .Select(p => $"{p.DeclaringType!.Name}.{p.Name}");

        Assert.Empty(propiedadesConSetterPublico);
        Assert.False(typeof(Practicante).GetProperty(nameof(Practicante.Estado))!.SetMethod!.IsPublic);
    }

    [Fact]
    public void CadaAccionDeLaApi_DeclaraExplicitamenteSiRequiereSesion()
    {
        // Seguridad por diseño (OWASP A01): ningún endpoint queda público por olvido.
        var sinDeclarar = Api.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ControllerBase)))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any())
                .Where(m => !TieneAtributo<IAuthorizeData>(m) && !TieneAtributo<IAllowAnonymous>(m))
                .Where(m => !TieneAtributo<IAuthorizeData>(t) && !TieneAtributo<IAllowAnonymous>(t))
                .Select(m => $"{t.Name}.{m.Name}"));

        Assert.Empty(sinDeclarar);
    }

    [Fact]
    public void AccionesPublicas_SonSoloLasPrevistas()
    {
        var publicas = Api.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ControllerBase)))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any())
                .Where(m => TieneAtributo<IAllowAnonymous>(m) || (!TieneAtributo<IAuthorizeData>(m) && !TieneAtributo<IAuthorizeData>(t)))
                .Select(m => $"{t.Name}.{m.Name}"))
            .Order();

        Assert.Equal(["AuthController.Login", "PracticantesController.Registrar"], publicas);
    }

    private static bool TieneAtributo<T>(MemberInfo miembro) =>
        miembro.GetCustomAttributes(inherit: true).OfType<T>().Any();
}
