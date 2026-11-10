namespace Practicas.Application.Errores;

/// <summary>
/// Error esperado del negocio. La capa HTTP decide el código de estado; aquí solo se describe el problema.
/// </summary>
public abstract class ReglaNegocioException : Exception
{
    protected ReglaNegocioException(string titulo, string mensaje)
        : base(mensaje)
    {
        Titulo = titulo;
    }

    public string Titulo { get; }
}

/// <summary>Ya existe un practicante con ese correo.</summary>
public sealed class CorreoDuplicadoException : ReglaNegocioException
{
    public CorreoDuplicadoException(string correo)
        : base("Correo ya registrado", $"Ya existe un practicante registrado con el correo {correo}.")
    {
    }
}
