using System.Security.Cryptography;
using System.Text;

namespace Practicas.Infrastructure.Autenticacion;

/// <summary>
/// Hash de contraseñas con PBKDF2-SHA256 y sal aleatoria. Formato guardado en la configuración:
/// <c>PBKDF2-SHA256$iteraciones$salBase64$hashBase64</c>. Así la contraseña nunca queda en texto plano.
/// </summary>
public static class HashContrasena
{
    private const string Algoritmo = "PBKDF2-SHA256";
    private const int Iteraciones = 210_000;
    private const int BytesSal = 16;
    private const int BytesHash = 32;

    public static string Crear(string contrasena)
    {
        var sal = RandomNumberGenerator.GetBytes(BytesSal);
        var hash = Derivar(contrasena, sal, Iteraciones, BytesHash);
        return $"{Algoritmo}${Iteraciones}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    /// <summary>Compara en tiempo constante. Devuelve <c>false</c> si el hash guardado no tiene un formato válido.</summary>
    public static bool Verificar(string contrasena, string hashGuardado)
    {
        var partes = hashGuardado.Split('$');
        if (partes.Length != 4 || partes[0] != Algoritmo || !int.TryParse(partes[1], out var iteraciones) || iteraciones < 1)
        {
            return false;
        }

        try
        {
            var sal = Convert.FromBase64String(partes[2]);
            var esperado = Convert.FromBase64String(partes[3]);
            var calculado = Derivar(contrasena, sal, iteraciones, esperado.Length);
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool TieneFormatoValido(string? hash) =>
        hash?.Split('$') is [Algoritmo, var iteraciones, { Length: > 0 }, { Length: > 0 }] && int.TryParse(iteraciones, out _);

    private static byte[] Derivar(string contrasena, byte[] sal, int iteraciones, int longitud) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(contrasena), sal, iteraciones, HashAlgorithmName.SHA256, longitud);
}
