using System.Text.Json;
using System.Text.Json.Serialization;

namespace Practicas.Api.Tests.Infraestructura;

public static class Json
{
    /// <summary>Mismas opciones que la API: camelCase y enums como texto.</summary>
    public static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
