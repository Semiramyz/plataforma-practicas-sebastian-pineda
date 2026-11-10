using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Practicas.Application.Autenticacion;
using Practicas.Infrastructure.Autenticacion;

namespace Practicas.Api.Seguridad;

public static class AutenticacionJwtExtensions
{
    /// <summary>
    /// Valida los JWT del analista: firma HMAC-SHA256, emisor, audiencia y expiración
    /// (con 1 minuto de tolerancia), y rechaza los tokens revocados al cerrar sesión.
    /// </summary>
    public static IServiceCollection AddAutenticacionJwt(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AutenticacionOptions>>((jwt, auth) =>
            {
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = auth.Value.Emisor,
                    ValidAudience = auth.Value.Audiencia,
                    IssuerSigningKey = AutenticacionService.CrearClave(auth.Value.ClaveFirma),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = ReclamosJwt.Nombre,
                    RoleClaimType = ReclamosJwt.Rol,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
                jwt.Events = new JwtBearerEvents
                {
                    // Un token de una sesión cerrada (logout) se rechaza aunque no haya expirado.
                    OnTokenValidated = contexto =>
                    {
                        var jti = contexto.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                        var revocacion = contexto.HttpContext.RequestServices.GetRequiredService<IRevocacionTokens>();
                        if (jti is null || revocacion.EstaRevocado(jti))
                        {
                            contexto.Fail("La sesión fue cerrada.");
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        // Política por defecto: todo endpoint exige sesión salvo que se marque [AllowAnonymous].
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        return services;
    }
}
