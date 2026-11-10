using Microsoft.Extensions.Caching.Memory;
using Practicas.Application.Autenticacion;

namespace Practicas.Infrastructure.Autenticacion;

/// <summary>
/// Lista de tokens revocados en memoria: cada jti se guarda solo hasta la expiración del token.
/// La API corre en un único proceso; con varias instancias se usaría una caché distribuida (Redis).
/// </summary>
public sealed class RevocacionTokensEnMemoria(IMemoryCache cache) : IRevocacionTokens
{
    private static string Clave(string jti) => $"token-revocado:{jti}";

    public void Revocar(string jti, DateTimeOffset expira) =>
        cache.Set(Clave(jti), true, expira);

    public bool EstaRevocado(string jti) => cache.TryGetValue(Clave(jti), out _);
}
