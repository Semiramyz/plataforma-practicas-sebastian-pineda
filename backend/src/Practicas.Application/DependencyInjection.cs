using Microsoft.Extensions.DependencyInjection;
using Practicas.Application.Practicantes;

namespace Practicas.Application;

public static class DependencyInjection
{
    /// <summary>Registra los casos de uso de la aplicación.</summary>
    public static IServiceCollection AddAplicacion(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IPracticanteService, PracticanteService>();
        return services;
    }
}
