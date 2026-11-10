using Practicas.Application.Validacion;

namespace Practicas.Application.Tests;

public class ValidadorNombreTests
{
    [Theory]
    [InlineData("Ana Gómez")]
    [InlineData("María José O'Neil")]
    [InlineData("Juan-Pablo de la Cruz")]
    [InlineData("Ángela Núñez")]
    public void NombresValidos_NoTienenErrores(string nombre)
    {
        Assert.Null(ValidadorNombre.Validar(nombre));
    }

    [Theory]
    [InlineData("Ana 123", "solo puede contener letras")]
    [InlineData("<script>alert(1)</script>", "solo puede contener letras")]
    [InlineData("Ana", "al menos un apellido")]
    [InlineData("Aaaaana Gómez", "letras repetidas")]
    public void NombresInvalidos_ExplicanElProblema(string nombre, string mensaje)
    {
        Assert.Contains(mensaje, ValidadorNombre.Validar(nombre), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ANA MARÍA DE LA ROSA", "Ana María de la Rosa")]
    [InlineData("juan  pérez ", "Juan Pérez")]
    [InlineData("DE LA CRUZ JUAN", "De la Cruz Juan")]
    [InlineData("María José McAllister", "María José McAllister")]
    public void NormalizarNombre_SoloCorrigeNombresTodoEnMayusculasOMinusculas(string escrito, string esperado)
    {
        Assert.Equal(esperado, ValidadorNombre.NormalizarNombre(escrito));
    }
}
