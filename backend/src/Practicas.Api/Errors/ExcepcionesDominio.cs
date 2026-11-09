namespace Practicas.Api.Errors;

/// <summary>Error de negocio que se traduce a una respuesta HTTP con ProblemDetails.</summary>
public abstract class ExcepcionDominio(string titulo, string mensaje, int codigoHttp) : Exception(mensaje)
{
    public string Titulo { get; } = titulo;

    public int CodigoHttp { get; } = codigoHttp;
}

public sealed class CorreoDuplicadoException(string correo)
    : ExcepcionDominio(
        "Correo ya registrado",
        $"Ya existe un practicante registrado con el correo {correo}.",
        StatusCodes.Status409Conflict);
