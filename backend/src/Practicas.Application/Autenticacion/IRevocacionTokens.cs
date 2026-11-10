namespace Practicas.Application.Autenticacion;

/// <summary>
/// Tokens revocados al cerrar sesión. Un JWT es válido hasta que expira; guardando su identificador
/// (jti) hasta esa fecha, un token robado deja de servir en cuanto el analista cierra la sesión.
/// </summary>
public interface IRevocacionTokens
{
    void Revocar(string jti, DateTimeOffset expira);

    bool EstaRevocado(string jti);
}
